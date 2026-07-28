using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Application.Helpers.ZIP;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

[Scoped]
public sealed class ManagedDigitalEnvelopeService : IManagedDigitalEnvelopeService
{
    public const string CryptographicProfile = "ACH-V32 XML Digital Envelope; AES-256-CBC/PKCS7; RSA-PKCS1-v1_5; SHA-256-RSA";
    private const int MaximumPlaintextBytes = 50 * 1024 * 1024;

    private readonly AchDbContext _context;
    private readonly ICertificateSecretResolver _secretResolver;
    private readonly IDigitalEnvelopeSignatureValidator _signatureValidator;
    private readonly ILogger<ManagedDigitalEnvelopeService> _logger;

    public ManagedDigitalEnvelopeService(
        AchDbContext context,
        ICertificateSecretResolver secretResolver,
        IDigitalEnvelopeSignatureValidator signatureValidator,
        ILogger<ManagedDigitalEnvelopeService> logger)
    {
        _context = context;
        _secretResolver = secretResolver;
        _signatureValidator = signatureValidator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ManagedDigitalEnvelopeCertificateDto>> ListUsableCertificatesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var versions = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .Where(x => x.Status == CertificateStatus.Active
                        && x.NotBefore <= now
                        && x.NotAfter > now
                        && x.RawPublicCertificate != null
                        && (x.Purpose == CertificatePurpose.OutboundEncryption
                            || x.Purpose == CertificatePurpose.InboundDecryption))
            .OrderBy(x => x.DigitalCertificate.DisplayName)
            .ThenByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions
            .Where(HasUsableRsaPublicKey)
            .Select(x => new ManagedDigitalEnvelopeCertificateDto(
                x.Id,
                x.DigitalCertificate.Code,
                x.DigitalCertificate.DisplayName,
                x.FileName,
                x.ClearingHouseId,
                x.Environment,
                x.Purpose,
                x.VersionNumber,
                x.HasPrivateKey,
                MaskThumbprint(x.Thumbprint),
                x.NotBefore,
                x.NotAfter,
                CanEncrypt: true,
                CanDecrypt: x.Purpose == CertificatePurpose.InboundDecryption
                            && x.HasPrivateKey
                            && !string.IsNullOrWhiteSpace(x.SecretRef)))
            .ToList();
    }

    public async Task<ManagedDigitalEnvelopeResult> EncryptAsync(
        ManagedDigitalEnvelopeRequest request,
        CancellationToken cancellationToken = default)
    {
        var safeInputName = SanitizeFileName(request.FileName);
        var outputName = BuildEncryptedFileName(safeInputName);
        DigitalCertificateVersion? recipient = null;

        try
        {
            ValidateContent(request.Content);
            recipient = await GetActiveVersionAsync(request.CertificateVersionId, cancellationToken);
            if (recipient.Purpose is not (CertificatePurpose.OutboundEncryption or CertificatePurpose.InboundDecryption))
            {
                throw Error("CERTIFICATE_PURPOSE_INVALID", "El certificado seleccionado no está habilitado para cifrado de sobre digital.");
            }

            using var recipientCertificate = LoadPublicCertificate(recipient);
            ValidateRsaCertificate(recipientCertificate, requirePrivateKey: false);

            var now = DateTime.UtcNow;
            var signerVersion = await _context.DigitalCertificateVersions
                .AsNoTracking()
                .Include(x => x.DigitalCertificate)
                .Where(x => x.Status == CertificateStatus.Active
                            && x.NotBefore <= now
                            && x.NotAfter > now
                            && x.ClearingHouseId == recipient.ClearingHouseId
                            && x.Environment == recipient.Environment
                            && x.Purpose == CertificatePurpose.OutboundSigning
                            && x.HolderType == CertificateHolderType.Participant)
                .OrderByDescending(x => x.VersionNumber)
                .ThenByDescending(x => x.ActivatedAtUtc)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw Error("SIGNING_CERTIFICATE_NOT_FOUND", "No existe un certificado de firma activo y vigente para la cámara seleccionada.");

            using var signerCertificate = await ResolvePrivateCertificateAsync(signerVersion, request.Actor, cancellationToken);
            ValidateRsaCertificate(signerCertificate, requirePrivateKey: true);

            var signedData = CreateSignedData(request.Content, safeInputName, signerCertificate);
            var signedXml = SerializeXml(signedData);
            var identifier = $"{signerCertificate.SerialNumber}{Guid.NewGuid():N}".ToUpperInvariant();
            var iv = DeriveIv(identifier);

            byte[] encryptedContent;
            byte[] encryptedKey;
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                encryptedContent = TransformAes(Encoding.UTF8.GetBytes(signedXml), aes.Key, iv, encrypt: true);
                using var rsa = recipientCertificate.GetRSAPublicKey()
                    ?? throw Error("CERTIFICATE_PUBLIC_KEY_UNUSABLE", "El certificado seleccionado no contiene una clave pública RSA utilizable.");
                encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);
                CryptographicOperations.ZeroMemory(aes.Key);
            }

            var envelope = new DigitalEnvelopeModel
            {
                Version = 1,
                Identifier = identifier,
                Timestamp = DateTime.UtcNow.ToString("O"),
                RecipientInfo = new RecipientInfo
                {
                    CertificateInfo = new CertificateInfo
                    {
                        Issuer = recipientCertificate.Issuer,
                        Serial = recipientCertificate.SerialNumber
                    },
                    KeyEncryptionAlgorithm = "RSA/NONE/PKCS1Padding",
                    EncryptedKey = Convert.ToBase64String(encryptedKey)
                },
                EncryptedContentInfo = new EncryptedContentInfo
                {
                    ContentType = "signedData",
                    ContentEncryptionAlgorithm = "AES/CBC/PKCS5padding",
                    EncryptedContent = Convert.ToBase64String(encryptedContent)
                }
            };

            var output = Encoding.UTF8.GetBytes(SerializeXml(envelope));
            await AuditAsync("Encrypt", recipient, safeInputName, outputName, request.Content, output, "SUCCESS", null, request.Actor, cancellationToken);
            _logger.LogInformation(
                "Digital envelope encryption succeeded. CertificateVersionId={CertificateVersionId} Thumbprint={Thumbprint} FileName={FileName} SizeBefore={SizeBefore} SizeAfter={SizeAfter}",
                recipient.Id,
                MaskThumbprint(recipient.Thumbprint),
                safeInputName,
                request.Content.LongLength,
                output.LongLength);

            return new ManagedDigitalEnvelopeResult(
                output,
                outputName,
                "application/octet-stream",
                recipient.Id,
                recipient.Thumbprint,
                CryptographicProfile);
        }
        catch (Exception ex)
        {
            var managed = NormalizeException(ex, "ENVELOPE_ENCRYPT_FAILED", "No fue posible cifrar el archivo.");
            await AuditFailureBestEffortAsync("Encrypt", recipient, safeInputName, outputName, request.Content, managed.ErrorCode, request.Actor, cancellationToken);
            _logger.LogWarning(
                "Digital envelope encryption failed. CertificateVersionId={CertificateVersionId} FileName={FileName} ErrorCode={ErrorCode}",
                recipient?.Id,
                safeInputName,
                managed.ErrorCode);
            throw managed;
        }
    }

    public async Task<ManagedDigitalEnvelopeResult> DecryptAsync(
        ManagedDigitalEnvelopeRequest request,
        CancellationToken cancellationToken = default)
    {
        var safeInputName = SanitizeFileName(request.FileName);
        var outputName = BuildDecryptedFileName(safeInputName);
        DigitalCertificateVersion? recipient = null;

        try
        {
            ValidateContent(request.Content);
            recipient = await GetActiveVersionAsync(request.CertificateVersionId, cancellationToken);
            if (recipient.Purpose != CertificatePurpose.InboundDecryption || !recipient.HasPrivateKey)
            {
                throw Error("CERTIFICATE_PRIVATE_KEY_REQUIRED", "El certificado seleccionado no está habilitado para descifrado o no contiene clave privada.");
            }

            using var recipientCertificate = await ResolvePrivateCertificateAsync(recipient, request.Actor, cancellationToken);
            ValidateRsaCertificate(recipientCertificate, requirePrivateKey: true);

            DigitalEnvelopeModel envelope;
            try
            {
                envelope = DeserializeXml<DigitalEnvelopeModel>(Encoding.UTF8.GetString(request.Content));
            }
            catch (Exception ex) when (ex is InvalidOperationException or XmlException)
            {
                throw Error("ENVELOPE_INVALID", "El archivo no contiene un sobre digital XML válido.");
            }

            ValidateEnvelope(envelope, recipientCertificate);
            var encryptedKey = Convert.FromBase64String(envelope.RecipientInfo.EncryptedKey);
            var encryptedContent = Convert.FromBase64String(envelope.EncryptedContentInfo.EncryptedContent);

            byte[] aesKey;
            using (var rsa = recipientCertificate.GetRSAPrivateKey()
                             ?? throw Error("CERTIFICATE_PRIVATE_KEY_REQUIRED", "No fue posible abrir la clave privada del certificado seleccionado."))
            {
                try
                {
                    aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);
                }
                catch (CryptographicException)
                {
                    throw Error("CERTIFICATE_MISMATCH", "El certificado seleccionado no corresponde al destinatario del sobre.");
                }
            }

            byte[] signedXmlBytes;
            try
            {
                signedXmlBytes = TransformAes(encryptedContent, aesKey, DeriveIv(envelope.Identifier), encrypt: false);
            }
            catch (CryptographicException)
            {
                throw Error("ENVELOPE_INTEGRITY_INVALID", "El sobre digital está alterado o corrupto.");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(aesKey);
            }

            SignedData signedData;
            try
            {
                signedData = DeserializeXml<SignedData>(Encoding.UTF8.GetString(signedXmlBytes));
            }
            catch (Exception ex) when (ex is InvalidOperationException or XmlException)
            {
                throw Error("SIGNED_CONTENT_INVALID", "El contenido firmado del sobre no es válido.");
            }

            byte[] plain;
            try
            {
                var zipped = Convert.FromBase64String(signedData.ContentInfo.Replace("\r", string.Empty).Replace("\n", string.Empty));
                (plain, _) = ZipHelper.UnZipContend(zipped);
            }
            catch
            {
                throw Error("SIGNED_CONTENT_INVALID", "No fue posible recuperar el contenido comprimido del sobre.");
            }

            if (plain.Length == 0 || plain.Length > MaximumPlaintextBytes)
            {
                CryptographicOperations.ZeroMemory(plain);
                throw Error("PLAINTEXT_SIZE_INVALID", "El contenido recuperado tiene un tamaño no permitido.");
            }

            var signatureValidation = await _signatureValidator.ValidateAsync(
                new DigitalEnvelopeSignatureValidationRequest(signedData, plain),
                cancellationToken);
            if (!signatureValidation.IsValid || !signatureValidation.IsVerified)
            {
                CryptographicOperations.ZeroMemory(plain);
                throw Error(
                    signatureValidation.ErrorCode ?? "SIGNATURE_VALIDATION_FAILED",
                    signatureValidation.ErrorMessage ?? "La firma del sobre digital no es válida.");
            }

            await AuditAsync("Decrypt", recipient, safeInputName, outputName, plain, request.Content, "SUCCESS", null, request.Actor, cancellationToken);
            _logger.LogInformation(
                "Digital envelope decryption succeeded. CertificateVersionId={CertificateVersionId} Thumbprint={Thumbprint} FileName={FileName} SizeBefore={SizeBefore} SizeAfter={SizeAfter}",
                recipient.Id,
                MaskThumbprint(recipient.Thumbprint),
                safeInputName,
                request.Content.LongLength,
                plain.LongLength);

            return new ManagedDigitalEnvelopeResult(
                plain,
                outputName,
                "application/octet-stream",
                recipient.Id,
                recipient.Thumbprint,
                CryptographicProfile);
        }
        catch (Exception ex)
        {
            var managed = NormalizeException(ex, "ENVELOPE_DECRYPT_FAILED", "No fue posible descifrar el archivo.");
            await AuditFailureBestEffortAsync("Decrypt", recipient, safeInputName, outputName, request.Content, managed.ErrorCode, request.Actor, cancellationToken);
            _logger.LogWarning(
                "Digital envelope decryption failed. CertificateVersionId={CertificateVersionId} FileName={FileName} ErrorCode={ErrorCode}",
                recipient?.Id,
                safeInputName,
                managed.ErrorCode);
            throw managed;
        }
    }

    public static string BuildEncryptedFileName(string fileName)
        => $"{SanitizeFileName(fileName)}.ENV";

    public static string BuildDecryptedFileName(string fileName)
    {
        var safeName = SanitizeFileName(fileName);
        if (!safeName.EndsWith(".ENV", StringComparison.OrdinalIgnoreCase))
        {
            throw Error("ENVELOPE_EXTENSION_REQUIRED", "El archivo para descifrar debe terminar en .ENV.");
        }

        var result = safeName[..^4];
        if (string.IsNullOrWhiteSpace(result))
        {
            throw Error("FILE_NAME_INVALID", "El nombre original del archivo no es válido.");
        }

        return result;
    }

    public static string SanitizeFileName(string fileName)
    {
        var normalized = (fileName ?? string.Empty).Replace('\\', '/');
        var safeName = Path.GetFileName(normalized).Trim();
        if (string.IsNullOrWhiteSpace(safeName) || safeName is "." or "..")
        {
            throw Error("FILE_NAME_INVALID", "El nombre del archivo no es válido.");
        }

        return safeName;
    }

    private async Task<DigitalCertificateVersion> GetActiveVersionAsync(int versionId, CancellationToken cancellationToken)
    {
        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .Include(x => x.DigitalCertificate)
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken)
            ?? throw Error("CERTIFICATE_NOT_FOUND", "El certificado seleccionado no existe.");

        var now = DateTime.UtcNow;
        if (version.Status != CertificateStatus.Active)
            throw Error("CERTIFICATE_INACTIVE", "El certificado seleccionado no está activo.");
        if (version.NotBefore > now)
            throw Error("CERTIFICATE_NOT_YET_VALID", "El certificado seleccionado aún no está vigente.");
        if (version.NotAfter <= now)
            throw Error("CERTIFICATE_EXPIRED", "El certificado seleccionado está vencido.");

        return version;
    }

    private async Task<X509Certificate2> ResolvePrivateCertificateAsync(
        DigitalCertificateVersion version,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!version.HasPrivateKey || string.IsNullOrWhiteSpace(version.SecretRef))
            throw Error("CERTIFICATE_PRIVATE_KEY_REQUIRED", "El certificado no tiene material privado disponible.");

        var resolution = await _secretResolver.ResolveAsync(
            new CertificateSecretResolutionRequest(
                version.Id,
                version.Purpose,
                version.PrivateMaterialStorageMode,
                version.SecretRef,
                actor),
            cancellationToken);

        if (!resolution.Success || resolution.Material is null || !resolution.Material.HasPrivateKey)
        {
            throw Error(
                resolution.ErrorCode ?? "CERTIFICATE_PRIVATE_KEY_UNAVAILABLE",
                "No fue posible resolver el material privado del certificado.");
        }

        return resolution.Material.Certificate;
    }

    private static SignedData CreateSignedData(byte[] content, string fileName, X509Certificate2 signer)
    {
        using var rsa = signer.GetRSAPrivateKey()
            ?? throw Error("SIGNING_PRIVATE_KEY_REQUIRED", "El certificado de firma no contiene clave privada RSA.");
        var signature = rsa.SignHash(SHA256.HashData(content), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return new SignedData
        {
            Version = "1",
            SignerInfo = new SignerInfo
            {
                SignatureAlgorithm = "SHA256withRSA",
                Certificate = Convert.ToBase64String(signer.RawData)
            },
            ContentInfo = Convert.ToBase64String(ZipHelper.ZipContend(content, fileName)),
            EncryptedDigest = Convert.ToBase64String(signature)
        };
    }

    private static void ValidateEnvelope(DigitalEnvelopeModel envelope, X509Certificate2 recipient)
    {
        if (envelope.Version != 1
            || string.IsNullOrWhiteSpace(envelope.Identifier)
            || envelope.RecipientInfo?.CertificateInfo is null
            || envelope.EncryptedContentInfo is null
            || string.IsNullOrWhiteSpace(envelope.RecipientInfo.EncryptedKey)
            || string.IsNullOrWhiteSpace(envelope.EncryptedContentInfo.EncryptedContent))
        {
            throw Error("ENVELOPE_INVALID", "El sobre digital no contiene todos los campos obligatorios.");
        }

        if (!string.Equals(envelope.RecipientInfo.KeyEncryptionAlgorithm, "RSA/NONE/PKCS1Padding", StringComparison.Ordinal)
            || !string.Equals(envelope.EncryptedContentInfo.ContentEncryptionAlgorithm, "AES/CBC/PKCS5padding", StringComparison.Ordinal)
            || !string.Equals(envelope.EncryptedContentInfo.ContentType, "signedData", StringComparison.Ordinal))
        {
            throw Error("ENVELOPE_ALGORITHM_UNSUPPORTED", "El sobre digital declara algoritmos no permitidos.");
        }

        if (!string.Equals(NormalizeSerial(envelope.RecipientInfo.CertificateInfo.Serial), NormalizeSerial(recipient.SerialNumber), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(envelope.RecipientInfo.CertificateInfo.Issuer?.Trim(), recipient.Issuer.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw Error("CERTIFICATE_MISMATCH", "El certificado seleccionado no corresponde al destinatario del sobre.");
        }
    }

    private static X509Certificate2 LoadPublicCertificate(DigitalCertificateVersion version)
    {
        if (version.RawPublicCertificate is not { Length: > 0 })
            throw Error("CERTIFICATE_PUBLIC_KEY_UNAVAILABLE", "El certificado no contiene material público disponible.");

        try
        {
            return X509CertificateLoader.LoadCertificate(version.RawPublicCertificate);
        }
        catch (CryptographicException)
        {
            throw Error("CERTIFICATE_PUBLIC_KEY_UNUSABLE", "El material público del certificado no es válido.");
        }
    }

    private static void ValidateRsaCertificate(X509Certificate2 certificate, bool requirePrivateKey)
    {
        var now = DateTime.UtcNow;
        if (certificate.NotBefore.ToUniversalTime() > now)
            throw Error("CERTIFICATE_NOT_YET_VALID", "El certificado aún no está vigente.");
        if (certificate.NotAfter.ToUniversalTime() <= now)
            throw Error("CERTIFICATE_EXPIRED", "El certificado está vencido.");
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is null || rsa.KeySize < 2048)
            throw Error("CERTIFICATE_PUBLIC_KEY_UNUSABLE", "El certificado requiere una clave pública RSA de al menos 2048 bits.");
        if (requirePrivateKey && !certificate.HasPrivateKey)
            throw Error("CERTIFICATE_PRIVATE_KEY_REQUIRED", "El certificado no contiene clave privada.");
    }

    private static bool HasUsableRsaPublicKey(DigitalCertificateVersion version)
    {
        try
        {
            using var certificate = LoadPublicCertificate(version);
            using var rsa = certificate.GetRSAPublicKey();
            return rsa is { KeySize: >= 2048 };
        }
        catch
        {
            return false;
        }
    }

    private static byte[] TransformAes(byte[] content, byte[] key, byte[] iv, bool encrypt)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
        return transform.TransformFinalBlock(content, 0, content.Length);
    }

    private static byte[] DeriveIv(string identifier)
        => SHA256.HashData(Encoding.UTF8.GetBytes(identifier)).AsSpan(0, 16).ToArray();

    private static string SerializeXml<T>(T value)
    {
        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            OmitXmlDeclaration = false
        };
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            serializer.Serialize(writer, value);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static T DeserializeXml<T>(string xml)
    {
        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 100 * 1024 * 1024
        };
        using var textReader = new StringReader(xml);
        using var reader = XmlReader.Create(textReader, settings);
        return (T)(serializer.Deserialize(reader)
                   ?? throw Error("ENVELOPE_INVALID", "El sobre digital no se pudo deserializar."));
    }

    private static void ValidateContent(byte[] content)
    {
        if (content is not { Length: > 0 })
            throw Error("FILE_EMPTY", "El archivo no puede estar vacío.");
        if (content.Length > MaximumPlaintextBytes)
            throw Error("FILE_TOO_LARGE", "El archivo supera el máximo permitido de 50 MB.");
    }

    private async Task AuditAsync(
        string direction,
        DigitalCertificateVersion version,
        string inputName,
        string outputName,
        byte[] plain,
        byte[] encrypted,
        string result,
        string? errorCode,
        string actor,
        CancellationToken cancellationToken)
    {
        _context.DigitalEnvelopeOperationLogs.Add(new DigitalEnvelopeOperationLog
        {
            Direction = direction,
            ClearingHouseId = version.ClearingHouseId,
            Environment = version.Environment,
            Purpose = version.Purpose,
            CertificateVersionId = version.Id,
            FileNameIn = inputName,
            FileNameOut = outputName,
            HashPlainSha256 = Convert.ToHexString(SHA256.HashData(plain)),
            HashEncryptedSha256 = Convert.ToHexString(SHA256.HashData(encrypted)),
            SizeBefore = direction == "Encrypt" ? plain.LongLength : encrypted.LongLength,
            SizeAfter = direction == "Encrypt" ? encrypted.LongLength : plain.LongLength,
            Result = result,
            ErrorCode = errorCode,
            Actor = string.IsNullOrWhiteSpace(actor) ? "api" : actor[..Math.Min(actor.Length, 120)]
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AuditFailureBestEffortAsync(
        string direction,
        DigitalCertificateVersion? version,
        string inputName,
        string outputName,
        byte[] input,
        string errorCode,
        string actor,
        CancellationToken cancellationToken)
    {
        if (version is null) return;
        try
        {
            _context.ChangeTracker.Clear();
            _context.DigitalEnvelopeOperationLogs.Add(new DigitalEnvelopeOperationLog
            {
                Direction = direction,
                ClearingHouseId = version.ClearingHouseId,
                Environment = version.Environment,
                Purpose = version.Purpose,
                CertificateVersionId = version.Id,
                FileNameIn = inputName,
                FileNameOut = outputName,
                SizeBefore = input.LongLength,
                Result = "FAILED",
                ErrorCode = errorCode,
                Actor = string.IsNullOrWhiteSpace(actor) ? "api" : actor[..Math.Min(actor.Length, 120)]
            });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // The primary cryptographic error must not be hidden by a secondary audit failure.
        }
    }

    private static ManagedDigitalEnvelopeException NormalizeException(Exception exception, string fallbackCode, string fallbackMessage)
        => exception as ManagedDigitalEnvelopeException
           ?? new ManagedDigitalEnvelopeException(fallbackCode, fallbackMessage);

    private static ManagedDigitalEnvelopeException Error(string code, string message)
        => new(code, message);

    private static string NormalizeSerial(string? serial)
        => (serial ?? string.Empty).Replace(" ", string.Empty).TrimStart('0');

    private static string MaskThumbprint(string thumbprint)
        => string.IsNullOrWhiteSpace(thumbprint)
            ? "****"
            : thumbprint.Length <= 12
                ? "****"
                : $"{thumbprint[..6]}...{thumbprint[^6..]}";
}
