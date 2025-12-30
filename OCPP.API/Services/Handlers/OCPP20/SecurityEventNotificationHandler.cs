using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class SecurityEventNotificationHandler : IMessageHandler
{
    private readonly ILogger<SecurityEventNotificationHandler> _logger;
        
    public SecurityEventNotificationHandler(ILogger<SecurityEventNotificationHandler> logger)
    {
        _logger = logger;
    }
        
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug("Processing OCPP 2.0 SecurityEventNotification for {ChargePointId}", chargePointId);
            
        var jsonRpc = message as JsonRpcMessage;
        if (jsonRpc == null)
            throw new ArgumentException("Invalid message format");
            
        var request = jsonRpc.Params.Deserialize<SecurityEventNotificationRequest>();
            
        // Логируем security event для аудита
        _logger.LogWarning(
            "Security event on {ChargePointId}: Type={Type}, TechInfo={TechInfo}, Timestamp={Timestamp}",
            chargePointId, request.Type, request.TechInfo, request.Timestamp);
            
        // Здесь можно добавить логику обработки security events:
        // 1. Сохранение в базу для аудита
        // 2. Отправка уведомлений администраторам
        // 3. Блокировка станции при серьезных нарушениях
            
        return new SecurityEventNotificationResponse();
    }
        
    private class SecurityEventNotificationRequest
    {
        public string Type { get; set; } = string.Empty; // e.g., "FirmwareUpdated", "FailedToAuthenticateAtCentralSystem"
        public DateTime Timestamp { get; set; }
        public string? TechInfo { get; set; }
    }
        
    private class SecurityEventNotificationResponse
    {
        // Пустой ответ для OCPP 2.0 SecurityEventNotification
    }
}