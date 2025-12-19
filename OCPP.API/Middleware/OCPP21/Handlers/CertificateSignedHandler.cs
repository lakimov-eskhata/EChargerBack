using OCPP.API.Middleware.Common;

namespace OCPP.API.Middleware.OCPP21.Handlers;

public class CertificateSignedHandler : OCPP.API.Middleware.OCPP16.IMessageHandler
{
    private readonly ILogger<CertificateSignedHandler> _logger;
    public CertificateSignedHandler(ILogger<CertificateSignedHandler> logger) { _logger = logger; }
    public Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation($"[OCPP2.1] CertificateSigned from {chargePointId}");
        return Task.FromResult<object>(new { status = "Accepted" });
    }
}
