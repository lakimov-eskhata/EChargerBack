using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class FirmwareStatusNotificationHandler : IMessageHandler
{
    private readonly ILogger<FirmwareStatusNotificationHandler> _logger;

    public FirmwareStatusNotificationHandler(ILogger<FirmwareStatusNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation($"[OCPP1.6] FirmwareStatusNotification from {chargePointId}");
        return new { };
    }
}

