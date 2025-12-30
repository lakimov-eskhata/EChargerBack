using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCPP.API.Services;

namespace OCPP.API.Controllers;

 [ApiController]
    [Route("api/ocpp/{chargePointId}/commands")]
    public class OCPPCommandsController : ControllerBase
    {
        private readonly ICommandService _commandService;
        private readonly ILogger<OCPPCommandsController> _logger;
        private readonly IChargePointConnectionStorage _connectionStorage;
        private readonly IChargePointRepository _chargePointRepository;
        
        public OCPPCommandsController(
            ICommandService commandService,
            ILogger<OCPPCommandsController> logger,
            IChargePointConnectionStorage connectionStorage,
            IChargePointRepository chargePointRepository)
        {
            _commandService = commandService;
            _logger = logger;
            _connectionStorage = connectionStorage;
            _chargePointRepository = chargePointRepository;
        }
        
        [HttpGet("status")]
        public async Task<IActionResult> GetChargePointStatus([FromRoute] string chargePointId)
        {
            var connection = await _connectionStorage.GetConnectionAsync(chargePointId);
            var chargePoint = await _chargePointRepository.GetByIdAsync(chargePointId);
            
            if (chargePoint == null)
            {
                return NotFound(new { Message = $"Charge point {chargePointId} not found" });
            }
            
            var isConnected = connection != null && connection.IsActive;
            
            return Ok(new
            {
                ChargePointId = chargePointId,
                ProtocolVersion = chargePoint.ProtocolVersion,
                Status = chargePoint.Status,
                IsWebSocketConnected = isConnected,
                ConnectionInfo = isConnected ? new
                {
                    ConnectedSince = connection!.ConnectedAt,
                    LastActivity = connection.LastActivity,
                    RemoteIp = connection.RemoteIpAddress
                } : null,
                ChargePointInfo = new
                {
                    chargePoint.Vendor,
                    chargePoint.Model,
                    chargePoint.SerialNumber,
                    chargePoint.FirmwareVersion,
                    chargePoint.LastBootTime,
                    chargePoint.LastHeartbeat,
                    ConnectorCount = chargePoint.ConnectorCount
                }
            });
        }
        
        [HttpPost("send")]
        public async Task<IActionResult> SendCommand(
            [FromRoute] string chargePointId,
            [FromBody] SendCommandRequest request)
        {
            var result = await _commandService.SendCommandAsync(
                chargePointId, request.Action, request.Payload);
            
            if (!result.Success)
            {
                return BadRequest(new 
                { 
                    Success = false,
                    Error = result.ErrorMessage,
                    Timestamp = DateTime.UtcNow
                });
            }
            
            return Ok(new 
            { 
                Success = true,
                Message = "Command sent successfully",
                Data = result.Response,
                Timestamp = DateTime.UtcNow
            });
        }
        
        [HttpGet("supported-commands")]
        public async Task<IActionResult> GetSupportedCommands([FromRoute] string chargePointId)
        {
            var chargePoint = await _chargePointRepository.GetByIdAsync(chargePointId);
            if (chargePoint == null)
            {
                return NotFound(new { Message = $"Charge point {chargePointId} not found" });
            }
            
            var protocolVersion = chargePoint.ProtocolVersion;
            var commands = GetSupportedCommandsByProtocol(protocolVersion);
            
            return Ok(new
            {
                ChargePointId = chargePointId,
                ProtocolVersion = protocolVersion,
                SupportedCommands = commands,
                IsConnected = await _connectionStorage.IsConnectedAsync(chargePointId)
            });
        }
        
        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastCommand(
            [FromBody] BroadcastCommandRequest request)
        {
            try
            {
                var sentCount = 0;
                var failedChargePoints = new List<string>();
                
                // Получаем все подключенные станции
                var connections = await _connectionStorage.GetAllConnectionsAsync();
                var filteredConnections = connections
                    .Where(c => request.ProtocolVersion == null || c.ProtocolVersion == request.ProtocolVersion)
                    .ToList();
                
                foreach (var connection in filteredConnections)
                {
                    try
                    {
                        var result = await _commandService.SendCommandAsync(
                            connection.ChargePointId, request.Action, request.Payload);
                        
                        if (result.Success)
                        {
                            sentCount++;
                        }
                        else
                        {
                            failedChargePoints.Add(connection.ChargePointId);
                            _logger.LogWarning(
                                "Failed to send broadcast command to {ChargePointId}: {Error}",
                                connection.ChargePointId, result.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedChargePoints.Add(connection.ChargePointId);
                        _logger.LogError(ex, "Error sending broadcast command to {ChargePointId}", 
                            connection.ChargePointId);
                    }
                }
                
                return Ok(new
                {
                    Success = true,
                    Message = $"Broadcast completed. Sent to {sentCount} charge points.",
                    Details = new
                    {
                        TotalTargets = filteredConnections.Count,
                        SuccessfullySent = sentCount,
                        Failed = filteredConnections.Count - sentCount,
                        FailedChargePoints = failedChargePoints,
                        ProtocolVersionFilter = request.ProtocolVersion
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting command");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        
        private Dictionary<string, string[]> GetSupportedCommandsByProtocol(string protocolVersion)
        {
            return protocolVersion switch
            {
                "1.6" => new Dictionary<string, string[]>
                {
                    ["Transaction Commands"] = new[] { "RemoteStartTransaction", "RemoteStopTransaction" },
                    ["Connector Commands"] = new[] { "UnlockConnector", "ChangeAvailability" },
                    ["Management Commands"] = new[] { "Reset", "ChangeConfiguration", "ClearCache", "GetConfiguration" },
                    ["Diagnostics Commands"] = new[] { "GetDiagnostics", "UpdateFirmware" },
                    ["Utility Commands"] = new[] { "TriggerMessage", "DataTransfer" }
                },
                "2.0" or "2.1" => new Dictionary<string, string[]>
                {
                    ["Transaction Commands"] = new[] { "RequestStartTransaction", "RequestStopTransaction" },
                    ["Connector Commands"] = new[] { "UnlockConnector", "ChangeAvailability" },
                    ["Charging Profile Commands"] = new[] { "SetChargingProfile", "ClearChargingProfile", "GetCompositeSchedule" },
                    ["Configuration Commands"] = new[] { "Reset", "GetVariables", "SetVariables", "GetBaseReport", "GetReport" },
                    ["Monitoring Commands"] = new[] { "SetMonitoringLevel", "SetMonitoringBase", "GetMonitoringReport", "ClearVariableMonitoring" },
                    ["Diagnostics Commands"] = new[] { "GetLog", "TriggerMessage" },
                    ["Security Commands"] = new[] { "CustomerInformation" },
                    ["Utility Commands"] = new[] { "DataTransfer", "Custom" }
                },
                _ => new Dictionary<string, string[]>
                {
                    ["Basic Commands"] = new[] { "Reset", "UnlockConnector", "RemoteStartTransaction", "RemoteStopTransaction" }
                }
            };
        }
        
        // Request DTOs
        
        public class SendCommandRequest
        {
            public string Action { get; set; } = string.Empty;
            public object Payload { get; set; } = new object();
        }
        
        public class BroadcastCommandRequest
        {
            public string Action { get; set; } = string.Empty;
            public object Payload { get; set; } = new object();
            public string? ProtocolVersion { get; set; } // null для всех версий
        }
    }