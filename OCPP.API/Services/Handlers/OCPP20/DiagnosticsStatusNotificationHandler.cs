using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class DiagnosticsStatusNotificationHandler : IMessageHandler
{
    private readonly ILogger<DiagnosticsStatusNotificationHandler> _logger;
        
    public DiagnosticsStatusNotificationHandler(ILogger<DiagnosticsStatusNotificationHandler> logger)
    {
        _logger = logger;
    }
        
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug("Processing OCPP 2.0 DiagnosticsStatusNotification for {ChargePointId}", chargePointId);
            
        var jsonRpc = message as JsonRpcMessage;
        if (jsonRpc == null)
            throw new ArgumentException("Invalid message format");
            
        var request = jsonRpc.Params.Deserialize<DiagnosticsStatusNotificationRequest>();
            
        _logger.LogInformation(
            "Diagnostics status for {ChargePointId}: Status={Status}, UploadStatus={UploadStatus}",
            chargePointId, request.Status, request.UploadStatus);
            
        // Здесь можно обновить статус диагностики в базе данных
        // и уведомить администраторов о завершении загрузки диагностики
            
        return new DiagnosticsStatusNotificationResponse();
    }
        
    private class DiagnosticsStatusNotificationRequest
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty; // Idle, Uploaded, UploadFailed, Uploading
        public string? UploadStatus { get; set; } // Only present if Status is Uploaded or UploadFailed
    }
        
    private class DiagnosticsStatusNotificationResponse
    {
        // Пустой ответ для OCPP 2.0 DiagnosticsStatusNotification
    }
}