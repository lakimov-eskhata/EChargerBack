using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

public class MeterValuesHandler : IMessageHandler
    {
        private readonly ILogger<MeterValuesHandler> _logger;
        private readonly IOCPPService _ocppService;
        
        public MeterValuesHandler(
            ILogger<MeterValuesHandler> logger,
            IOCPPService ocppService)
        {
            _logger = logger;
            _ocppService = ocppService;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogDebug("Processing MeterValues for {ChargePointId}", chargePointId);
            
            var jsonElement = message as JsonElement?;
            if (!jsonElement.HasValue)
                throw new ArgumentException("Invalid message format");
            
            var payload = jsonElement.Value[3];
            
            var connectorId = GetIntValue(payload, "connectorId");
            var transactionId = GetIntValue(payload, "transactionId", null);
            var meterValueArray = GetMeterValueArray(payload, "meterValue");
            
            foreach (var meterValue in meterValueArray)
            {
                if (transactionId > 0)
                {
                    await ProcessTransactionMeterValue(
                        chargePointId, connectorId, transactionId, meterValue);
                }
                else
                {
                    await ProcessConnectorMeterValue(chargePointId, connectorId, meterValue);
                }
            }
            
            return new { };
        }
        
        private async Task ProcessTransactionMeterValue(
            string chargePointId, int connectorId, int transactionId, JsonElement meterValue)
        {
            var timestamp = GetDateTimeValue(meterValue, "timestamp");
            var sampledValue = GetSampledValue(meterValue, "sampledValue");
            
            if (sampledValue.Value.HasValue)
            {
                // Обновляем значение в транзакции
                await _ocppService.UpdateTransactionMeterValueAsync(
                    $"TXN-{transactionId}", sampledValue.Value.Value);
            }
            
            _logger.LogDebug(
                "Meter value for transaction {TransactionId}: {Value} {Unit}",
                transactionId, sampledValue.Value, sampledValue.Unit);
        }
        
        private async Task ProcessConnectorMeterValue(
            string chargePointId, int connectorId, JsonElement meterValue)
        {
            var timestamp = GetDateTimeValue(meterValue, "timestamp");
            var sampledValue = GetSampledValue(meterValue, "sampledValue");
            
            // Здесь можно обновить значение счетчика на коннекторе
            _logger.LogDebug(
                "Meter value for connector {ConnectorId}: {Value} {Unit}",
                connectorId, sampledValue.Value, sampledValue.Unit);
        }
        
        private int GetIntValue(JsonElement element, string propertyName, int? defaultValue = 0)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetInt32() 
                : defaultValue ?? 0;
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
        
        private JsonElement[] GetMeterValueArray(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                return value.EnumerateArray().ToArray();
            }
            
            return Array.Empty<JsonElement>();
        }
        
        private (double? Value, string Unit) GetSampledValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                var sampledValue = value.EnumerateArray().FirstOrDefault();
                if (sampledValue.ValueKind == JsonValueKind.Object)
                {
                    var sampledValueValue = GetDoubleValue(sampledValue, "value");
                    var unit = GetStringValue(sampledValue, "unit");
                    return (sampledValueValue, unit);
                }
            }
            
            return (null, string.Empty);
        }
        
        private double GetDoubleValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var doubleValue))
                    return doubleValue;
                else if (value.ValueKind == JsonValueKind.Number)
                    return value.GetDouble();
            }
            
            return 0.0;
        }
        
        private string GetStringValue(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) 
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
    }