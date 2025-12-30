using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class FirmwareStatusNotificationHandler : IMessageHandler
{
    private readonly ILogger<FirmwareStatusNotificationHandler> _logger;
        
    public FirmwareStatusNotificationHandler(ILogger<FirmwareStatusNotificationHandler> logger)
    {
        _logger = logger;
    }
        
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug("Processing OCPP 2.0 FirmwareStatusNotification for {ChargePointId}", chargePointId);
            
        var jsonRpc = message as JsonRpcMessage;
        if (jsonRpc == null)
            throw new ArgumentException("Invalid message format");
            
        var request = jsonRpc.Params.Deserialize<FirmwareStatusNotificationRequest>();
            
        _logger.LogInformation(
            "Firmware status for {ChargePointId}: Status={Status}",
            chargePointId, request.Status);
            
        // Здесь можно обновить статус прошивки в базе данных
        // и уведомить администраторов о завершении обновления
            
        return new FirmwareStatusNotificationResponse();
    }
        
    private class FirmwareStatusNotificationRequest
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty; // Downloaded, DownloadFailed, InstallationFailed, Installed, InstallRebooting, InstallScheduled, InstallVerificationFailed, InvalidSignature, SignatureVerified
        public int? RequestId { get; set; }
    }
        
    private class FirmwareStatusNotificationResponse
    {
        // Пустой ответ для OCPP 2.0 FirmwareStatusNotification
    }
}