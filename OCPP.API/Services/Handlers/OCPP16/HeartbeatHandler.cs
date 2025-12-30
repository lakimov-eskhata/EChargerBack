using System;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP16;

public class HeartbeatHandler : IMessageHandler
{
    private readonly ILogger<HeartbeatHandler> _logger;
    private readonly IOCPPService _ocppService;
        
    public HeartbeatHandler(
        ILogger<HeartbeatHandler> logger,
        IOCPPService ocppService)
    {
        _logger = logger;
        _ocppService = ocppService;
    }
        
    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogDebug("Processing Heartbeat for {ChargePointId}", chargePointId);
            
        // Обрабатываем heartbeat через OCPPService
        await _ocppService.ProcessHeartbeatAsync(chargePointId);
            
        return new
        {
            currentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }
}