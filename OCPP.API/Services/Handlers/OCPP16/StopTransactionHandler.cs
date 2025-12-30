using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

public class StopTransactionHandler : IMessageHandler
    {
        private readonly ILogger<StopTransactionHandler> _logger;
        private readonly IOCPPService _ocppService;
        
        public StopTransactionHandler(
            ILogger<StopTransactionHandler> logger,
            IOCPPService ocppService)
        {
            _logger = logger;
            _ocppService = ocppService;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogInformation("Processing StopTransaction for {ChargePointId}", chargePointId);
            
            var jsonElement = message as JsonElement?;
            if (!jsonElement.HasValue)
                throw new ArgumentException("Invalid message format");
            
            var payload = jsonElement.Value[3];
            
            var transactionId = GetIntValue(payload, "transactionId").ToString();
            var idTag = GetStringValue(payload, "idTag");
            var meterStop = GetDoubleValue(payload, "meterStop");
            var timestamp = GetDateTimeValue(payload, "timestamp");
            var reason = GetStringValue(payload, "reason");
            var transactionData = GetTransactionData(payload);
            
            // Обрабатываем через OCPPService
            var result = await _ocppService.StopTransactionAsync(
                $"TXN-{transactionId}", meterStop, reason);
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to stop transaction {TransactionId}: {Error}", 
                    transactionId, result.ErrorMessage);
            }
            
            return new
            {
                idTagInfo = new
                {
                    status = "Accepted",
                    expiryDate = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            };
        }
        
        private int GetIntValue(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetInt32() 
                : 0;
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
        
        private List<object> GetTransactionData(JsonElement element)
        {
            var result = new List<object>();
            
            if (element.TryGetProperty("transactionData", out var dataArray))
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    // Парсим данные транзакции
                    result.Add(new
                    {
                        timestamp = GetDateTimeValue(item, "timestamp"),
                        value = GetDoubleValue(item, "value")
                    });
                }
            }
            
            return result;
        }
    }