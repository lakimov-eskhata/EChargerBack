using OCPP.API.Middleware.OCPP16;
using Microsoft.Extensions.Logging;

namespace OCPP.API.Middleware.OCPP21.Handlers;

public class CustomerInformationHandler : IMessageHandler
{
    private readonly ILogger<CustomerInformationHandler> _logger;
    public CustomerInformationHandler(ILogger<CustomerInformationHandler> logger) { _logger = logger; }
    public Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation($"[OCPP2.1] CustomerInformation from {chargePointId}");
        return Task.FromResult<object>(new { status = "Accepted" });
    }
}
