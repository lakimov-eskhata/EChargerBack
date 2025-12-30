using System;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

public class DataTransferHandler : IMessageHandler
{
    private readonly ILogger<DataTransferHandler> _logger;
    private readonly IOCPPService _ocppService;
        
    public DataTransferHandler(
        ILogger<DataTransferHandler> logger,
        IOCPPService ocppService)
    {
        _logger = logger;
        _ocppService = ocppService;
    }
        
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug("Processing DataTransfer for {ChargePointId}", chargePointId);
            
        var jsonElement = message as JsonElement?;
        if (!jsonElement.HasValue)
            throw new ArgumentException("Invalid message format");
            
        var payload = jsonElement.Value[3];
            
        var vendorId = GetStringValue(payload, "vendorId");
        var messageId = GetStringValue(payload, "messageId");
        var data = GetStringValue(payload, "data");
            
        // Обрабатываем через OCPPService
        var result = await _ocppService.ProcessDataTransferAsync(
            chargePointId, vendorId, messageId, data);
            
        return new
        {
            status = result.Status,
            data = result.Data
        };
    }
        
    private string GetStringValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) 
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}