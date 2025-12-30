using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OCPP.API.Common;
using OCPP.API.Core.Abstractions;

namespace OCPP.API.Services.Handlers.OCPP20;

 public class AuthorizeHandler : IMessageHandler
    {
        private readonly ILogger<AuthorizeHandler> _logger;
        
        public AuthorizeHandler(ILogger<AuthorizeHandler> logger)
        {
            _logger = logger;
        }
        
        public async Task<object> HandleAsync(string chargePointId, object message)
        {
            _logger.LogDebug("Processing OCPP 2.0 Authorize for {ChargePointId}", chargePointId);
            
            var jsonRpc = message as JsonRpcMessage;
            if (jsonRpc == null)
                throw new ArgumentException("Invalid message format");
            
            var request = jsonRpc.Params.Deserialize<AuthorizeRequest>();
            
            // Здесь должна быть логика авторизации
            // Проверка по базе данных, внешнему сервису и т.д.
            var idTokenInfo = new IdTokenInfo
            {
                Status = ValidateIdToken(request.IdToken) ? "Accepted" : "Invalid",
                CacheExpiryDateTime = DateTime.UtcNow.AddHours(1),
                GroupIdToken = null,
                PersonalMessage = new PersonalMessage
                {
                    Content = "Authorized",
                    Format = "UTF8",
                    Language = "en"
                }
            };
            
            _logger.LogInformation("Authorized idToken for {ChargePointId}", chargePointId);
            
            return new AuthorizeResponse
            {
                IdTokenInfo = idTokenInfo,
                CertificateStatus = "Accepted"
            };
        }
        
        private bool ValidateIdToken(IdToken idToken)
        {
            // Реальная логика валидации токена
            // Проверка в базе данных, проверка срока действия и т.д.
            return !string.IsNullOrEmpty(idToken?.Id);
        }
        
        private class AuthorizeRequest
        {
            public IdToken IdToken { get; set; } = new();
            public string? Certificate { get; set; }
            public Iso15118CertificateHashData[]? Iso15118CertificateHashData { get; set; }
        }
        
        private class AuthorizeResponse
        {
            public IdTokenInfo IdTokenInfo { get; set; } = new();
            public string CertificateStatus { get; set; } = string.Empty;
        }
        
        private class IdToken
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty; // e.g., "Central", "eMAID", "ISO14443"
            public AdditionalInfo[]? AdditionalInfo { get; set; }
        }
        
        private class IdTokenInfo
        {
            public string Status { get; set; } = string.Empty; // Accepted, Blocked, Expired, Invalid, ConcurrentTx
            public DateTime? CacheExpiryDateTime { get; set; }
            public IdToken? GroupIdToken { get; set; }
            public PersonalMessage? PersonalMessage { get; set; }
        }
        
        private class AdditionalInfo
        {
            public string AdditionalIdToken { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
        }
        
        private class PersonalMessage
        {
            public string Format { get; set; } = string.Empty; // UTF8
            public string Language { get; set; } = string.Empty; // en, de, fr, etc.
            public string Content { get; set; } = string.Empty;
        }
        
        private class Iso15118CertificateHashData
        {
            public string HashAlgorithm { get; set; } = string.Empty;
            public string IssuerNameHash { get; set; } = string.Empty;
            public string IssuerKeyHash { get; set; } = string.Empty;
            public string SerialNumber { get; set; } = string.Empty;
        }
    }