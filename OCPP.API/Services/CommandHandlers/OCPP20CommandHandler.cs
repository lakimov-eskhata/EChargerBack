using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using OCPP.API.Services;

namespace OCPP.API.Services.CommandHandlers;

public partial class OCPP20CommandHandler : ICommandHandler
{
    private readonly Dictionary<string, Func<string, object, Task<CommandResult>>> _handlers;
    private readonly ILogger<OCPP20CommandHandler> _logger;
    private readonly IChargePointConnectionStorage _connectionStorage;
    private readonly IPendingCommandsStorage _pendingCommands;

    public OCPP20CommandHandler(
        ILogger<OCPP20CommandHandler> logger,
        IChargePointConnectionStorage connectionStorage,
        IPendingCommandsStorage pendingCommands)
    {
        _logger = logger;
        _connectionStorage = connectionStorage;
        _pendingCommands = pendingCommands;

        _handlers = new Dictionary<string, Func<string, object, Task<CommandResult>>>
        {
            ["Reset"] = HandleResetAsync,
            ["SetChargingProfile"] = HandleSetChargingProfileAsync,
            ["ClearChargingProfile"] = HandleClearChargingProfileAsync,
            ["ChangeAvailability"] = HandleChangeAvailabilityAsync,
            ["TriggerMessage"] = HandleTriggerMessageAsync,
            ["GetLog"] = HandleGetLogAsync,
            ["SetVariables"] = HandleSetVariablesAsync,
            ["GetVariables"] = HandleGetVariablesAsync,
            ["GetBaseReport"] = HandleGetBaseReportAsync,
            ["GetReport"] = HandleGetReportAsync,
            ["SetMonitoringLevel"] = HandleSetMonitoringLevelAsync,
            ["SetMonitoringBase"] = HandleSetMonitoringBaseAsync,
            ["GetMonitoringReport"] = HandleGetMonitoringReportAsync,
            ["ClearVariableMonitoring"] = HandleClearVariableMonitoringAsync,
            ["CustomerInformation"] = HandleCustomerInformationAsync,
            ["GetCompositeSchedule"] = HandleGetCompositeScheduleAsync,
            ["UnlockConnector"] = HandleUnlockConnectorAsync,
            ["RequestStartTransaction"] = HandleRequestStartTransactionAsync,
            ["RequestStopTransaction"] = HandleRequestStopTransactionAsync
        };
    }

    public bool CanHandle(string action) => _handlers.ContainsKey(action);

    public async Task<CommandResult> HandleAsync(string chargePointId, ServerCommand command)
    {
        if (!_handlers.TryGetValue(command.Action, out var handler))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Unsupported OCPP 2.0 command: {command.Action}"
            };
        }

        try
        {
            // Проверяем подключение
            if (!await _connectionStorage.IsConnectedAsync(chargePointId))
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Charge point {chargePointId} is not connected"
                };
            }

            // Генерируем уникальный ID для команды
            var messageId = Guid.NewGuid().ToString();

            // Создаем JSON-RPC сообщение
            var jsonRpcMessage = new
            {
                jsonrpc = "2.0",
                id = messageId,
                method = command.Action,
                @params = command.Payload
            };

            var json = System.Text.Json.JsonSerializer.Serialize(jsonRpcMessage);

            // Сохраняем команду как ожидающую ответа
            await _pendingCommands.AddAsync(new PendingCommand
            {
                MessageId = messageId,
                ChargePointId = chargePointId,
                Action = command.Action,
                Payload = command.Payload,
                SentAt = DateTime.UtcNow
            });

            // Отправляем команду
            await _connectionStorage.SendMessageAsync(chargePointId, json);

            _logger.LogInformation(
                "Sent OCPP 2.0 command {Action} to {ChargePointId} with messageId {MessageId}",
                command.Action, chargePointId, messageId);

            return new CommandResult
            {
                Success = true,
                Response = new { messageId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OCPP 2.0 command {Action} for {ChargePointId}",
                command.Action, chargePointId);

            return new CommandResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<CommandResult> HandleResetAsync(string chargePointId, object payload)
    {
        var resetPayload = DeserializePayload<ResetRequest>(payload);

        var jsonRpcPayload = new
        {
            type = resetPayload.Type // "Hard", "Soft", "SoftOnIdle"
        };

        return await SendJsonRpcCommandAsync(chargePointId, "Reset", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleSetChargingProfileAsync(string chargePointId, object payload)
    {
        var profilePayload = DeserializePayload<SetChargingProfileRequest>(payload);

        var jsonRpcPayload = new
        {
            evseId = profilePayload.EvseId,
            chargingProfile = profilePayload.ChargingProfile
        };

        return await SendJsonRpcCommandAsync(chargePointId, "SetChargingProfile", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleClearChargingProfileAsync(string chargePointId, object payload)
    {
        var clearPayload = DeserializePayload<ClearChargingProfileRequest>(payload);

        var jsonRpcPayload = new
        {
            chargingProfileId = clearPayload.ChargingProfileId,
            chargingProfilePurpose = clearPayload.ChargingProfilePurpose,
            evseId = clearPayload.EvseId
        };

        return await SendJsonRpcCommandAsync(chargePointId, "ClearChargingProfile", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleChangeAvailabilityAsync(string chargePointId, object payload)
    {
        var availabilityPayload = DeserializePayload<ChangeAvailabilityRequest>(payload);

        var jsonRpcPayload = new
        {
            operationalStatus = availabilityPayload.OperationalStatus, // "Inoperative", "Operative"
            evse = availabilityPayload.EvseId.HasValue ? new { id = availabilityPayload.EvseId.Value } : null
        };

        return await SendJsonRpcCommandAsync(chargePointId, "ChangeAvailability", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleTriggerMessageAsync(string chargePointId, object payload)
    {
        var triggerPayload = DeserializePayload<TriggerMessageRequest>(payload);

        var jsonRpcPayload = new
        {
            requestedMessage = triggerPayload.RequestedMessage, // "BootNotification", "Heartbeat", etc.
            evse = triggerPayload.EvseId.HasValue ? new { id = triggerPayload.EvseId.Value } : null
        };

        return await SendJsonRpcCommandAsync(chargePointId, "TriggerMessage", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleGetLogAsync(string chargePointId, object payload)
    {
        var logPayload = DeserializePayload<GetLogRequest>(payload);

        var jsonRpcPayload = new
        {
            logType = logPayload.LogType, // "DiagnosticsLog", "SecurityLog"
            requestId = logPayload.RequestId,
            log = new
            {
                remoteLocation = logPayload.RemoteLocation,
                oldestTimestamp = logPayload.OldestTimestamp?.ToString("o"),
                latestTimestamp = logPayload.LatestTimestamp?.ToString("o")
            }
        };

        return await SendJsonRpcCommandAsync(chargePointId, "GetLog", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleSetVariablesAsync(string chargePointId, object payload)
    {
        var variablesPayload = DeserializePayload<SetVariablesRequest>(payload);

        var jsonRpcPayload = new
        {
            setVariableData = variablesPayload.SetVariableData.Select(v => new
            {
                component = new { name = v.ComponentName, instance = v.ComponentInstance },
                variable = new { name = v.VariableName },
                attributeType = v.AttributeType, // "Actual", "Target", "MinSet", "MaxSet"
                attributeValue = v.AttributeValue
            }).ToArray()
        };

        return await SendJsonRpcCommandAsync(chargePointId, "SetVariables", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleGetVariablesAsync(string chargePointId, object payload)
    {
        var variablesPayload = DeserializePayload<GetVariablesRequest>(payload);

        var jsonRpcPayload = new
        {
            getVariableData = variablesPayload.GetVariableData.Select(v => new
            {
                component = new { name = v.ComponentName, instance = v.ComponentInstance },
                variable = new { name = v.VariableName },
                attributeType = v.AttributeType // "Actual", "Target", "MinSet", "MaxSet"
            }).ToArray()
        };

        return await SendJsonRpcCommandAsync(chargePointId, "GetVariables", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleGetBaseReportAsync(string chargePointId, object payload)
    {
        var reportPayload = DeserializePayload<GetBaseReportRequest>(payload);

        var jsonRpcPayload = new
        {
            requestId = reportPayload.RequestId,
            reportBase = reportPayload.ReportBase // "ConfigurationInventory", "FullInventory", "SummaryInventory"
        };

        return await SendJsonRpcCommandAsync(chargePointId, "GetBaseReport", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleGetReportAsync(string chargePointId, object payload)
    {
        var reportPayload = DeserializePayload<GetReportRequest>(payload);

        var jsonRpcPayload = new
        {
            requestId = reportPayload.RequestId,
            componentCriteria = reportPayload.ComponentCriteria,
            componentVariable = reportPayload.ComponentVariable
        };

        return await SendJsonRpcCommandAsync(chargePointId, "GetReport", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleSetMonitoringLevelAsync(string chargePointId, object payload)
    {
        var monitoringPayload = DeserializePayload<SetMonitoringLevelRequest>(payload);

        var jsonRpcPayload = new
        {
            severity = monitoringPayload.Severity // 0-9, где 0 - выключено, 9 - все
        };

        return await SendJsonRpcCommandAsync(chargePointId, "SetMonitoringLevel", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleSetMonitoringBaseAsync(string chargePointId, object payload)
    {
        var monitoringPayload = DeserializePayload<SetMonitoringBaseRequest>(payload);

        var jsonRpcPayload = new
        {
            monitoringBase = monitoringPayload.MonitoringBase // "All", "FactoryDefault", "HardWiredOnly"
        };

        return await SendJsonRpcCommandAsync(chargePointId, "SetMonitoringBase", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleGetMonitoringReportAsync(string chargePointId, object payload)
    {
        var reportPayload = DeserializePayload<GetMonitoringReportRequest>(payload);

        var jsonRpcPayload = new
        {
            requestId = reportPayload.RequestId,
            componentVariable = reportPayload.ComponentVariable,
            monitoringCriteria = reportPayload.MonitoringCriteria
        };

        return await SendJsonRpcCommandAsync(chargePointId, "GetMonitoringReport", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleClearVariableMonitoringAsync(string chargePointId, object payload)
    {
        var monitoringPayload = DeserializePayload<ClearVariableMonitoringRequest>(payload);

        var jsonRpcPayload = new
        {
            id = monitoringPayload.Ids
        };

        return await SendJsonRpcCommandAsync(chargePointId, "ClearVariableMonitoring", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleCustomerInformationAsync(string chargePointId, object payload)
    {
        var customerPayload = DeserializePayload<CustomerInformationRequest>(payload);

        var jsonRpcPayload = new
        {
            requestId = customerPayload.RequestId,
            report = customerPayload.Report,
            clear = customerPayload.Clear,
            customerCertificate = customerPayload.CustomerCertificate,
            idToken = customerPayload.IdToken,
            customerIdentifier = customerPayload.CustomerIdentifier
        };

        return await SendJsonRpcCommandAsync(chargePointId, "CustomerInformation", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleGetCompositeScheduleAsync(string chargePointId, object payload)
    {
        var schedulePayload = DeserializePayload<GetCompositeScheduleRequest>(payload);

        var jsonRpcPayload = new
        {
            duration = schedulePayload.Duration,
            evseId = schedulePayload.EvseId,
            chargingRateUnit = schedulePayload.ChargingRateUnit // "A", "W"
        };

        return await SendJsonRpcCommandAsync(chargePointId, "GetCompositeSchedule", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleUnlockConnectorAsync(string chargePointId, object payload)
    {
        var unlockPayload = DeserializePayload<UnlockConnectorRequest>(payload);

        var jsonRpcPayload = new
        {
            evseId = unlockPayload.EvseId
        };

        return await SendJsonRpcCommandAsync(chargePointId, "UnlockConnector", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleRequestStartTransactionAsync(string chargePointId, object payload)
    {
        var startPayload = DeserializePayload<RequestStartTransactionRequest>(payload);

        var jsonRpcPayload = new
        {
            evseId = startPayload.EvseId,
            idToken = startPayload.IdToken,
            remoteStartId = startPayload.RemoteStartId,
            chargingProfile = startPayload.ChargingProfile
        };

        return await SendJsonRpcCommandAsync(chargePointId, "RequestStartTransaction", jsonRpcPayload);
    }

    private async Task<CommandResult> HandleRequestStopTransactionAsync(string chargePointId, object payload)
    {
        var stopPayload = DeserializePayload<RequestStopTransactionRequest>(payload);

        var jsonRpcPayload = new
        {
            transactionId = stopPayload.TransactionId
        };

        return await SendJsonRpcCommandAsync(chargePointId, "RequestStopTransaction", jsonRpcPayload);
    }

    private async Task<CommandResult> SendJsonRpcCommandAsync(string chargePointId, string method, object payload)
    {
        var messageId = Guid.NewGuid().ToString();

        var jsonRpcMessage = new
        {
            jsonrpc = "2.0",
            id = messageId,
            method,
            @params = payload
        };

        var json = System.Text.Json.JsonSerializer.Serialize(jsonRpcMessage);

        // Сохраняем команду как ожидающую ответа
        await _pendingCommands.AddAsync(new PendingCommand
        {
            MessageId = messageId,
            ChargePointId = chargePointId,
            Action = method,
            Payload = payload,
            SentAt = DateTime.UtcNow
        });

        // Отправляем команду
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation(
            "Sent OCPP 2.0 command {Method} to {ChargePointId} with messageId {MessageId}",
            method, chargePointId, messageId);

        return new CommandResult
        {
            Success = true,
            Response = new { messageId }
        };
    }

    private T DeserializePayload<T>(object payload)
    {
        if (payload is T typedPayload)
            return typedPayload;

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json)
               ?? throw new ArgumentException($"Invalid payload for {typeof(T).Name}");
    }

    // DTO классы для команд OCPP 2.0
    private class ResetRequest
    {
        public string Type { get; set; } = "Soft"; // "Hard", "Soft", "SoftOnIdle"
    }

    private class SetChargingProfileRequest
    {
        public int EvseId { get; set; }
        public object ChargingProfile { get; set; } = new();
    }

    private class ClearChargingProfileRequest
    {
        public int? ChargingProfileId { get; set; }
        public string? ChargingProfilePurpose { get; set; }
        public int? EvseId { get; set; }
    }

    private class ChangeAvailabilityRequest
    {
        public string OperationalStatus { get; set; } = "Operative"; // "Inoperative", "Operative"
        public int? EvseId { get; set; }
    }

    private class TriggerMessageRequest
    {
        public string RequestedMessage { get; set; } = string.Empty;
        public int? EvseId { get; set; }
    }

    private class GetLogRequest
    {
        public string LogType { get; set; } = string.Empty;
        public int RequestId { get; set; }
        public string RemoteLocation { get; set; } = string.Empty;
        public DateTime? OldestTimestamp { get; set; }
        public DateTime? LatestTimestamp { get; set; }
    }

    private class SetVariablesRequest
    {
        public SetVariableData[] SetVariableData { get; set; } = Array.Empty<SetVariableData>();
    }

    private class SetVariableData
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
        public string VariableName { get; set; } = string.Empty;
        public string AttributeType { get; set; } = "Actual";
        public string AttributeValue { get; set; } = string.Empty;
    }

    private class GetVariablesRequest
    {
        public GetVariableData[] GetVariableData { get; set; } = Array.Empty<GetVariableData>();
    }

    private class GetVariableData
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
        public string VariableName { get; set; } = string.Empty;
        public string AttributeType { get; set; } = "Actual";
    }

    private class GetBaseReportRequest
    {
        public int RequestId { get; set; }
        public string ReportBase { get; set; } = "FullInventory"; // "ConfigurationInventory", "FullInventory", "SummaryInventory"
    }

    private class GetReportRequest
    {
        public int RequestId { get; set; }
        public ComponentCriteria[]? ComponentCriteria { get; set; }
        public ComponentVariable[]? ComponentVariable { get; set; }
    }

    private class ComponentCriteria
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
    }

    private class ComponentVariable
    {
        public string ComponentName { get; set; } = string.Empty;
        public string? ComponentInstance { get; set; }
        public string VariableName { get; set; } = string.Empty;
    }

    private class SetMonitoringLevelRequest
    {
        public int Severity { get; set; } = 0; // 0-9
    }

    private class SetMonitoringBaseRequest
    {
        public string MonitoringBase { get; set; } = "All"; // "All", "FactoryDefault", "HardWiredOnly"
    }

    private class GetMonitoringReportRequest
    {
        public int RequestId { get; set; }
        public ComponentVariable[]? ComponentVariable { get; set; }
        public string[]? MonitoringCriteria { get; set; }
    }

    private class ClearVariableMonitoringRequest
    {
        public int[] Ids { get; set; } = Array.Empty<int>();
    }

    private class CustomerInformationRequest
    {
        public int RequestId { get; set; }
        public bool Report { get; set; }
        public bool Clear { get; set; }
        public string? CustomerCertificate { get; set; }
        public IdToken? IdToken { get; set; }
        public string? CustomerIdentifier { get; set; }
    }

    private class GetCompositeScheduleRequest
    {
        public int Duration { get; set; }
        public int EvseId { get; set; }
        public string ChargingRateUnit { get; set; } = "A"; // "A", "W"
    }

    private class UnlockConnectorRequest
    {
        public int EvseId { get; set; }
    }

    private class RequestStartTransactionRequest
    {
        public int EvseId { get; set; }
        public IdToken IdToken { get; set; } = new();
        public int RemoteStartId { get; set; }
        public object? ChargingProfile { get; set; }
    }

    private class RequestStopTransactionRequest
    {
        public string TransactionId { get; set; } = string.Empty;
    }

    private class IdToken
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Central", "eMAID", "ISO14443", "ISO15693", "KeyCode", "Local", "NoAuthorization", "Remote"
        public AdditionalInfo[]? AdditionalInfo { get; set; }
    }

    private class AdditionalInfo
    {
        public string AdditionalIdToken { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}