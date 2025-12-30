using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using OCPP.API.Controllers;
using OCPP.API.Services.CommandHandlers;
using OCPP.API.Shared.Models;
using CommandResult = OCPP.API.Services.CommandHandlers.CommandResult;
using ServerCommand = OCPP.API.Services.CommandHandlers.ServerCommand;

namespace OCPP.API.Services;

public interface ICommandService
{
// Базовые методы
        Task<CommandResult> SendCommandAsync(string chargePointId, ServerCommand command);
        Task<CommandResult> SendCommandAsync(string chargePointId, string action, object payload);
        
        // Общие команды для всех версий OCPP
        Task<CommandResult> ResetChargePointAsync(string chargePointId, string resetType = "Soft");
        Task<CommandResult> UnlockConnectorAsync(string chargePointId, int connectorId);
        Task<CommandResult> RemoteStartTransactionAsync(string chargePointId, int connectorId, string idTag);
        Task<CommandResult> RemoteStopTransactionAsync(string chargePointId, string transactionId);
        Task<CommandResult> SetChargingProfileAsync(string chargePointId, int connectorId, object chargingProfile);
        Task<CommandResult> ClearChargingProfileAsync(string chargePointId, int? connectorId = null, int? profileId = null);
        
        // OCPP 2.0/2.1 специфичные команды
        Task<CommandResult> GetVariablesAsync(string chargePointId, IEnumerable<GetVariableRequest> variables);
        Task<CommandResult> SetVariablesAsync(string chargePointId, IEnumerable<SetVariableRequest> variables);
        Task<CommandResult> GetBaseReportAsync(string chargePointId, int requestId, string reportBase);
        Task<CommandResult> GetReportAsync(
            string chargePointId, 
            int requestId, 
            IEnumerable<ComponentCriteria>? componentCriteria = null,
            IEnumerable<ComponentVariable>? componentVariable = null);
        Task<CommandResult> GetLogAsync(
            string chargePointId, 
            string logType, 
            int requestId, 
            string remoteLocation,
            DateTime? oldestTimestamp = null,
            DateTime? latestTimestamp = null);
        Task<CommandResult> SetMonitoringLevelAsync(string chargePointId, int severity);
        Task<CommandResult> GetMonitoringReportAsync(
            string chargePointId, 
            int requestId,
            IEnumerable<ComponentVariable>? componentVariable = null,
            IEnumerable<string>? monitoringCriteria = null);
        Task<CommandResult> ClearVariableMonitoringAsync(string chargePointId, IEnumerable<int> ids);
        Task<CommandResult> CustomerInformationAsync(
            string chargePointId, 
            int requestId, 
            bool report, 
            bool clear,
            string? customerCertificate = null,
            IdTokenClass? idToken = null,
            string? customerIdentifier = null);
        Task<CommandResult> GetCompositeScheduleAsync(
            string chargePointId, 
            int duration, 
            int evseId, 
            string chargingRateUnit = "A");
        Task<CommandResult> ChangeAvailabilityAsync(string chargePointId, string operationalStatus, int? evseId = null);
        Task<CommandResult> TriggerMessageAsync(string chargePointId, string requestedMessage, int? evseId = null);
        
        // // Вспомогательные методы
        Task<string> GetProtocolVersionAsync(string chargePointId);
        // Task<bool> IsChargePointConnectedAsync(string chargePointId);
        // Task<IEnumerable<string>> GetConnectedChargePointsAsync(string? protocolVersion = null);
}

public class CommandService : ICommandService
{
    private readonly ILogger<CommandService> _logger;
    private readonly ICommandHandlerFactory _handlerFactory;
    private readonly IChargePointConnectionStorage _connectionStorage;
    private readonly IChargePointRepository _chargePointRepository;

    public CommandService(
        ILogger<CommandService> logger,
        ICommandHandlerFactory handlerFactory,
        IChargePointConnectionStorage connectionStorage,
        IChargePointRepository chargePointRepository)
    {
        _logger = logger;
        _handlerFactory = handlerFactory;
        _connectionStorage = connectionStorage;
        _chargePointRepository = chargePointRepository;
    }

    public async Task<CommandResult> SendCommandAsync(string chargePointId, ServerCommand command)
    {
        try
        {
            // Проверяем подключение станции
            var isConnected = await _connectionStorage.IsConnectedAsync(chargePointId);
            if (!isConnected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Charge point {chargePointId} is not connected"
                };
            }

            // Получаем версию протокола станции
            var protocolVersion = await GetProtocolVersionAsync(chargePointId);

            // Получаем обработчик для этой версии протокола
            var handler = _handlerFactory.GetHandler(protocolVersion);

            if (!handler.CanHandle(command.Action))
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = $"Command {command.Action} not supported for protocol {protocolVersion}"
                };
            }

            // Отправляем команду
            _logger.LogInformation(
                "Sending command {Action} to {ChargePointId} via {ProtocolVersion}",
                command.Action, chargePointId, protocolVersion);

            return await handler.HandleAsync(chargePointId, command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command to {ChargePointId}", chargePointId);
            return new CommandResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<CommandResult> SendCommandAsync(string chargePointId, string action, object payload)
    {
        var command = new ServerCommand
        {
            Action = action,
            Payload = payload,
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> ResetChargePointAsync(string chargePointId, string resetType = "Soft")
    {
        var command = new ServerCommand
        {
            Action = "Reset",
            Payload = new { type = resetType },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> UnlockConnectorAsync(string chargePointId, int connectorId)
    {
        var command = new ServerCommand
        {
            Action = "UnlockConnector",
            Payload = new { connectorId },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> RemoteStartTransactionAsync(string chargePointId, int connectorId, string idTag)
    {
        // Определяем правильное название команды в зависимости от версии протокола
        var protocolVersion = await GetProtocolVersionAsync(chargePointId);
        var action = protocolVersion == "1.6" ? "RemoteStartTransaction" : "RequestStartTransaction";

        object payload = protocolVersion == "1.6"
            ? new { connectorId, idTag }
            : new { evseId = connectorId, idToken = new { idToken = idTag, type = "Central" } };

        var command = new ServerCommand
        {
            Action = action,
            Payload = payload,
            MessageId = Guid.NewGuid().ToString(),
            ProtocolVersion = protocolVersion
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> RemoteStopTransactionAsync(string chargePointId, string transactionId)
    {
        var protocolVersion = await GetProtocolVersionAsync(chargePointId);
        var action = protocolVersion == "1.6" ? "RemoteStopTransaction" : "RequestStopTransaction";

        var command = new ServerCommand
        {
            Action = action,
            Payload = new { transactionId },
            MessageId = Guid.NewGuid().ToString(),
            ProtocolVersion = protocolVersion
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> SetChargingProfileAsync(string chargePointId, int connectorId, object chargingProfile)
    {
        var command = new ServerCommand
        {
            Action = "SetChargingProfile",
            Payload = new { connectorId, csChargingProfiles = chargingProfile },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> ClearChargingProfileAsync(string chargePointId, int? connectorId = null, int? profileId = null)
    {
        var command = new ServerCommand
        {
            Action = "ClearChargingProfile",
            Payload = new { id = profileId, connectorId },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> GetVariablesAsync(string chargePointId, IEnumerable<GetVariableRequest> variables)
    {
        var command = new ServerCommand
        {
            Action = "GetVariables",
            Payload = new { getVariableData = variables },
            MessageId = Guid.NewGuid().ToString()
        };
            
        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> SetVariablesAsync(string chargePointId, IEnumerable<SetVariableRequest> variables)
    {
        var command = new ServerCommand
        {
            Action = "SetVariables",
            Payload = new { setVariableData = variables },
            MessageId = Guid.NewGuid().ToString()
        };
            
        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> GetBaseReportAsync(
        string chargePointId,
        int requestId,
        string reportBase)
    {
        var command = new ServerCommand
        {
            Action = "GetBaseReport",
            Payload = new { requestId, reportBase },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }
    

    public async Task<CommandResult> GetReportAsync(
        string chargePointId,
        int requestId,
        IEnumerable<ComponentCriteria>? componentCriteria = null,
        IEnumerable<ComponentVariable>? componentVariable = null)
    {
        var command = new ServerCommand
        {
            Action = "GetReport",
            Payload = new { requestId, componentCriteria, componentVariable },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> GetLogAsync(
        string chargePointId,
        string logType,
        int requestId,
        string remoteLocation,
        DateTime? oldestTimestamp = null,
        DateTime? latestTimestamp = null)
    {
        var command = new ServerCommand
        {
            Action = "GetLog",
            Payload = new
            {
                logType,
                requestId,
                log = new
                {
                    remoteLocation,
                    oldestTimestamp = oldestTimestamp?.ToString("o"),
                    latestTimestamp = latestTimestamp?.ToString("o")
                }
            },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> SetMonitoringLevelAsync(
        string chargePointId,
        int severity)
    {
        var command = new ServerCommand
        {
            Action = "SetMonitoringLevel",
            Payload = new { severity },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }
    
    public async Task<CommandResult> GetMonitoringReportAsync(
        string chargePointId,
        int requestId,
        IEnumerable<ComponentVariable>? componentVariable = null,
        IEnumerable<string>? monitoringCriteria = null)
    {
        var command = new ServerCommand
        {
            Action = "GetMonitoringReport",
            Payload = new { requestId, componentVariable, monitoringCriteria },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> ClearVariableMonitoringAsync(string chargePointId, IEnumerable<int> ids)
    {
        var command = new ServerCommand
        {
            Action = "ClearVariableMonitoring",
            Payload = new { id = ids },
            MessageId = Guid.NewGuid().ToString()
        };
            
        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> CustomerInformationAsync(
        string chargePointId,
        int requestId,
        bool report,
        bool clear,
        string? customerCertificate = null,
        IdTokenClass? idToken = null,
        string? customerIdentifier = null)
    {
        var command = new ServerCommand
        {
            Action = "CustomerInformation",
            Payload = new
            {
                requestId,
                report,
                clear,
                customerCertificate,
                idToken,
                customerIdentifier
            },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<CommandResult> GetCompositeScheduleAsync(
        string chargePointId,
        int duration,
        int evseId,
        string chargingRateUnit = "A")
    {
        var command = new ServerCommand
        {
            Action = "GetCompositeSchedule",
            Payload = new { duration, evseId, chargingRateUnit },
            MessageId = Guid.NewGuid().ToString()
        };

        return await SendCommandAsync(chargePointId, command);
    }
    
    public async Task<CommandResult> ChangeAvailabilityAsync(string chargePointId, string operationalStatus, int? evseId = null)
    {
        var command = new ServerCommand
        {
            Action = "ChangeAvailability",
            Payload = new 
            { 
                operationalStatus,
                evse = evseId.HasValue ? new { id = evseId.Value } : null
            },
            MessageId = Guid.NewGuid().ToString()
        };
            
        return await SendCommandAsync(chargePointId, command);
    }
        
    public async Task<CommandResult> TriggerMessageAsync(string chargePointId, string requestedMessage, int? evseId = null)
    {
        var command = new ServerCommand
        {
            Action = "TriggerMessage",
            Payload = new 
            { 
                requestedMessage,
                evse = evseId.HasValue ? new { id = evseId.Value } : null
            },
            MessageId = Guid.NewGuid().ToString()
        };
            
        return await SendCommandAsync(chargePointId, command);
    }

    public async Task<bool> IsChargePointConnectedAsync(string chargePointId)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetProtocolVersionAsync(string chargePointId)
    {
        try
        {
            // Пытаемся получить из подключенных станций
            var connection = await _connectionStorage.GetConnectionAsync(chargePointId);
            if (connection != null && !string.IsNullOrEmpty(connection.ProtocolVersion))
            {
                return connection.ProtocolVersion;
            }

            // Если не найдено в подключениях, ищем в базе данных
            var chargePoint = await _chargePointRepository.GetByIdAsync(chargePointId);
            if (chargePoint != null && !string.IsNullOrEmpty(chargePoint.ProtocolVersion))
            {
                return chargePoint.ProtocolVersion;
            }

            // По умолчанию используем OCPP 1.6
            return "1.6";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting protocol version for {ChargePointId}, using default 1.6", chargePointId);
            return "1.6";
        }
    }
}