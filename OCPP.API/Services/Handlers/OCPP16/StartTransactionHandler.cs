using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

public class StartTransactionHandler : IMessageHandler
    {
        private readonly ILogger<StartTransactionHandler> _logger;
        private readonly IOCPPService _ocppService;
        
        public StartTransactionHandler(
            ILogger<StartTransactionHandler> logger,
            IOCPPService ocppService)
        {
            _logger = logger;
            _ocppService = ocppService;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogInformation("Processing StartTransaction for {ChargePointId}", chargePointId);
            
            var jsonElement = message as JsonElement?;
            if (!jsonElement.HasValue)
                throw new ArgumentException("Invalid message format");
            
            var payload = jsonElement.Value[3];
            
            var connectorId = GetIntValue(payload, "connectorId");
            var idTag = GetStringValue(payload, "idTag");
            var meterStart = GetDoubleValue(payload, "meterStart");
            var reservationId = GetIntValue(payload, "reservationId", null);
            var timestamp = GetDateTimeValue(payload, "timestamp");
            
            // Обрабатываем через OCPPService
            var result = await _ocppService.StartTransactionAsync(
                chargePointId, connectorId, idTag, meterStart);
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to start transaction for {ChargePointId}: {Error}", 
                    chargePointId, result.ErrorMessage);
            }
            
            return new
            {
                transactionId = result.TransactionId,
                idTagInfo = new
                {
                    status = result.IdTagInfo ?? "Accepted",
                    expiryDate = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            };
        }
        
        private int GetIntValue(JsonElement element, string propertyName, int? defaultValue = 0)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetInt32() 
                : defaultValue ?? 0;
        }
        
        private double GetDoubleValue(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetDouble() 
                : 0.0;
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