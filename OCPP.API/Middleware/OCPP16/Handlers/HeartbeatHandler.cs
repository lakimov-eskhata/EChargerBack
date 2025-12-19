using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP16.Handlers;

public class HeartbeatHandler : IMessageHandler
{
    private readonly ILogger<HeartbeatHandler> _logger;

    public HeartbeatHandler(ILogger<HeartbeatHandler> logger)
    {
        _logger = logger;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug($"[OCPP1.6] Heartbeat from {chargePointId}");
        return new { currentTime = DateTime.UtcNow.ToString("o") };
    }
}

