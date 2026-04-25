using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public class DigitalEnvelopeSignatureFailCloseTests
{
    [Fact]
    public async Task SignatureValidator_ShouldValidateValidSignature()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-Valid");
        var content = Encoding.UTF8.GetBytes("NACHA-VALID-CONTENT");
        var signedData = CreateSignedData(content, signer);
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content));

        result.IsValid.Should().BeTrue();
        result.IsVerified.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
    }

    [Fact]
    public async Task SignatureValidator_ShouldRejectTamperedContent()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-TamperedContent");
        var original = Encoding.UTF8.GetBytes("NACHA-ORIGINAL");
        var tampered = Encoding.UTF8.GetBytes("NACHA-TAMPERED");
        var signedData = CreateSignedData(original, signer);
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, tampered));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("SIGNATURE_VALIDATION_FAILED");
    }

    [Fact]
    public async Task SignatureValidator_ShouldRejectTamperedSignature()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-TamperedSig");
        var content = Encoding.UTF8.GetBytes("NACHA-SIG");
        var signedData = CreateSignedData(content, signer);
        signedData.EncryptedDigest = Convert.ToBase64String(Encoding.UTF8.GetBytes("invalid-signature"));
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("SIGNATURE_VALIDATION_FAILED");
    }

    [Fact]
    public async Task SignatureValidator_ShouldFail_WhenSignerCertificateMissing()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-MissingCert");
        var content = Encoding.UTF8.GetBytes("NACHA-MISSING-CERT");
        var signedData = CreateSignedData(content, signer);
        signedData.SignerInfo.Certificate = string.Empty;
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("SIGNER_CERTIFICATE_MISSING");
    }

    [Fact]
    public async Task OpenEnvelopeAsync_ShouldFailClose_WhenSignatureInvalid()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-OpenEnvelopeFail");
        var receiver = CreateSelfSignedCertificate("CN=Receiver-OpenEnvelopeFail");
        var envelope = BuildEnvelope(Encoding.UTF8.GetBytes("NACHA-FAIL"), signer, receiver, tamperSignature: true);

        var service = CreateCryptoService(signer, receiver, new DigitalEnvelopeSignatureValidationOptions
        {
            EnableSignatureValidation = true,
            FailCloseOnInvalidSignature = true,
            AuditInvalidSignature = true
        }, out var audit);

        var act = () => service.OpenEnvelopeAsync(envelope, "input.env");
        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(act);

        ex.ErrorCode.Should().Be("SIGNATURE_VALIDATION_FAILED");
        audit.Events.Should().Contain(e => e.Result == "FAILED" && e.ErrorCode == "SIGNATURE_VALIDATION_FAILED" && e.FailCloseApplied);
    }

    [Fact]
    public async Task OpenEnvelopeAsync_ShouldNotReturnPlainContent_WhenSignatureInvalid()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-NoPlain");
        var receiver = CreateSelfSignedCertificate("CN=Receiver-NoPlain");
        var envelope = BuildEnvelope(Encoding.UTF8.GetBytes("NACHA-NO-PLAIN"), signer, receiver, tamperSignature: true);

        var service = CreateCryptoService(signer, receiver, new DigitalEnvelopeSignatureValidationOptions
        {
            EnableSignatureValidation = true,
            FailCloseOnInvalidSignature = true
        }, out _);

        var act = () => service.OpenEnvelopeAsync(envelope, "input.env");
        await act.Should().ThrowAsync<DigitalEnvelopeSignatureValidationException>();
    }

    [Fact]
    public async Task OpenEnvelopeAsync_ShouldAllowValidSignedEnvelope()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-ValidOpen");
        var receiver = CreateSelfSignedCertificate("CN=Receiver-ValidOpen");
        var plain = Encoding.UTF8.GetBytes("NACHA-VALID-OPEN");
        var envelope = BuildEnvelope(plain, signer, receiver);
        var service = CreateCryptoService(signer, receiver, new DigitalEnvelopeSignatureValidationOptions
        {
            EnableSignatureValidation = true,
            FailCloseOnInvalidSignature = true
        }, out var audit);

        var result = await service.OpenEnvelopeAsync(envelope, "valid.env");

        result.Should().Equal(plain);
        audit.Events.Should().Contain(e => e.Result == "SUCCESS");
    }

    [Fact]
    public async Task OpenEnvelopeAsync_ShouldRespectLegacyCompatibilityFlag_WhenDisabledOnlyInTest()
    {
        var signer = CreateSelfSignedCertificate("CN=Signer-Legacy");
        var receiver = CreateSelfSignedCertificate("CN=Receiver-Legacy");
        var plain = Encoding.UTF8.GetBytes("NACHA-LEGACY");
        var envelope = BuildEnvelope(plain, signer, receiver, removeSignature: true);
        var service = CreateCryptoService(signer, receiver, new DigitalEnvelopeSignatureValidationOptions
        {
            EnableSignatureValidation = false,
            AllowLegacyUnsignedEnvelope = true,
            AuditInvalidSignature = true
        }, out var audit);

        var result = await service.OpenEnvelopeAsync(envelope, "legacy.env");

        result.Should().Equal(plain);
        audit.Events.Should().Contain(e => e.ErrorCode == "SIGNATURE_VALIDATION_DISABLED_WARNING" && e.LegacyBypassUsed);
    }

    private static IDigitalEnvelopeSignatureValidator CreateValidator(DigitalEnvelopeSignatureValidationOptions? options = null)
    {
        return new DigitalEnvelopeSignatureValidator(
            Options.Create(options ?? new DigitalEnvelopeSignatureValidationOptions
            {
                EnableSignatureValidation = true,
                FailCloseOnInvalidSignature = true,
                FailWhenSignerCertificateMissing = true,
                FailWhenSignerCertificateExpired = true
            }));
    }

    private static CryptoServiceScoped CreateCryptoService(
        X509Certificate2 signer,
        X509Certificate2 receiver,
        DigitalEnvelopeSignatureValidationOptions options,
        out InMemorySignatureAudit audit)
    {
        audit = new InMemorySignatureAudit();
        return new CryptoServiceScoped(
            new FakeRsaKeyProvider(signer, receiver),
            CreateValidator(options),
            audit,
            Options.Create(options),
            NullLogger<CryptoServiceScoped>.Instance);
    }

    private static SignedData CreateSignedData(byte[] plainContent, X509Certificate2 signer, bool removeSignature = false)
    {
        var hash = SHA256.HashData(plainContent);
        using var rsa = signer.GetRSAPrivateKey()!;
        var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return new SignedData
        {
            Version = "1",
            SignerInfo = new SignerInfo
            {
                SignatureAlgorithm = "SHA256withRSA",
                Certificate = Convert.ToBase64String(signer.RawData)
            },
            ContentInfo = Convert.ToBase64String(Cfa.ACHInterbank.Application.Helpers.ZIP.ZipHelper.ZipContend(plainContent, "test.txt")),
            EncryptedDigest = removeSignature ? string.Empty : Convert.ToBase64String(signature)
        };
    }

    private static byte[] BuildEnvelope(byte[] plainContent, X509Certificate2 signer, X509Certificate2 receiver, bool tamperSignature = false, bool removeSignature = false)
    {
        var signedData = CreateSignedData(plainContent, signer, removeSignature);
        if (tamperSignature)
        {
            var sig = Convert.FromBase64String(signedData.EncryptedDigest);
            sig[0] ^= 0xFF;
            signedData.EncryptedDigest = Convert.ToBase64String(sig);
        }

        var signedXml = SerializeXml(signedData);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var identifier = (signer.SerialNumber + Guid.NewGuid().ToString("N")).ToUpperInvariant();
        var iv = DeriveIvFromIdentifier(identifier);
        var encryptedContent = EncryptAes(Encoding.UTF8.GetBytes(signedXml), aes.Key, iv);

        using var recipientPublic = receiver.GetRSAPublicKey()!;
        var encryptedKey = recipientPublic.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);

        var envelope = new DigitalEnvelopeModel
        {
            Version = 1,
            Identifier = identifier,
            Timestamp = DateTime.UtcNow.ToString("o"),
            RecipientInfo = new RecipientInfo
            {
                KeyEncryptionAlgorithm = "RSA/NONE/PKCS1Padding",
                EncryptedKey = Convert.ToBase64String(encryptedKey),
                CertificateInfo = new CertificateInfo
                {
                    Issuer = receiver.Issuer,
                    Serial = receiver.SerialNumber
                }
            },
            EncryptedContentInfo = new EncryptedContentInfo
            {
                ContentType = "signedData",
                ContentEncryptionAlgorithm = "AES/CBC/PKCS5padding",
                EncryptedContent = Convert.ToBase64String(encryptedContent)
            }
        };

        return Encoding.UTF8.GetBytes(SerializeXml(envelope));
    }

    private static byte[] DeriveIvFromIdentifier(string identifier)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(identifier)).Take(16).ToArray();
    }

    private static byte[] EncryptAes(byte[] plain, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plain, 0, plain.Length);
    }

    private static string SerializeXml<T>(T payload)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var sw = new StringWriter();
        serializer.Serialize(sw, payload);
        return sw.ToString();
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    private sealed class FakeRsaKeyProvider : IRsaKeyProvider
    {
        private readonly X509Certificate2 _signer;
        private readonly X509Certificate2 _receiver;

        public FakeRsaKeyProvider(X509Certificate2 signer, X509Certificate2 receiver)
        {
            _signer = signer;
            _receiver = receiver;
        }

        public X509Certificate2 ObtenerCertificate(string Key_cert)
        {
            return Key_cert switch
            {
                "CertSign" => _signer,
                "CertDecrypt" => _receiver,
                "CertCrypt" => _receiver,
                _ => throw new InvalidOperationException($"Certificado no soportado en test: {Key_cert}.")
            };
        }

        public X509Certificate2 ObtenerCertificateForDecrypt(string? recipientIssuer, string? recipientSerial, string? recipientThumbprint = null)
            => _receiver;
    }

    private sealed class InMemorySignatureAudit : IDigitalEnvelopeSignatureAuditService
    {
        public List<AuditEvent> Events { get; } = new();

        public Task AuditAsync(
            string result,
            string? errorCode,
            string? signerThumbprint,
            string? signerSerialNumber,
            string? signatureAlgorithm,
            bool failCloseApplied,
            bool legacyBypassUsed,
            string actor,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new AuditEvent(result, errorCode, signerThumbprint, signerSerialNumber, signatureAlgorithm, failCloseApplied, legacyBypassUsed, actor));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEvent(
        string Result,
        string? ErrorCode,
        string? SignerThumbprint,
        string? SignerSerialNumber,
        string? SignatureAlgorithm,
        bool FailCloseApplied,
        bool LegacyBypassUsed,
        string Actor);
}
