using System.Reflection;
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

public class DigitalEnvelopeCertificateCharacterizationTests
{
    [Fact]
    public async Task DigitalEnvelope_ShouldCreateAndOpenEnvelope_WithValidCertificates_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=char-signer", withPrivateKey: true);
        var receiver = CreateSelfSignedCertificate("CN=char-receiver", withPrivateKey: true);
        var service = CreateCryptoService(signer, receiver, DefaultOptions(), out var audit);
        var plain = Encoding.UTF8.GetBytes("NACHA-CHAR-VALID");

        var envelopeBytes = await service.CreateEnvelopeAsync(plain, "char.txt");
        var envelope = DeserializeXml<DigitalEnvelopeModel>(Encoding.UTF8.GetString(envelopeBytes));
        var compatibleEnvelope = BuildEnvelopeWithMutator(plain, signer, receiver, null);
        var roundtrip = await service.OpenEnvelopeAsync(compatibleEnvelope, "char.env");

        roundtrip.Should().Equal(plain);
        envelope.RecipientInfo.Should().NotBeNull();
        envelope.RecipientInfo.CertificateInfo.Should().NotBeNull();
        envelope.RecipientInfo.EncryptedKey.Should().NotBeNullOrWhiteSpace();
        envelope.EncryptedContentInfo.Should().NotBeNull();
        envelope.EncryptedContentInfo.EncryptedContent.Should().NotBeNullOrWhiteSpace();
        audit.Events.Should().Contain(x => x.Result == "SUCCESS");
    }

    [Fact]
    public async Task DigitalEnvelope_ShouldFail_WhenRecipientInfoIsMissing_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=char-signer2", true);
        var receiver = CreateSelfSignedCertificate("CN=char-receiver2", true);
        var service = CreateCryptoService(signer, receiver, DefaultOptions(), out _);
        var envelope = BuildEnvelopeWithMutator(Encoding.UTF8.GetBytes("x"), signer, receiver, e => e.RecipientInfo = null!);

        await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "broken.env"));
    }

    [Fact]
    public async Task DigitalEnvelope_ShouldFail_WhenEncryptedKeyIsMissing_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=char-signer3", true);
        var receiver = CreateSelfSignedCertificate("CN=char-receiver3", true);
        var service = CreateCryptoService(signer, receiver, DefaultOptions(), out _);
        var envelope = BuildEnvelopeWithMutator(Encoding.UTF8.GetBytes("x"), signer, receiver, e => e.RecipientInfo.EncryptedKey = "");

        await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "broken.env"));
    }

    [Fact]
    public async Task DigitalEnvelope_ShouldFail_WhenEncryptedContentIsMissing_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=char-signer4", true);
        var receiver = CreateSelfSignedCertificate("CN=char-receiver4", true);
        var service = CreateCryptoService(signer, receiver, DefaultOptions(), out _);
        var envelope = BuildEnvelopeWithMutator(Encoding.UTF8.GetBytes("x"), signer, receiver, e => e.EncryptedContentInfo.EncryptedContent = "");

        await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "broken.env"));
    }

    [Fact]
    public async Task CertificateResolver_ShouldRequirePrivateKey_ForDecryptOrSigningPurpose_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=sign", true);
        var receiverPublicOnly = CreateSelfSignedCertificate("CN=receiver", false);
        var service = CreateCryptoService(signer, receiverPublicOnly, DefaultOptions(), out _);
        var envelope = BuildEnvelopeWithMutator(Encoding.UTF8.GetBytes("NACHA"), signer, CreateSelfSignedCertificate("CN=receiver-real", true), null);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "privatekey.env"));
        ex.ErrorCode.Should().Be("CERTIFICATE_PRIVATE_KEY_REQUIRED");
        ex.Message.ToLowerInvariant().Should().NotContain("password").And.NotContain("private key");
    }

    [Fact]
    public async Task CryptoService_ShouldFailWithFunctionalError_WhenSigningCertificateHasNoPrivateKey()
    {
        var signerWithoutPrivate = CreateSelfSignedCertificate("CN=sign-no-private", false);
        var receiver = CreateSelfSignedCertificate("CN=receiver-private", true);
        var service = CreateCryptoService(signerWithoutPrivate, receiver, DefaultOptions(), out _);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.CreateEnvelopeAsync(Encoding.UTF8.GetBytes("NACHA"), "sign.env"));

        ex.ErrorCode.Should().Be("CERTIFICATE_PRIVATE_KEY_REQUIRED");
        ex.Message.ToLowerInvariant().Should().NotContain("password").And.NotContain("private key");
    }

    [Fact]
    public async Task CertificateResolver_ShouldAllowPublicCertificate_ForSignatureValidationPurpose_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=sign-valid", true);
        var content = Encoding.UTF8.GetBytes("abc");
        var signedData = CreateSignedData(content, signer);
        var validator = new DigitalEnvelopeSignatureValidator(Options.Create(DefaultOptions()));

        var result = await validator.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content));

        result.IsVerified.Should().BeTrue();
        result.SignerCertificateThumbprint.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignatureValidator_ShouldRespectCertificateValidity_WhenConfigured_CurrentBehavior()
    {
        var expired = CreateSelfSignedCertificate("CN=expired", true, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-2));
        var content = Encoding.UTF8.GetBytes("abc");
        var signedData = CreateSignedData(content, expired);
        var enabled = new DigitalEnvelopeSignatureValidator(Options.Create(new DigitalEnvelopeSignatureValidationOptions { FailWhenSignerCertificateExpired = true }));
        var disabled = new DigitalEnvelopeSignatureValidator(Options.Create(new DigitalEnvelopeSignatureValidationOptions { FailWhenSignerCertificateExpired = false }));

        (await enabled.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content))).ErrorCode.Should().Be("SIGNER_CERTIFICATE_EXPIRED");
        (await disabled.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content))).IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task SignatureValidator_ShouldUseChainValidationOnlyWhenConfigured_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=self", true);
        var content = Encoding.UTF8.GetBytes("abc");
        var signedData = CreateSignedData(content, signer);

        var withChain = new DigitalEnvelopeSignatureValidator(Options.Create(new DigitalEnvelopeSignatureValidationOptions { ValidateSignerCertificateChain = true }));
        var withoutChain = new DigitalEnvelopeSignatureValidator(Options.Create(new DigitalEnvelopeSignatureValidationOptions { ValidateSignerCertificateChain = false }));

        (await withChain.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content))).ErrorCode.Should().Be("SIGNER_CERTIFICATE_NOT_TRUSTED");
        (await withoutChain.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content))).IsVerified.Should().BeTrue();
    }

    [Fact]
    public void SignatureValidator_ShouldUseNoRevocationCheck_CurrentBehavior()
    {
        var root = ResolveRepositoryRoot();
        var text = File.ReadAllText(Path.Combine(root, "src", "Cfa.ACHInterbank.Application", "ACHSobreDigital", "Implementation", "DigitalEnvelopeSignatureValidator.cs"));
        text.Should().Contain("X509RevocationMode.NoCheck");
    }

    [Fact]
    public async Task SignatureValidator_ShouldRejectUnsignedEnvelope_WhenLegacyBypassDisabled_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=s1", true);
        var receiver = CreateSelfSignedCertificate("CN=r1", true);
        var options = new DigitalEnvelopeSignatureValidationOptions { EnableSignatureValidation = false, AllowLegacyUnsignedEnvelope = false };
        var service = CreateCryptoService(signer, receiver, options, out var audit);
        var envelope = BuildEnvelopeWithMutator(Encoding.UTF8.GetBytes("abc"), signer, receiver, _ => { }, removeSignature: true);

        await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "legacy-off.env"));
        audit.Events.Should().Contain(x => x.ErrorCode == "SIGNATURE_VALIDATION_FAILED" && x.Result == "FAILED");
    }

    [Fact]
    public async Task SignatureValidator_ShouldRejectUnsignedEnvelope_EvenWhenLegacyBypassOptionIsEnabled_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=s2", true);
        var receiver = CreateSelfSignedCertificate("CN=r2", true);
        var options = new DigitalEnvelopeSignatureValidationOptions { EnableSignatureValidation = false, AllowLegacyUnsignedEnvelope = true, AuditInvalidSignature = true };
        var service = CreateCryptoService(signer, receiver, options, out var audit);
        var plain = Encoding.UTF8.GetBytes("legacy");
        var envelope = BuildEnvelopeWithMutator(plain, signer, receiver, _ => { }, removeSignature: true);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "legacy-on.env"));

        ex.ErrorCode.Should().Be("SIGNATURE_VALIDATION_FAILED");
        audit.Events.Should().Contain(x => x.Result == "FAILED" && x.LegacyBypassUsed == false);
    }

    [Fact]
    public async Task Audit_ShouldRecordPrivateKeyValidationFailure_WhenDecryptFails()
    {
        var signer = CreateSelfSignedCertificate("CN=audit-sign", true);
        var receiverPublicOnly = CreateSelfSignedCertificate("CN=audit-receiver", false);
        var service = CreateCryptoService(signer, receiverPublicOnly, DefaultOptions(), out var audit);
        var envelope = BuildEnvelopeWithMutator(Encoding.UTF8.GetBytes("audit-fail"), signer, CreateSelfSignedCertificate("CN=receiver-real2", true), null);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "audit-fail.env"));

        ex.ErrorCode.Should().Be("CERTIFICATE_PRIVATE_KEY_REQUIRED");
        audit.Events.Should().Contain(x => x.Result == "FAILED" && x.ErrorCode == "CERTIFICATE_PRIVATE_KEY_REQUIRED" && x.LegacyBypassUsed == false);
    }

    [Fact]
    public async Task SignatureAudit_ShouldRecordCertificateMetadata_AndNotSecrets_CurrentBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=s3", true);
        var receiver = CreateSelfSignedCertificate("CN=r3", true);
        var service = CreateCryptoService(signer, receiver, DefaultOptions(), out var audit);

        await service.OpenEnvelopeAsync(await service.CreateEnvelopeAsync(Encoding.UTF8.GetBytes("audit"), "a.txt"), "a.env");

        var evt = audit.Events.Single();
        evt.SignerThumbprint.Should().NotBeNullOrWhiteSpace();
        evt.SignerSerialNumber.Should().NotBeNullOrWhiteSpace();
        evt.SignatureAlgorithm.Should().Be("SHA256withRSA");
        evt.ErrorCode.Should().BeNull();
        evt.Actor.Should().Contain("CryptoServiceScoped");
        evt.ToString().Should().NotContain("PrivateKey").And.NotContain("PfxPassword");
    }

    [Fact]
    public async Task SignatureValidator_ShouldRejectNotYetValidSignerCertificate_WhenValidityValidationEnabled()
    {
        var future = CreateSelfSignedCertificate("CN=future", true, DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(30));
        var content = Encoding.UTF8.GetBytes("abc");
        var signedData = CreateSignedData(content, future);
        var validator = new DigitalEnvelopeSignatureValidator(Options.Create(new DigitalEnvelopeSignatureValidationOptions { FailWhenSignerCertificateExpired = true }));

        var result = await validator.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content));

        result.ErrorCode.Should().Be("SIGNER_CERTIFICATE_NOT_YET_VALID");
    }

    [Fact]
    public async Task SignatureValidator_ShouldAcceptSelfSignedCertificate_WhenChainValidationDisabled_CurrentDevBehavior()
    {
        var signer = CreateSelfSignedCertificate("CN=selfsigned", true);
        var content = Encoding.UTF8.GetBytes("abc");
        var signedData = CreateSignedData(content, signer);
        var validator = new DigitalEnvelopeSignatureValidator(Options.Create(new DigitalEnvelopeSignatureValidationOptions { ValidateSignerCertificateChain = false, FailWhenSignerCertificateExpired = true }));

        var result = await validator.ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(signedData, content));
        result.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task CryptoService_ShouldRejectExpiredSigningCertificate_WhenCreatingEnvelope()
    {
        var signerExpired = CreateSelfSignedCertificate("CN=sign-exp", true, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));
        var receiver = CreateSelfSignedCertificate("CN=recv-ok", true);
        var service = CreateCryptoService(signerExpired, receiver, DefaultOptions(), out _);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.CreateEnvelopeAsync(Encoding.UTF8.GetBytes("x"), "e.env"));
        ex.ErrorCode.Should().Be("CERTIFICATE_EXPIRED");
    }

    [Fact]
    public async Task CryptoService_ShouldRejectExpiredDecryptCertificate_WhenOpeningEnvelope()
    {
        var signer = CreateSelfSignedCertificate("CN=sign-ok", true);
        var decryptExpired = CreateSelfSignedCertificate("CN=recv-exp", true, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));
        var service = CreateCryptoService(signer, decryptExpired, DefaultOptions(), out var audit);
        var envelope = BuildEnvelopeWithMutator(Encoding.UTF8.GetBytes("x"), signer, CreateSelfSignedCertificate("CN=recv-valid", true), null);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.OpenEnvelopeAsync(envelope, "exp.env"));
        ex.ErrorCode.Should().Be("CERTIFICATE_EXPIRED");
        audit.Events.Should().Contain(x => x.Result == "FAILED" && x.ErrorCode == "CERTIFICATE_EXPIRED");
    }

    [Fact]
    public async Task CryptoService_ShouldRejectExpiredRecipientCertificate_WhenCreatingEnvelope()
    {
        var signer = CreateSelfSignedCertificate("CN=sign-ok2", true);
        var receiverExpired = CreateSelfSignedCertificate("CN=recv-exp2", true, DateTimeOffset.UtcNow.AddDays(-20), DateTimeOffset.UtcNow.AddDays(-1));
        var service = CreateCryptoService(signer, receiverExpired, DefaultOptions(), out _);

        var ex = await Assert.ThrowsAsync<DigitalEnvelopeSignatureValidationException>(() => service.CreateEnvelopeAsync(Encoding.UTF8.GetBytes("x"), "exp2.env"));
        ex.ErrorCode.Should().Be("CERTIFICATE_EXPIRED");
    }

    private static DigitalEnvelopeSignatureValidationOptions DefaultOptions() => new()
    {
        EnableSignatureValidation = true,
        FailCloseOnInvalidSignature = true,
        FailWhenSignerCertificateMissing = true,
        FailWhenSignerCertificateExpired = true,
        AllowLegacyUnsignedEnvelope = false,
        AuditInvalidSignature = true
    };

    private static CryptoServiceScoped CreateCryptoService(X509Certificate2 signer, X509Certificate2 receiver, DigitalEnvelopeSignatureValidationOptions options, out AuditSink audit)
    {
        audit = new AuditSink();
        return new CryptoServiceScoped(new TestRsaKeyProvider(signer, receiver), new DigitalEnvelopeSignatureValidator(Options.Create(options)), audit, Options.Create(options), NullLogger<CryptoServiceScoped>.Instance);
    }

    private static byte[] BuildEnvelopeWithMutator(byte[] plainContent, X509Certificate2 signer, X509Certificate2 receiver, Action<DigitalEnvelopeModel>? mutator, bool removeSignature = false)
    {
        var signedData = CreateSignedData(plainContent, signer, removeSignature);
        var signedXml = SerializeXml(signedData);
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var identifier = (signer.SerialNumber + Guid.NewGuid().ToString("N")).ToUpperInvariant();
        var iv = SHA256.HashData(Encoding.UTF8.GetBytes(identifier)).Take(16).ToArray();
        var encryptedContent = EncryptAes(Encoding.UTF8.GetBytes(signedXml), aes.Key, iv);
        using var recipientPublic = receiver.GetRSAPublicKey()!;
        var encryptedKey = recipientPublic.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);

        var envelope = new DigitalEnvelopeModel
        {
            Version = 1,
            Identifier = identifier,
            Timestamp = DateTime.UnixEpoch.ToString("o"),
            RecipientInfo = new RecipientInfo
            {
                KeyEncryptionAlgorithm = "RSA/NONE/PKCS1Padding",
                EncryptedKey = Convert.ToBase64String(encryptedKey),
                CertificateInfo = new CertificateInfo { Issuer = receiver.Issuer, Serial = receiver.SerialNumber }
            },
            EncryptedContentInfo = new EncryptedContentInfo
            {
                ContentType = "signedData",
                ContentEncryptionAlgorithm = "AES/CBC/PKCS5padding",
                EncryptedContent = Convert.ToBase64String(encryptedContent)
            }
        };
        mutator?.Invoke(envelope);
        return Encoding.UTF8.GetBytes(SerializeXml(envelope));
    }

    private static SignedData CreateSignedData(byte[] plainContent, X509Certificate2 signer, bool removeSignature = false)
    {
        var hash = SHA256.HashData(plainContent);
        using var rsa = signer.GetRSAPrivateKey()!;
        var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return new SignedData
        {
            Version = "1",
            SignerInfo = new SignerInfo { SignatureAlgorithm = "SHA256withRSA", Certificate = Convert.ToBase64String(signer.RawData) },
            ContentInfo = Convert.ToBase64String(Cfa.ACHInterbank.Application.Helpers.ZIP.ZipHelper.ZipContend(plainContent, "test.txt")),
            EncryptedDigest = removeSignature ? string.Empty : Convert.ToBase64String(signature)
        };
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

    private static T DeserializeXml<T>(string xml)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);
        return (T)serializer.Deserialize(reader)!;
    }

    private static X509Certificate2 CreateSelfSignedCertificate(string subjectName, bool withPrivateKey, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var certWithKey = request.CreateSelfSigned(notBefore ?? DateTimeOffset.UtcNow.AddDays(-1), notAfter ?? DateTimeOffset.UtcNow.AddDays(30));

        if (!withPrivateKey)
        {
            return X509CertificateLoader.LoadCertificate(certWithKey.Export(X509ContentType.Cert));
        }

        var password = Guid.NewGuid().ToString("N");
        var pfx = certWithKey.Export(X509ContentType.Pkcs12, password);
        return X509CertificateLoader.LoadPkcs12(
            pfx,
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
    }


    private static string ResolveRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ACHInterbank.sln"))) return dir;
            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }

        throw new DirectoryNotFoundException("No se pudo resolver la raíz del repositorio.");
    }
    private sealed class TestRsaKeyProvider(X509Certificate2 signer, X509Certificate2 decrypt) : IRsaKeyProvider
    {
        public X509Certificate2 ObtenerCertificate(string Key_cert) => Key_cert switch
        {
            "CertSign" => signer,
            "CertDecrypt" => decrypt,
            "CertCrypt" => decrypt,
            _ => throw new InvalidOperationException($"Certificado no soportado en test: {Key_cert}.")
        };

        public X509Certificate2 ObtenerCertificateForDecrypt(string? recipientIssuer, string? recipientSerial, string? recipientThumbprint = null) => decrypt;
    }

    private sealed class AuditSink : IDigitalEnvelopeSignatureAuditService
    {
        public List<AuditEvent> Events { get; } = new();
        public Task AuditAsync(string result, string? errorCode, string? signerThumbprint, string? signerSerialNumber, string? signatureAlgorithm, bool failCloseApplied, bool legacyBypassUsed, string actor, CancellationToken cancellationToken = default)
        {
            Events.Add(new AuditEvent(result, errorCode, signerThumbprint, signerSerialNumber, signatureAlgorithm, failCloseApplied, legacyBypassUsed, actor));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEvent(string Result, string? ErrorCode, string? SignerThumbprint, string? SignerSerialNumber, string? SignatureAlgorithm, bool FailCloseApplied, bool LegacyBypassUsed, string Actor);
}
