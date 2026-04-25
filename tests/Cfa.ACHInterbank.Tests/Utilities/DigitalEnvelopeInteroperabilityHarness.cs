using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests.Utilities;

internal sealed class DigitalEnvelopeInteroperabilityHarness
{
    public DigitalEnvelopeInteroperabilityReport InspectEnvelope(byte[] envelopeBytes)
    {
        var xml = Encoding.UTF8.GetString(envelopeBytes);
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var envelope = DeserializeXml<DigitalEnvelopeModel>(xml);
        var report = new DigitalEnvelopeInteroperabilityReport
        {
            EnvelopeFormatDetected = "XML",
            RequiredNodesPresent = RequiredNodes(doc, "/envelope/version", "/envelope/identifier", "/envelope/timestamp", "/envelope/recipientInfo/encryptedKey", "/envelope/encryptedContentInfo/encryptedContent"),
            AlgorithmsDeclared = new Dictionary<string, string>
            {
                ["KeyEncryptionAlgorithm"] = envelope.RecipientInfo.KeyEncryptionAlgorithm,
                ["ContentEncryptionAlgorithm"] = envelope.EncryptedContentInfo.ContentEncryptionAlgorithm,
                ["ContentType"] = envelope.EncryptedContentInfo.ContentType
            },
            Identifier = envelope.Identifier,
            IdentifierLength = envelope.Identifier?.Length ?? 0,
            IvDiagnostics = BuildIvDiagnostics(envelope.Identifier ?? string.Empty),
            SignedDataPresent = false,
            SignatureValidationResult = "PENDING_DECRYPT",
            EncryptionMetadata = new Dictionary<string, string>
            {
                ["EncryptedKeyLength"] = envelope.RecipientInfo.EncryptedKey?.Length.ToString() ?? "0",
                ["EncryptedContentLength"] = envelope.EncryptedContentInfo.EncryptedContent?.Length.ToString() ?? "0"
            },
            ZipBase64Validation = new Dictionary<string, bool>
            {
                ["EncryptedKeyBase64"] = IsBase64(envelope.RecipientInfo.EncryptedKey),
                ["EncryptedContentBase64"] = IsBase64(envelope.EncryptedContentInfo.EncryptedContent)
            },
            CertificateMetadata = new Dictionary<string, string>
            {
                ["RecipientIssuer"] = envelope.RecipientInfo.CertificateInfo.Issuer,
                ["RecipientSerial"] = envelope.RecipientInfo.CertificateInfo.Serial
            },
            OfficialVectorPresent = false,
            RequiresOfficialVector = true
        };

        return report;
    }

    public byte[] RoundtripDecrypt(byte[] envelopeBytes, X509Certificate2 signer, X509Certificate2 decryptCert, out DigitalEnvelopeInteroperabilityReport report)
    {
        var service = CreateCryptoService(signer, decryptCert);
        var plain = service.OpenEnvelopeAsync(envelopeBytes, "interop.env").GetAwaiter().GetResult();
        report = InspectEnvelope(envelopeBytes);
        report.SignedDataPresent = true;
        report.SignatureValidationResult = "VALID";
        return plain;
    }

    public byte[] BuildSyntheticEnvelope(byte[] plainContent, X509Certificate2 signer, X509Certificate2 receiver, string fileName = "interop.txt")
    {
        var signedData = CreateSignedData(plainContent, signer, fileName);
        var signedXml = SerializeXml(signedData);
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var identifier = $"{signer.SerialNumber}INTEROPVECTOR001".ToUpperInvariant();
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

    public OfficialVectorLoadResult TryLoadOfficialVector(string rootPath)
    {
        var basePath = Path.Combine(rootPath, "tests", "Cfa.ACHInterbank.Tests", "Fixtures", "DigitalEnvelope", "OfficialVectors");
        var envelopePath = Path.Combine(basePath, "official-envelope.env");
        var plainPath = Path.Combine(basePath, "official-plain-nacha.txt");
        var certPath = Path.Combine(basePath, "official-public-cert.cer");
        var metadataPath = Path.Combine(basePath, "official-metadata.json");

        if (!File.Exists(envelopePath) || !File.Exists(plainPath) || !File.Exists(certPath) || !File.Exists(metadataPath))
        {
            return new OfficialVectorLoadResult(false, basePath, null, null, null, null);
        }

        return new OfficialVectorLoadResult(
            true,
            basePath,
            File.ReadAllBytes(envelopePath),
            File.ReadAllBytes(plainPath),
            X509CertificateLoader.LoadCertificate(File.ReadAllBytes(certPath)),
            File.ReadAllText(metadataPath));
    }

    public static X509Certificate2 CreateSelfSignedCertificate(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    private static SignedData CreateSignedData(byte[] plainContent, X509Certificate2 signer, string fileName)
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
            ContentInfo = Convert.ToBase64String(Cfa.ACHInterbank.Application.Helpers.ZIP.ZipHelper.ZipContend(plainContent, fileName)),
            EncryptedDigest = Convert.ToBase64String(signature)
        };
    }

    private static CryptoServiceScoped CreateCryptoService(X509Certificate2 signer, X509Certificate2 decryptCert)
    {
        var options = new DigitalEnvelopeSignatureValidationOptions
        {
            EnableSignatureValidation = true,
            FailCloseOnInvalidSignature = true,
            FailWhenSignerCertificateMissing = true,
            FailWhenSignerCertificateExpired = true,
            AllowLegacyUnsignedEnvelope = false,
            AuditInvalidSignature = true
        };

        return new CryptoServiceScoped(
            new HarnessRsaKeyProvider(signer, decryptCert),
            new DigitalEnvelopeSignatureValidator(Options.Create(options)),
            new HarnessAudit(),
            Options.Create(options),
            NullLogger<CryptoServiceScoped>.Instance);
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

    private static bool RequiredNodes(XmlDocument document, params string[] xpaths)
    {
        return xpaths.All(x => document.SelectSingleNode(x) != null);
    }

    private static bool IsBase64(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Replace("\n", string.Empty);
        Span<byte> buffer = new byte[trimmed.Length];
        return Convert.TryFromBase64String(trimmed, buffer, out _);
    }

    private static byte[] DeriveIvFromIdentifier(string identifier)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(identifier)).Take(16).ToArray();
    }

    private static IvDiagnostics BuildIvDiagnostics(string identifier)
    {
        var iv = DeriveIvFromIdentifier(identifier);
        return new IvDiagnostics(
            identifier,
            identifier.Length,
            "UTF-8",
            "SHA-256(identifier) first 16 bytes",
            iv.Length,
            Convert.ToHexString(iv));
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

    private sealed class HarnessRsaKeyProvider : IRsaKeyProvider
    {
        private readonly X509Certificate2 _signer;
        private readonly X509Certificate2 _decrypt;

        public HarnessRsaKeyProvider(X509Certificate2 signer, X509Certificate2 decrypt)
        {
            _signer = signer;
            _decrypt = decrypt;
        }

        public X509Certificate2 ObtenerCertificate(string Key_cert)
        {
            return Key_cert switch
            {
                "CertSign" => _signer,
                "CertDecrypt" => _decrypt,
                "CertCrypt" => _decrypt,
                _ => throw new InvalidOperationException($"Key cert no soportado en harness: {Key_cert}.")
            };
        }

        public X509Certificate2 ObtenerCertificateForDecrypt(string? recipientIssuer, string? recipientSerial, string? recipientThumbprint = null)
            => _decrypt;
    }

    private sealed class HarnessAudit : IDigitalEnvelopeSignatureAuditService
    {
        public Task AuditAsync(string result, string? errorCode, string? signerThumbprint, string? signerSerialNumber, string? signatureAlgorithm, bool failCloseApplied, bool legacyBypassUsed, string actor, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

internal sealed record IvDiagnostics(
    string Identifier,
    int IdentifierLength,
    string IdentifierEncoding,
    string DerivationAlgorithm,
    int IvLength,
    string IvHex);

internal sealed class DigitalEnvelopeInteroperabilityReport
{
    public string EnvelopeFormatDetected { get; set; } = "Unknown";
    public bool RequiredNodesPresent { get; set; }
    public Dictionary<string, string> AlgorithmsDeclared { get; set; } = new();
    public string Identifier { get; set; } = string.Empty;
    public int IdentifierLength { get; set; }
    public IvDiagnostics? IvDiagnostics { get; set; }
    public bool SignedDataPresent { get; set; }
    public string SignatureValidationResult { get; set; } = "UNKNOWN";
    public Dictionary<string, string> EncryptionMetadata { get; set; } = new();
    public Dictionary<string, bool> ZipBase64Validation { get; set; } = new();
    public Dictionary<string, string> CertificateMetadata { get; set; } = new();
    public bool OfficialVectorPresent { get; set; }
    public List<string> Differences { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool RequiresOfficialVector { get; set; } = true;
}

internal sealed record OfficialVectorLoadResult(
    bool Present,
    string BasePath,
    byte[]? EnvelopeBytes,
    byte[]? PlainBytes,
    X509Certificate2? PublicCertificate,
    string? MetadataJson);
