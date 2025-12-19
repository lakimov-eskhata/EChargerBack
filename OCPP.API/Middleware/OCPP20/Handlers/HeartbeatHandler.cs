using OCPP.API.Middleware.Common;
using OCPP.API.Middleware.OCPP16;

namespace OCPP.API.Middleware.OCPP20.Handlers;

public class HeartbeatHandler : IMessageHandler
{
    private readonly ILogger<HeartbeatHandler> _logger;
    public HeartbeatHandler(ILogger<HeartbeatHandler> logger) { _logger = logger; }
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        return new { currentTime = DateTime.UtcNow.ToString("o") };
    }
}

