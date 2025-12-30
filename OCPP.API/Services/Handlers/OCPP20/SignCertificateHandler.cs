using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

public class SignCertificateHandler : IMessageHandler
{
    private readonly ILogger<SignCertificateHandler> _logger;
    private readonly ICertificateService _certificateService;

    public SignCertificateHandler(
        ILogger<SignCertificateHandler> logger,
        ICertificateService certificateService)
    {
        _logger = logger;
        _certificateService = certificateService;
    }

    public async Task<object> HandleAsync(string chargePointId, object message)
    {
        _logger.LogInformation("Processing OCPP 2.0 SignCertificate for {ChargePointId}", chargePointId);

        var jsonRpc = message as JsonRpcMessage;
        if (jsonRpc == null)
            throw new ArgumentException("Invalid message format");

        var request = jsonRpc.Params.Deserialize<SignCertificateRequest>();

        try
        {
            // Подписываем CSR (Certificate Signing Request)
            var signedCertificate = await _certificateService.SignCertificateAsync(
                request.Csr,
                request.CertificateType);

            _logger.LogInformation("Certificate signed successfully for {ChargePointId}", chargePointId);

            return new SignCertificateResponse
            {
                Status = "Accepted",
                CertificateChain = signedCertificate,
                StatusInfo = new StatusInfo
                {
                    ReasonCode = "Signed",
                    AdditionalInfo = "Certificate signed successfully"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign certificate for {ChargePointId}", chargePointId);

            return new SignCertificateResponse
            {
                Status = "Rejected",
                StatusInfo = new StatusInfo
                {
                    ReasonCode = "SigningError",
                    AdditionalInfo = ex.Message
                }
            };
        }
    }

    private class SignCertificateRequest
    {
        public string Csr { get; set; } = string.Empty; // Certificate Signing Request
        public string CertificateType { get; set; } = string.Empty; // V2G, MO, CSMS, MF
    }

    private class SignCertificateResponse
    {
        public string Status { get; set; } = string.Empty; // Accepted, Rejected
        public string? CertificateChain { get; set; }
        public StatusInfo? StatusInfo { get; set; }
    }

    private class StatusInfo
    {
        public string ReasonCode { get; set; } = string.Empty;
        public string AdditionalInfo { get; set; } = string.Empty;
    }
}

// Интерфейс сервиса сертификатов
public interface ICertificateService
{
    Task<string> SignCertificateAsync(string csr, string certificateType);
    Task<bool> ValidateCertificateAsync(string certificate);
    Task<string> GenerateCsrAsync(string commonName, string organization);
}