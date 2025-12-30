using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

public class AuthorizeHandler : IMessageHandler
{
    private readonly ILogger<AuthorizeHandler> _logger;
        
    public AuthorizeHandler(ILogger<AuthorizeHandler> logger)
    {
        _logger = logger;
    }
        
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug("Processing Authorize for {ChargePointId}", chargePointId);
            
        var jsonElement = message as JsonElement?;
        if (!jsonElement.HasValue)
            throw new ArgumentException("Invalid message format");
            
        var payload = jsonElement.Value[3];
        var idTag = GetStringValue(payload, "idTag");
            
        // Здесь должна быть логика авторизации через внешний сервис
        // Для примера - всегда разрешаем
        var idTagInfo = new
        {
            status = "Accepted",
            expiryDate = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            parentIdTag = (string?)null
        };
            
        _logger.LogInformation("Authorized idTag {IdTag} for {ChargePointId}", idTag, chargePointId);
            
        return new { idTagInfo };
    }
        
    private string GetStringValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) 
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}