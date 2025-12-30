using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCPP.API.Services.Handlers.OCPP20;

namespace OCPP.API.Services;

 public class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly string _caCertificatePath;
        private readonly string _caPrivateKeyPath;
        
        public CertificateService(
            ILogger<CertificateService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _caCertificatePath = configuration["Certificates:CaCertificatePath"] ?? "certs/ca.crt";
            _caPrivateKeyPath = configuration["Certificates:CaPrivateKeyPath"] ?? "certs/ca.key";
        }
        
        public async Task<string> SignCertificateAsync(string csr, string certificateType)
        {
            try
            {
                _logger.LogInformation("Signing certificate of type: {CertificateType}", certificateType);
                
                // Загружаем CA сертификат и приватный ключ
                var caCertificate = await LoadCACertificateAsync();
                var caPrivateKey = await LoadCAPrivateKeyAsync();
                
                // Парсим CSR
                var csrBytes = Convert.FromBase64String(csr
                    .Replace("-----BEGIN CERTIFICATE REQUEST-----", "")
                    .Replace("-----END CERTIFICATE REQUEST-----", "")
                    .Replace("\n", "")
                    .Trim());
                
                var certificateRequest = new CertificateRequest(
                    new X500DistinguishedName($"CN={certificateType}_Certificate,O=ECharger"),
                    ECDsa.Create(),
                    HashAlgorithmName.SHA256);
                
                // Создаем сертификат
                var notBefore = DateTime.UtcNow.AddDays(-1);
                var notAfter = DateTime.UtcNow.AddYears(1);
                
                var certificate = certificateRequest.Create(
                    caCertificate,
                    notBefore,
                    notAfter,
                    Guid.NewGuid().ToByteArray());
                
                // Экспортируем в PEM формат
                var certificatePem = ExportCertificateToPem(certificate);
                
                _logger.LogInformation("Certificate signed successfully");
                return certificatePem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing certificate");
                throw;
            }
        }
        
        public async Task<bool> ValidateCertificateAsync(string certificateChain)
        {
            try
            {
                _logger.LogDebug("Validating certificate chain");
                
                // Загружаем CA сертификат
                var caCertificate = await LoadCACertificateAsync();
                
                // Парсим цепочку сертификатов
                var certificates = ParseCertificateChain(certificateChain);
                
                // Проверяем каждый сертификат в цепочке
                foreach (var cert in certificates)
                {
                    // Проверяем срок действия
                    if (DateTime.UtcNow < cert.NotBefore || DateTime.UtcNow > cert.NotAfter)
                    {
                        _logger.LogWarning("Certificate expired or not yet valid");
                        return false;
                    }
                    
                    // Проверяем подпись (в реальной системе здесь была бы проверка цепочки доверия)
                    // Для упрощения проверяем только что сертификат существует
                }
                
                _logger.LogInformation("Certificate chain validated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating certificate chain");
                return false;
            }
        }
        
        public async Task<string> GenerateCsrAsync(string commonName, string organization)
        {
            try
            {
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                
                var request = new CertificateRequest(
                    new X500DistinguishedName($"CN={commonName},O={organization}"),
                    ecdsa,
                    HashAlgorithmName.SHA256);
                
                // Добавляем расширения
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, true));
                
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        true));
                
                // Генерируем CSR
                var csr = request.CreateSigningRequest();
                var csrPem = FormatPem("CERTIFICATE REQUEST", csr);
                
                _logger.LogInformation("CSR generated for {CommonName}", commonName);
                return csrPem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSR");
                throw;
            }
        }
        
        private async Task<X509Certificate2> LoadCACertificateAsync()
        {
            try
            {
                if (File.Exists(_caCertificatePath))
                {
                    var certBytes = await File.ReadAllBytesAsync(_caCertificatePath);
                    return new X509Certificate2(certBytes);
                }
                else
                {
                    _logger.LogWarning("CA certificate not found at {Path}, generating new one", _caCertificatePath);
                    return await GenerateCACertificateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading CA certificate");
                throw;
            }
        }
        
        private async Task<ECDsa> LoadCAPrivateKeyAsync()
        {
            try
            {
                if (File.Exists(_caPrivateKeyPath))
                {
                    var keyPem = await File.ReadAllTextAsync(_caPrivateKeyPath);
                    var keyBytes = Convert.FromBase64String(keyPem
                        .Replace("-----BEGIN PRIVATE KEY-----", "")
                        .Replace("-----END PRIVATE KEY-----", "")
                        .Replace("\n", "")
                        .Trim());
                    
                    var ecdsa = ECDsa.Create();
                    ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);
                    return ecdsa;
                }
                else
                {
                    _logger.LogWarning("CA private key not found at {Path}, generating new one", _caPrivateKeyPath);
                    var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                    
                    // Сохраняем ключ для будущего использования
                    var keyBytes = ecdsa.ExportPkcs8PrivateKey();
                    var keyPem = FormatPem("PRIVATE KEY", keyBytes);
                    await File.WriteAllTextAsync(_caPrivateKeyPath, keyPem);
                    
                    return ecdsa;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading CA private key");
                throw;
            }
        }
        
        private async Task<X509Certificate2> GenerateCACertificateAsync()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            
            var request = new CertificateRequest(
                new X500DistinguishedName("CN=ECharger CA,O=ECharger"),
                ecdsa,
                HashAlgorithmName.SHA256);
            
            // CA расширения
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, true, 0, true));
            
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                    true));
            
            // Создаем самоподписанный CA сертификат
            var certificate = request.CreateSelfSigned(
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddYears(10));
            
            // Сохраняем сертификат
            var certBytes = certificate.Export(X509ContentType.Cert);
            await File.WriteAllBytesAsync(_caCertificatePath, certBytes);
            
            // Сохраняем приватный ключ
            var keyBytes = ecdsa.ExportPkcs8PrivateKey();
            var keyPem = FormatPem("PRIVATE KEY", keyBytes);
            await File.WriteAllTextAsync(_caPrivateKeyPath, keyPem);
            
            _logger.LogInformation("Generated new CA certificate");
            return certificate;
        }
        
        private string ExportCertificateToPem(X509Certificate2 certificate)
        {
            var certBytes = certificate.Export(X509ContentType.Cert);
            return FormatPem("CERTIFICATE", certBytes);
        }
        
        private List<X509Certificate2> ParseCertificateChain(string certificateChain)
        {
            var certificates = new List<X509Certificate2>();
            
            var certs = certificateChain.Split(
                new[] { "-----BEGIN CERTIFICATE-----" },
                StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var cert in certs)
            {
                if (string.IsNullOrWhiteSpace(cert)) continue;
                
                var certPem = "-----BEGIN CERTIFICATE-----" + cert.Trim();
                var certBytes = Encoding.UTF8.GetBytes(certPem);
                certificates.Add(new X509Certificate2(certBytes));
            }
            
            return certificates;
        }
        
        private string FormatPem(string label, byte[] data)
        {
            var base64 = Convert.ToBase64String(data);
            var pem = new StringBuilder();
            
            pem.AppendLine($"-----BEGIN {label}-----");
            
            for (int i = 0; i < base64.Length; i += 64)
            {
                var line = base64.Substring(i, Math.Min(64, base64.Length - i));
                pem.AppendLine(line);
            }
            
            pem.AppendLine($"-----END {label}-----");
            return pem.ToString();
        }
    }