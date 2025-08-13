using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Implementations;

public class RsaKeyProviderSingleton : IRsaKeyProviderSingleton
{
    private readonly RSA _publicRsa;
    private readonly RSA _privateRsa;
    private readonly string _keyId;

    public RsaKeyProviderSingleton(IConfiguration config)
    {
        // Ejemplos de configuración:
        // "Crypto:CertPath": "certs/mycert.pfx"
        // "Crypto:CertPassword": "****"
        // o "Crypto:PublicPem", "Crypto:PrivatePem"

        var certPath = config["Crypto:CertPath"];
        var certPassword = config["Crypto:CertPassword"];

        if (!string.IsNullOrWhiteSpace(certPath))
        {
            var cert = new X509Certificate2(certPath, certPassword, X509KeyStorageFlags.Exportable);
            _publicRsa = cert.GetRSAPublicKey() ?? throw new InvalidOperationException("Cert sin clave pública");
            _privateRsa = cert.GetRSAPrivateKey() ?? throw new InvalidOperationException("Cert sin clave privada");
            _keyId = cert.Thumbprint ?? throw new InvalidOperationException("Cert sin Thumbprint");
        }
        else
        {
            // Fallback PEM
            var pubPem = config["Crypto:PublicPem"] ?? throw new InvalidOperationException("Falta PublicPem");
            var prvPem = config["Crypto:PrivatePem"] ?? throw new InvalidOperationException("Falta PrivatePem");

            _publicRsa = RSA.Create();
            _publicRsa.ImportFromPem(pubPem);

            _privateRsa = RSA.Create();
            _privateRsa.ImportFromPem(prvPem);

            // KeyId: hash del módulo público
            var parameters = _publicRsa.ExportParameters(false);
            using var sha = SHA256.Create();
            _keyId = Convert.ToHexString(sha.ComputeHash(parameters.Modulus!)).ToLowerInvariant();
        }
    }

    public RSA GetPublicRsa() => _publicRsa;
    public RSA GetPrivateRsa() => _privateRsa;
    public string GetKeyId() => _keyId;
}
