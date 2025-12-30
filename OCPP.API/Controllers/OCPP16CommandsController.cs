using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCPP.API.Services;
using OCPP.API.Services.CommandHandlers;

namespace OCPP.API.Controllers;

 [ApiController]
    [Route("api/ocpp/{chargePointId}/v16")]
    public class OCPP16CommandsController : ControllerBase
    {
        private readonly ICommandService _commandService;
        private readonly ILogger<OCPP16CommandsController> _logger;
        
        public OCPP16CommandsController(
            ICommandService commandService,
            ILogger<OCPP16CommandsController> logger)
        {
            _commandService = commandService;
            _logger = logger;
        }
        
        [HttpPost("reset")]
        public async Task<IActionResult> Reset(
            [FromRoute] string chargePointId,
            [FromBody] ResetRequest request)
        {
            var result = await _commandService.ResetChargePointAsync(
                chargePointId, request.Type);
            
            return HandleCommandResult(result);
        }
        
        [HttpPost("unlock-connector")]
        public async Task<IActionResult> UnlockConnector(
            [FromRoute] string chargePointId,
            [FromBody] UnlockConnectorRequest request)
        {
            var result = await _commandService.UnlockConnectorAsync(
                chargePointId, request.ConnectorId);
            
            return HandleCommandResult(result);
        }
        
        [HttpPost("remote-start-transaction")]
        public async Task<IActionResult> RemoteStartTransaction(
            [FromRoute] string chargePointId,
            [FromBody] RemoteStartTransactionRequest request)
        {
            var result = await _commandService.RemoteStartTransactionAsync(
                chargePointId, request.ConnectorId, request.IdTag);
            
            return HandleCommandResult(result);
        }
        
        [HttpPost("remote-stop-transaction")]
        public async Task<IActionResult> RemoteStopTransaction(
            [FromRoute] string chargePointId,
            [FromBody] RemoteStopTransactionRequest request)
        {
            var result = await _commandService.RemoteStopTransactionAsync(
                chargePointId, request.TransactionId);
            
            return HandleCommandResult(result);
        }
        
        [HttpPost("change-availability")]
        public async Task<IActionResult> ChangeAvailability(
            [FromRoute] string chargePointId,
            [FromBody] ChangeAvailabilityRequest request)
        {
            var command = new ServerCommand
            {
                Action = "ChangeAvailability",
                Payload = new 
                { 
                    connectorId = request.ConnectorId,
                    type = request.Type
                },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("change-configuration")]
        public async Task<IActionResult> ChangeConfiguration(
            [FromRoute] string chargePointId,
            [FromBody] ChangeConfigurationRequest request)
        {
            var command = new ServerCommand
            {
                Action = "ChangeConfiguration",
                Payload = new 
                { 
                    key = request.Key,
                    value = request.Value
                },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("clear-cache")]
        public async Task<IActionResult> ClearCache([FromRoute] string chargePointId)
        {
            var command = new ServerCommand
            {
                Action = "ClearCache",
                Payload = new { },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("get-diagnostics")]
        public async Task<IActionResult> GetDiagnostics(
            [FromRoute] string chargePointId,
            [FromBody] GetDiagnosticsRequest request)
        {
            var command = new ServerCommand
            {
                Action = "GetDiagnostics",
                Payload = new 
                { 
                    location = request.Location,
                    retries = request.Retries,
                    retryInterval = request.RetryInterval,
                    startTime = request.StartTime?.ToString("o"),
                    stopTime = request.StopTime?.ToString("o")
                },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("update-firmware")]
        public async Task<IActionResult> UpdateFirmware(
            [FromRoute] string chargePointId,
            [FromBody] UpdateFirmwareRequest request)
        {
            var command = new ServerCommand
            {
                Action = "UpdateFirmware",
                Payload = new 
                { 
                    location = request.Location,
                    retrieveDate = request.RetrieveDate.ToString("o"),
                    retries = request.Retries,
                    retryInterval = request.RetryInterval
                },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("get-configuration")]
        public async Task<IActionResult> GetConfiguration(
            [FromRoute] string chargePointId,
            [FromBody] GetConfigurationRequest request)
        {
            var command = new ServerCommand
            {
                Action = "GetConfiguration",
                Payload = new { key = request.Keys },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("trigger-message")]
        public async Task<IActionResult> TriggerMessage(
            [FromRoute] string chargePointId,
            [FromBody] TriggerMessageRequest request)
        {
            var command = new ServerCommand
            {
                Action = "TriggerMessage",
                Payload = new 
                { 
                    requestedMessage = request.RequestedMessage,
                    connectorId = request.ConnectorId
                },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("data-transfer")]
        public async Task<IActionResult> DataTransfer(
            [FromRoute] string chargePointId,
            [FromBody] DataTransferRequest request)
        {
            var command = new ServerCommand
            {
                Action = "DataTransfer",
                Payload = new 
                { 
                    vendorId = request.VendorId,
                    messageId = request.MessageId,
                    data = request.Data
                },
                MessageId = Guid.NewGuid().ToString()
            };
            
            var result = await _commandService.SendCommandAsync(chargePointId, command);
            return HandleCommandResult(result);
        }
        
        [HttpPost("custom-command")]
        public async Task<IActionResult> SendCustomCommand(
            [FromRoute] string chargePointId,
            [FromBody] CustomCommandRequest request)
        {
            var result = await _commandService.SendCommandAsync(
                chargePointId, request.Action, request.Payload);
            
            return HandleCommandResult(result);
        }
        
        private IActionResult HandleCommandResult(CommandResult result)
        {
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
        
        // Request DTOs для OCPP 1.6
        
        public class ResetRequest
        {
            [Required]
            [RegularExpression("^(Hard|Soft)$", ErrorMessage = "Type must be 'Hard' or 'Soft'")]
            public string Type { get; set; } = "Soft";
        }
        
        public class UnlockConnectorRequest
        {
            [Required]
            [Range(1, int.MaxValue)]
            public int ConnectorId { get; set; }
        }
        
        public class RemoteStartTransactionRequest
        {
            [Required]
            [Range(1, int.MaxValue)]
            public int ConnectorId { get; set; }
            
            [Required]
            public string IdTag { get; set; } = string.Empty;
            
            public int? ChargingProfileId { get; set; }
        }
        
        public class RemoteStopTransactionRequest
        {
            [Required]
            public string TransactionId { get; set; } = string.Empty;
        }
        
        public class ChangeAvailabilityRequest
        {
            [Required]
            [Range(0, int.MaxValue)]
            public int ConnectorId { get; set; }
            
            [Required]
            [RegularExpression("^(Inoperative|Operative)$", ErrorMessage = "Type must be 'Inoperative' or 'Operative'")]
            public string Type { get; set; } = "Operative";
        }
        
        public class ChangeConfigurationRequest
        {
            [Required]
            public string Key { get; set; } = string.Empty;
            
            [Required]
            public string Value { get; set; } = string.Empty;
        }
        
        public class GetDiagnosticsRequest
        {
            [Required]
            [Url]
            public string Location { get; set; } = string.Empty;
            
            public int? Retries { get; set; }
            public int? RetryInterval { get; set; }
            public DateTime? StartTime { get; set; }
            public DateTime? StopTime { get; set; }
        }
        
        public class UpdateFirmwareRequest
        {
            [Required]
            [Url]
            public string Location { get; set; } = string.Empty;
            
            [Required]
            public DateTime RetrieveDate { get; set; }
            
            public int? Retries { get; set; }
            public int? RetryInterval { get; set; }
        }
        
        public class GetConfigurationRequest
        {
            public string[]? Keys { get; set; }
        }
        
        public class TriggerMessageRequest
        {
            [Required]
            [RegularExpression("^(BootNotification|DiagnosticsStatusNotification|FirmwareStatusNotification|Heartbeat|MeterValues|StatusNotification)$",
                ErrorMessage = "Invalid requested message")]
            public string RequestedMessage { get; set; } = string.Empty;
            
            public int? ConnectorId { get; set; }
        }
        
        public class DataTransferRequest
        {
            [Required]
            public string VendorId { get; set; } = string.Empty;
            
            public string? MessageId { get; set; }
            public string? Data { get; set; }
        }
        
        public class CustomCommandRequest
        {
            [Required]
            public string Action { get; set; } = string.Empty;
            
            [Required]
            public object Payload { get; set; } = new object();
        }
    }