using System;
using System.Threading.Tasks;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

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
        _logger.LogDebug("Processing OCPP 2.0 Heartbeat for {ChargePointId}", chargePointId);
            
        await _ocppService.ProcessHeartbeatAsync(chargePointId);
            
        return new HeartbeatResponse
        {
            CurrentTime = DateTime.UtcNow
        };
    }
        
    private class HeartbeatResponse
    {
        public DateTime CurrentTime { get; set; }
    }
}