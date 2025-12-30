using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Services.CommandHandlers;

public interface ICommandHandler
{
    Task<CommandResult> HandleAsync(string chargePointId, ServerCommand command);
    bool CanHandle(string action);
}

public class ServerCommand
{
    public string Action { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public object Payload { get; set; } = new object();
    public string ProtocolVersion { get; set; } = "1.6";
}

public class CommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Response { get; set; }
}

// Обработчик команд OCPP 1.6
public class OCPP16CommandHandler : ICommandHandler
{
    private readonly Dictionary<string, Func<string, object, Task<CommandResult>>> _handlers;
    private readonly ILogger<OCPP16CommandHandler> _logger;
    private readonly IChargePointConnectionStorage _connectionStorage;
    private readonly IOCPPService _ocppService;

    public OCPP16CommandHandler(
        ILogger<OCPP16CommandHandler> logger,
        IChargePointConnectionStorage connectionStorage,
        IOCPPService ocppService)
    {
        _logger = logger;
        _connectionStorage = connectionStorage;
        _ocppService = ocppService;

        _handlers = new Dictionary<string, Func<string, object, Task<CommandResult>>>
        {
            ["Reset"] = HandleResetAsync,
            ["UnlockConnector"] = HandleUnlockConnectorAsync,
            ["SetChargingProfile"] = HandleSetChargingProfileAsync,
            ["ClearChargingProfile"] = HandleClearChargingProfileAsync,
            ["RemoteStartTransaction"] = HandleRemoteStartTransactionAsync,
            ["RemoteStopTransaction"] = HandleRemoteStopTransactionAsync,
            ["ChangeAvailability"] = HandleChangeAvailabilityAsync,
            ["ChangeConfiguration"] = HandleChangeConfigurationAsync,
            ["ClearCache"] = HandleClearCacheAsync,
            ["GetDiagnostics"] = HandleGetDiagnosticsAsync,
            ["UpdateFirmware"] = HandleUpdateFirmwareAsync,
            ["GetConfiguration"] = HandleGetConfigurationAsync,
            ["TriggerMessage"] = HandleTriggerMessageAsync,
            ["DataTransfer"] = HandleDataTransferAsync
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
                ErrorMessage = $"Unsupported command: {command.Action}"
            };
        }

        try
        {
            return await handler(chargePointId, command.Payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling command {Action} for {ChargePointId}",
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
        var resetPayload = DeserializePayload<ResetPayload>(payload);

        // Проверяем подключение
        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        // Отправляем команду Reset
        var message = new object[]
        {
            2, // MessageTypeId для Call
            Guid.NewGuid().ToString(),
            "Reset",
            new
            {
                type = resetPayload.Type // "Hard" или "Soft"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("Reset command sent to {ChargePointId} with type {Type}",
            chargePointId, resetPayload.Type);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleUnlockConnectorAsync(string chargePointId, object payload)
    {
        var unlockPayload = DeserializePayload<UnlockConnectorPayload>(payload);

        // Используем OCPPService для отправки команды
        var success = await _ocppService.UnlockConnectorAsync(
            chargePointId, unlockPayload.ConnectorId);

        return new CommandResult
        {
            Success = success,
            ErrorMessage = success ? null : "Failed to send unlock command"
        };
    }

    private async Task<CommandResult> HandleSetChargingProfileAsync(string chargePointId, object payload)
    {
        var profilePayload = DeserializePayload<SetChargingProfilePayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "SetChargingProfile",
            new
            {
                connectorId = profilePayload.ConnectorId,
                csChargingProfiles = profilePayload.ChargingProfile
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("SetChargingProfile command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleClearChargingProfileAsync(string chargePointId, object payload)
    {
        var clearPayload = DeserializePayload<ClearChargingProfilePayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "ClearChargingProfile",
            new
            {
                id = clearPayload.ProfileId,
                connectorId = clearPayload.ConnectorId,
                chargingProfilePurpose = clearPayload.Purpose,
                stackLevel = clearPayload.StackLevel
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("ClearChargingProfile command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleRemoteStartTransactionAsync(string chargePointId, object payload)
    {
        var startPayload = DeserializePayload<RemoteStartTransactionPayload>(payload);

        // Используем OCPPService для отправки команды
        var success = await _ocppService.RemoteStartTransactionAsync(
            chargePointId,
            startPayload.ConnectorId,
            startPayload.IdTag,
            startPayload.ChargingProfileId);

        return new CommandResult
        {
            Success = success,
            ErrorMessage = success ? null : "Failed to send remote start command"
        };
    }

    private async Task<CommandResult> HandleRemoteStopTransactionAsync(string chargePointId, object payload)
    {
        var stopPayload = DeserializePayload<RemoteStopTransactionPayload>(payload);

        // Используем OCPPService для отправки команды
        var success = await _ocppService.RemoteStopTransactionAsync(stopPayload.TransactionId);

        return new CommandResult
        {
            Success = success,
            ErrorMessage = success ? null : "Failed to send remote stop command"
        };
    }

    private async Task<CommandResult> HandleChangeAvailabilityAsync(string chargePointId, object payload)
    {
        var availabilityPayload = DeserializePayload<ChangeAvailabilityPayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "ChangeAvailability",
            new
            {
                connectorId = availabilityPayload.ConnectorId,
                type = availabilityPayload.Type // "Inoperative" или "Operative"
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("ChangeAvailability command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleChangeConfigurationAsync(string chargePointId, object payload)
    {
        var configPayload = DeserializePayload<ChangeConfigurationPayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "ChangeConfiguration",
            new
            {
                key = configPayload.Key,
                value = configPayload.Value
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("ChangeConfiguration command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleClearCacheAsync(string chargePointId, object payload)
    {
        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "ClearCache",
            new { }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("ClearCache command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleGetDiagnosticsAsync(string chargePointId, object payload)
    {
        var diagnosticsPayload = DeserializePayload<GetDiagnosticsPayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "GetDiagnostics",
            new
            {
                location = diagnosticsPayload.Location,
                retries = diagnosticsPayload.Retries,
                retryInterval = diagnosticsPayload.RetryInterval,
                startTime = diagnosticsPayload.StartTime?.ToString("o"),
                stopTime = diagnosticsPayload.StopTime?.ToString("o")
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("GetDiagnostics command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleUpdateFirmwareAsync(string chargePointId, object payload)
    {
        var firmwarePayload = DeserializePayload<UpdateFirmwarePayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "UpdateFirmware",
            new
            {
                location = firmwarePayload.Location,
                retrieveDate = firmwarePayload.RetrieveDate.ToString("o"),
                retries = firmwarePayload.Retries,
                retryInterval = firmwarePayload.RetryInterval
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("UpdateFirmware command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleGetConfigurationAsync(string chargePointId, object payload)
    {
        var configPayload = DeserializePayload<GetConfigurationPayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "GetConfiguration",
            new
            {
                key = configPayload.Keys
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("GetConfiguration command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleTriggerMessageAsync(string chargePointId, object payload)
    {
        var triggerPayload = DeserializePayload<TriggerMessagePayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "TriggerMessage",
            new
            {
                requestedMessage = triggerPayload.RequestedMessage,
                connectorId = triggerPayload.ConnectorId
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("TriggerMessage command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private async Task<CommandResult> HandleDataTransferAsync(string chargePointId, object payload)
    {
        var dataPayload = DeserializePayload<DataTransferCommandPayload>(payload);

        if (!await _connectionStorage.IsConnectedAsync(chargePointId))
        {
            return new CommandResult
            {
                Success = false,
                ErrorMessage = $"Charge point {chargePointId} is not connected"
            };
        }

        var message = new object[]
        {
            2,
            Guid.NewGuid().ToString(),
            "DataTransfer",
            new
            {
                vendorId = dataPayload.VendorId,
                messageId = dataPayload.MessageId,
                data = dataPayload.Data
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        await _connectionStorage.SendMessageAsync(chargePointId, json);

        _logger.LogInformation("DataTransfer command sent to {ChargePointId}", chargePointId);

        return new CommandResult { Success = true };
    }

    private T DeserializePayload<T>(object payload)
    {
        if (payload is T typedPayload)
            return typedPayload;

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        return System.Text.Json.JsonSerializer.Deserialize<T>(json)
               ?? throw new ArgumentException($"Invalid payload for {typeof(T).Name}");
    }

    // DTO классы для команд
    private class ResetPayload
    {
        public string Type { get; set; } = "Soft"; // "Hard" или "Soft"
    }

    private class UnlockConnectorPayload
    {
        public int ConnectorId { get; set; }
    }

    private class SetChargingProfilePayload
    {
        public int ConnectorId { get; set; }
        public object ChargingProfile { get; set; } = new();
    }

    private class ClearChargingProfilePayload
    {
        public int? ProfileId { get; set; }
        public int? ConnectorId { get; set; }
        public string? Purpose { get; set; }
        public int? StackLevel { get; set; }
    }

    private class RemoteStartTransactionPayload
    {
        public int ConnectorId { get; set; }
        public string IdTag { get; set; } = string.Empty;
        public int? ChargingProfileId { get; set; }
    }

    private class RemoteStopTransactionPayload
    {
        public string TransactionId { get; set; } = string.Empty;
    }

    private class ChangeAvailabilityPayload
    {
        public int ConnectorId { get; set; }
        public string Type { get; set; } = "Operative"; // "Inoperative" или "Operative"
    }

    private class ChangeConfigurationPayload
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private class GetDiagnosticsPayload
    {
        public string Location { get; set; } = string.Empty;
        public int? Retries { get; set; }
        public int? RetryInterval { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? StopTime { get; set; }
    }

    private class UpdateFirmwarePayload
    {
        public string Location { get; set; } = string.Empty;
        public DateTime RetrieveDate { get; set; }
        public int? Retries { get; set; }
        public int? RetryInterval { get; set; }
    }

    private class GetConfigurationPayload
    {
        public string[]? Keys { get; set; }
    }

    private class TriggerMessagePayload
    {
        public string RequestedMessage { get; set; } = string.Empty; // "BootNotification", "DiagnosticsStatusNotification", etc.
        public int? ConnectorId { get; set; }
    }

    private class DataTransferCommandPayload
    {
        public string VendorId { get; set; } = string.Empty;
        public string? MessageId { get; set; }
        public string? Data { get; set; }
    }
}


// Фабрика обработчиков команд
public interface ICommandHandlerFactory
{
    ICommandHandler GetHandler(string protocolVersion);
}

public class CommandHandlerFactory : ICommandHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CommandHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommandHandler GetHandler(string protocolVersion)
    {
        return protocolVersion switch
        {
            "1.6" => _serviceProvider.GetRequiredService<OCPP16CommandHandler>(),
            "2.0" => _serviceProvider.GetRequiredService<OCPP20CommandHandler>(),
            "2.1" => _serviceProvider.GetRequiredService<OCPP20CommandHandler>(), // Для 2.1 пока используем 2.0 обработчик
            _ => throw new NotSupportedException($"Unsupported protocol version: {protocolVersion}")
        };
    }
}