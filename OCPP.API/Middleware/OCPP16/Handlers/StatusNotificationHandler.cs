using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class StatusNotificationHandler : IMessageHandler
{
    private readonly ILogger<StatusNotificationHandler> _logger;

    public StatusNotificationHandler(ILogger<StatusNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation($"[OCPP1.6] StatusNotification from {chargePointId}");
        return new { };
    }
}

