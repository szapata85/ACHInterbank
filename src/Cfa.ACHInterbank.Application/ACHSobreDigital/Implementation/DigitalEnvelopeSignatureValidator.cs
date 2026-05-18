using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;

[Scoped]
public class DigitalEnvelopeSignatureValidator : IDigitalEnvelopeSignatureValidator
{
    private readonly DigitalEnvelopeSignatureValidationOptions _options;

    public DigitalEnvelopeSignatureValidator(IOptions<DigitalEnvelopeSignatureValidationOptions> options)
    {
        _options = options.Value ?? new DigitalEnvelopeSignatureValidationOptions();
    }

    public Task<DigitalEnvelopeSignatureValidationResult> ValidateAsync(
        DigitalEnvelopeSignatureValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var signedData = request.SignedData;

        if (string.IsNullOrWhiteSpace(signedData.EncryptedDigest))
        {
            return Task.FromResult(Fail("SIGNATURE_VALIDATION_FAILED", "No existe firma (encryptedDigest).", warnings));
        }

        if (string.IsNullOrWhiteSpace(signedData.SignerInfo?.Certificate))
        {
            return Task.FromResult(
                _options.FailWhenSignerCertificateMissing
                    ? Fail("SIGNER_CERTIFICATE_MISSING", "No existe certificado firmante en signedData.", warnings)
                    : WarnAndFail("SIGNER_CERTIFICATE_MISSING", "Certificado firmante ausente.", warnings));
        }

        X509Certificate2 signerCertificate;
        try
        {
            signerCertificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(signedData.SignerInfo.Certificate.Replace("\n", "")));
        }
        catch
        {
            return Task.FromResult(Fail("SIGNER_CERTIFICATE_MISSING", "Certificado firmante inválido o no legible.", warnings));
        }

        if (_options.FailWhenSignerCertificateExpired)
        {
            var now = DateTime.UtcNow;
            if (now < signerCertificate.NotBefore.ToUniversalTime())
            {
                return Task.FromResult(Fail("SIGNER_CERTIFICATE_NOT_YET_VALID", "El certificado firmante aún no está vigente.", warnings, signerCertificate));
            }

            if (now > signerCertificate.NotAfter.ToUniversalTime())
            {
                return Task.FromResult(Fail("SIGNER_CERTIFICATE_EXPIRED", "El certificado firmante está expirado.", warnings, signerCertificate));
            }
        }

        if (_options.ValidateSignerCertificateThumbprint
            && !string.IsNullOrWhiteSpace(request.ExpectedSignerThumbprint)
            && !string.Equals(
                signerCertificate.Thumbprint,
                request.ExpectedSignerThumbprint,
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Fail("SIGNED_CONTENT_MISMATCH", "Thumbprint del firmante no coincide con el esperado.", warnings, signerCertificate));
        }

        if (_options.ValidateSignerCertificateChain)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            if (!chain.Build(signerCertificate))
            {
                return Task.FromResult(Fail("SIGNER_CERTIFICATE_NOT_TRUSTED", "La cadena del certificado firmante no es confiable.", warnings, signerCertificate));
            }
        }

        var signatureAlgorithm = signedData.SignerInfo.SignatureAlgorithm;
        if (!string.Equals(signatureAlgorithm, "SHA256withRSA", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Fail("SIGNATURE_ALGORITHM_NOT_SUPPORTED", $"Algoritmo de firma no soportado: {signatureAlgorithm}.", warnings, signerCertificate));
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(signedData.EncryptedDigest.Replace("\n", ""));
        }
        catch
        {
            return Task.FromResult(Fail("SIGNATURE_VALIDATION_FAILED", "Firma no válida en formato Base64.", warnings, signerCertificate));
        }

        var hash = SHA256.HashData(request.PlainContent);
        using var rsa = signerCertificate.GetRSAPublicKey();
        if (rsa == null)
        {
            return Task.FromResult(Fail("SIGNATURE_ALGORITHM_NOT_SUPPORTED", "No se encontró llave pública RSA en el certificado firmante.", warnings, signerCertificate));
        }

        var verified = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        if (!verified)
        {
            return Task.FromResult(Fail("SIGNATURE_VALIDATION_FAILED", "La firma no corresponde al contenido firmado.", warnings, signerCertificate));
        }

        return Task.FromResult(new DigitalEnvelopeSignatureValidationResult(
            true,
            true,
            signerCertificate.Thumbprint,
            signerCertificate.SerialNumber,
            signatureAlgorithm,
            "SHA256",
            null,
            null,
            warnings));
    }

    private static DigitalEnvelopeSignatureValidationResult Fail(
        string code,
        string message,
        IReadOnlyList<string> warnings,
        X509Certificate2? cert = null)
    {
        return new DigitalEnvelopeSignatureValidationResult(
            false,
            false,
            cert?.Thumbprint,
            cert?.SerialNumber,
            null,
            "SHA256",
            code,
            message,
            warnings);
    }

    private static DigitalEnvelopeSignatureValidationResult WarnAndFail(string code, string message, List<string> warnings)
    {
        warnings.Add(message);
        return Fail(code, message, warnings);
    }
}
