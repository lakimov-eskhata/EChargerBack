using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class DiagnosticsStatusNotificationHandler : IMessageHandler
{
    private readonly ILogger<DiagnosticsStatusNotificationHandler> _logger;

    public DiagnosticsStatusNotificationHandler(ILogger<DiagnosticsStatusNotificationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation($"[OCPP1.6] DiagnosticsStatusNotification from {chargePointId}");
        return new { };
    }
}

