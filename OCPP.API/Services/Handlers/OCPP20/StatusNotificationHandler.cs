using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class StatusNotificationHandler : IMessageHandler
    {
        private readonly ILogger<StatusNotificationHandler> _logger;
        private readonly IOCPPService _ocppService;
        
        public StatusNotificationHandler(
            ILogger<StatusNotificationHandler> logger,
            IOCPPService ocppService)
        {
            _logger = logger;
            _ocppService = ocppService;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogDebug("Processing OCPP 2.0 StatusNotification for {ChargePointId}", chargePointId);
            
            var jsonRpc = message as JsonRpcMessage;
            if (jsonRpc == null)
                throw new ArgumentException("Invalid message format");
            
            var request = jsonRpc.Params.Deserialize<StatusNotificationRequest>();
            
            await _ocppService.UpdateConnectorStatusAsync(
                chargePointId, 
                request.EvseId, 
                MapStatus(request.ConnectorStatus),
                request.ErrorCode,
                request.Info);
            
            _logger.LogInformation(
                "Updated connector status for {ChargePointId}, EVSE {EvseId}: {Status}",
                chargePointId, request.EvseId, request.ConnectorStatus);
            
            return new StatusNotificationResponse();
        }
        
        private string MapStatus(string connectorStatus)
        {
            // Маппинг статусов OCPP 2.0 -> внутреннее представление
            return connectorStatus switch
            {
                "Available" => "Available",
                "Occupied" => "Occupied",
                "Reserved" => "Reserved",
                "Unavailable" => "Unavailable",
                "Faulted" => "Faulted",
                _ => "Unknown"
            };
        }
        
        private class StatusNotificationRequest
        {
            public DateTime Timestamp { get; set; }
            public string ConnectorStatus { get; set; } = string.Empty; // Available, Occupied, Reserved, Unavailable, Faulted
            public int EvseId { get; set; }
            public string? ErrorCode { get; set; } // ConnectorLockFailure, EVCommunicationError, GroundFailure, etc.
            public string? Info { get; set; }
            public string? VendorId { get; set; }
            public string? VendorErrorCode { get; set; }
        }
        
        private class StatusNotificationResponse
        {
            // Пустой ответ для OCPP 2.0 StatusNotification
        }
    }