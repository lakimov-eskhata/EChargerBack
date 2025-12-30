using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

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
            _logger.LogDebug("Processing StatusNotification for {ChargePointId}", chargePointId);
            
            var jsonElement = message as JsonElement?;
            if (!jsonElement.HasValue)
                throw new ArgumentException("Invalid message format");
            
            var payload = jsonElement.Value[3];
            
            var connectorId = GetIntValue(payload, "connectorId");
            var status = GetStringValue(payload, "status");
            var errorCode = GetStringValue(payload, "errorCode");
            var info = GetStringValue(payload, "info");
            var timestamp = GetDateTimeValue(payload, "timestamp");
            var vendorId = GetStringValue(payload, "vendorId");
            var vendorErrorCode = GetStringValue(payload, "vendorErrorCode");
            
            // Обрабатываем через OCPPService
            await _ocppService.UpdateConnectorStatusAsync(
                chargePointId, connectorId, status, errorCode, info);
            
            _logger.LogInformation(
                "Updated connector {ConnectorId} status to {Status} on {ChargePointId}",
                connectorId, status, chargePointId);
            
            return new { };
        }
        
        private int GetIntValue(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetInt32() 
                : 0;
        }
        
        private DateTime GetDateTimeValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                if (DateTime.TryParse(value.GetString(), out var dateTime))
                    return dateTime;
            }
            
            return DateTime.UtcNow;
        }
        
        private string GetStringValue(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }