using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class CertificateSignedHandler : IMessageHandler
{
    private readonly ILogger<CertificateSignedHandler> _logger;
    private readonly ICertificateService _certificateService;

    public CertificateSignedHandler(
        ILogger<CertificateSignedHandler> logger,
        ICertificateService certificateService)
    {
        _logger = logger;
        _certificateService = certificateService;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation("Processing OCPP 2.0 CertificateSigned for {ChargePointId}", chargePointId);

        var jsonRpc = message as JsonRpcMessage;
        if (jsonRpc == null)
            throw new ArgumentException("Invalid message format");

        var request = jsonRpc.Params.Deserialize<CertificateSignedRequest>();

        try
        {
            // Проверяем подписанный сертификат
            var isValid = await _certificateService.ValidateCertificateAsync(
                request.CertificateChain);

            if (isValid)
            {
                _logger.LogInformation("Certificate validated successfully for {ChargePointId}", chargePointId);

                return new CertificateSignedResponse
                {
                    Status = "Accepted",
                    StatusInfo = new StatusInfo
                    {
                        ReasonCode = "CertificateValid",
                        AdditionalInfo = "Certificate chain is valid"
                    }
                };
            }
            else
            {
                _logger.LogWarning("Invalid certificate chain for {ChargePointId}", chargePointId);

                return new CertificateSignedResponse
                {
                    Status = "Rejected",
                    StatusInfo = new StatusInfo
                    {
                        ReasonCode = "InvalidCertificateChain",
                        AdditionalInfo = "Certificate chain validation failed"
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Certificate validation error for {ChargePointId}", chargePointId);

            return new CertificateSignedResponse
            {
                Status = "Rejected",
                StatusInfo = new StatusInfo
                {
                    ReasonCode = "ValidationError",
                    AdditionalInfo = ex.Message
                }
            };
        }
    }

    private class CertificateSignedRequest
    {
        public string CertificateChain { get; set; } = string.Empty; // PEM encoded certificate chain
        public string CertificateType { get; set; } = string.Empty; // V2G, MO, CSMS, MF
    }

    private class CertificateSignedResponse
    {
        public string Status { get; set; } = string.Empty; // Accepted, Rejected
        public StatusInfo? StatusInfo { get; set; }
    }

    private class StatusInfo
    {
        public string ReasonCode { get; set; } = string.Empty;
        public string AdditionalInfo { get; set; } = string.Empty;
    }
}