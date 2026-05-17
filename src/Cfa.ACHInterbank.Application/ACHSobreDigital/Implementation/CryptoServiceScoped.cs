using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;

[Scoped]
public class CryptoServiceScoped : ICryptoServiceScoped
{
    private readonly IRsaKeyProvider _keys;
    private readonly IDigitalEnvelopeSignatureValidator _signatureValidator;
    private readonly IDigitalEnvelopeSignatureAuditService _signatureAuditService;
    private readonly DigitalEnvelopeSignatureValidationOptions _signatureOptions;
    private readonly ILogger<CryptoServiceScoped> _logger;

    public CryptoServiceScoped(
        IRsaKeyProvider keys,
        IDigitalEnvelopeSignatureValidator signatureValidator,
        IDigitalEnvelopeSignatureAuditService signatureAuditService,
        IOptions<DigitalEnvelopeSignatureValidationOptions> signatureOptions,
        ILogger<CryptoServiceScoped> logger)
    {
        _keys = keys;
        _signatureValidator = signatureValidator;
        _signatureAuditService = signatureAuditService;
        _signatureOptions = signatureOptions.Value ?? new DigitalEnvelopeSignatureValidationOptions();
        _logger = logger;
    }

    //public async Task<DigitalEnvelopeModel> CreateEnvelopeAsync(byte[] plaintext, IDictionary<string, string>? aad = null, CancellationToken ct = default)
    public Task<byte[]> CreateEnvelopeAsync(byte[] contenidoBytes, string FileName)
    {
        X509Certificate2 certificadoFirmante = _keys.ObtenerCertificate("CertSign");
        X509Certificate2 certificadoReceptor = _keys.ObtenerCertificate("CertCrypt");

        string? mensajeFirmado = CrearMensajeFirmado(contenidoBytes, certificadoFirmante, FileName);

        string? SobreDigitalFirmado = CrearSobreDigital(mensajeFirmado, certificadoReceptor, certificadoFirmante);

        byte[] contenidobytesResp = Encoding.UTF8.GetBytes(SobreDigitalFirmado);

        return Task.FromResult(contenidobytesResp);
    }

    public string CrearMensajeFirmado(byte[] contenidoBytes, X509Certificate2 certificadoFirmante, string FileName)
    {

        // Generar hash del contenido
        byte[] hashContenido;
        using (var sha256 = SHA256.Create())
        {
            hashContenido = sha256.ComputeHash(contenidoBytes);
        }

        // Firmar el hash con la clave privada
        using RSA rsa = RequireRsaPrivateKey(certificadoFirmante, "SIGN");
        byte[] firma = rsa.SignHash(hashContenido, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Codificar contenido en ZIP + Base64
        SignedData signed = new()
        {
            Version = "1",
            SignerInfo = new Domain.Models.ACHSobreDigital.SignerInfo()
            {
                SignatureAlgorithm = "SHA256withRSA",
                Certificate = Convert.ToBase64String(certificadoFirmante.RawData)
            },
            ContentInfo = Convert.ToBase64String(Helpers.ZIP.ZipHelper.ZipContend(contenidoBytes, FileName)),
            EncryptedDigest = Convert.ToBase64String(firma)
        };

        return SerializeXml<SignedData>(signed); ;
    }

    public string CrearSobreDigital(string mensajeFirmado, X509Certificate2 certificadoReceptor, X509Certificate2 certificadoFirmante)
    {
        // Generar llave AES y IV
        using (Aes aes = Aes.Create())
        {

            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            // IV = serial + random
            string identifier = certificadoFirmante.SerialNumber + Guid.NewGuid().ToString("N");
            //byte[] iv = aes.IV;

            // Cifrar el mensaje firmado
            byte[] mensajeFirmadoBytes = Encoding.UTF8.GetBytes(mensajeFirmado);
            byte[] mensajeCifrado = CifrarAES(mensajeFirmadoBytes, aes.Key, aes.IV);

            // Cifrar la llave AES con el certificado del receptor

            RSA rsaReceptor = certificadoReceptor.GetRSAPublicKey()!;
            byte[] llaveCifrada = rsaReceptor.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);

            // Construir XML del sobre
            DigitalEnvelopeModel digitalEnvelope = new()
            {
                Version = 1,
                Identifier = identifier.ToUpper(),
                Timestamp = DateTime.UtcNow.ToString("o"),
                RecipientInfo = new RecipientInfo()
                {
                    EncryptedKey = Convert.ToBase64String(llaveCifrada),
                    KeyEncryptionAlgorithm = "RSA/NONE/PKCS1Padding",

                    CertificateInfo = new CertificateInfo()
                    {
                        Issuer = certificadoReceptor.Issuer,
                        Serial = certificadoReceptor.SerialNumber
                    }
                },
                EncryptedContentInfo = new EncryptedContentInfo()
                {
                    ContentType = "signedData",
                    ContentEncryptionAlgorithm = "AES/CBC/PKCS5padding",
                    EncryptedContent = Convert.ToBase64String(mensajeCifrado)
                }
            };

            return SerializeXml<DigitalEnvelopeModel>(digitalEnvelope); ;
        }
    }


    private string SerializeXml<T>(T obj)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        using var sw = new StringWriter();
        serializer.Serialize(sw, obj);
        return sw.ToString();
    }

    private T DeserializeXml<T>(string xmlContent)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        using (StringReader reader = new StringReader(xmlContent))
        {
            return (T)serializer.Deserialize(reader)!;
        }
    }

    private byte[] CifrarAES(byte[] data, byte[] key, byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }

    private byte[] DescifrarAES(byte[] data, byte[] key, byte[] iv)
    {//AES/CBC/PKCS5padding
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }


    private static RSA RequireRsaPrivateKey(X509Certificate2? certificate, string purpose)
    {
        if (certificate == null)
        {
            throw new DigitalEnvelopeSignatureValidationException(
                "CERTIFICATE_PRIVATE_KEY_REQUIRED",
                $"No se encontró certificado para operación {purpose}.");
        }

        if (!certificate.HasPrivateKey)
        {
            throw new DigitalEnvelopeSignatureValidationException(
                "CERTIFICATE_PRIVATE_KEY_REQUIRED",
                $"El certificado requerido para {purpose} no contiene llave privada.");
        }

        try
        {
            var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
            {
                throw new DigitalEnvelopeSignatureValidationException(
                    "CERTIFICATE_PRIVATE_KEY_NOT_AVAILABLE",
                    $"No fue posible obtener la llave RSA privada para operación {purpose}.");
            }

            return rsa;
        }
        catch (DigitalEnvelopeSignatureValidationException)
        {
            throw;
        }
        catch (CryptographicException)
        {
            throw new DigitalEnvelopeSignatureValidationException(
                "CERTIFICATE_PRIVATE_KEY_NOT_AVAILABLE",
                $"No fue posible usar la llave RSA privada para operación {purpose}.");
        }
    }

    private byte[] GenerarIVDesdeIdentifier(string identifier)
    {
        // Aquí puedes derivar el IV desde el identifier como hash o truncamiento
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
        return hash.Take(16).ToArray(); // AES IV = 16 bytes
    }

    public Task<byte[]> OpenEnvelopeAsync(byte[] contenidoBytes, string FileName)
    {
        var validationActor = "CryptoServiceScoped.OpenEnvelopeAsync";
        var failCloseApplied = false;
        var legacyBypassUsed = false;
        string result = "FAILED";
        string? errorCode = null;
        string? signerThumbprint = null;
        string? signerSerial = null;
        string? signatureAlgorithm = null;

        try
        {
            string sobre = Encoding.UTF8.GetString(contenidoBytes);

            DigitalEnvelopeModel objsobre = DeserializeXml<DigitalEnvelopeModel>(sobre);
            var recipientInfo = objsobre.RecipientInfo
                ?? throw new InvalidOperationException("El sobre digital no contiene RecipientInfo.");
            var certificateInfo = recipientInfo.CertificateInfo
                ?? throw new InvalidOperationException("El sobre digital no contiene CertificateInfo.");
            var issuer = certificateInfo.Issuer;
            var serial = certificateInfo.Serial;
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(serial))
            {
                throw new InvalidOperationException("El sobre digital no contiene datos válidos de emisor/serial para el certificado receptor.");
            }
            if (string.IsNullOrWhiteSpace(recipientInfo.EncryptedKey))
            {
                throw new InvalidOperationException("El sobre digital no contiene EncryptedKey válido.");
            }
            var encryptedContentInfo = objsobre.EncryptedContentInfo
                ?? throw new InvalidOperationException("El sobre digital no contiene EncryptedContentInfo.");
            if (string.IsNullOrWhiteSpace(encryptedContentInfo.EncryptedContent))
            {
                throw new InvalidOperationException("El sobre digital no contiene EncryptedContent válido.");
            }

            X509Certificate2 certificadoReceptor = _keys.ObtenerCertificateForDecrypt(
                issuer,
                serial);

            byte[] encryptedKey = Convert.FromBase64String(recipientInfo.EncryptedKey);
            byte[] encryptedContent = Convert.FromBase64String(encryptedContentInfo.EncryptedContent);

            using RSA rsaReceptor = RequireRsaPrivateKey(certificadoReceptor, "DECRYPT");
            byte[] aesKey = rsaReceptor.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);

            byte[] iv = GenerarIVDesdeIdentifier(objsobre.Identifier);
            byte[] contenidoFirmadoBytes = DescifrarAES(encryptedContent, aesKey, iv);

            string xmlFirmadoStr = Encoding.UTF8.GetString(contenidoFirmadoBytes);
            xmlFirmadoStr = xmlFirmadoStr.Substring(xmlFirmadoStr.IndexOf("\n") + 1);

            SignedData objSignedMessageFirmado = DeserializeXml<SignedData>(xmlFirmadoStr);
            string contentinfo = objSignedMessageFirmado.ContentInfo.Replace("\n", "");
            byte[] zipBytes = Convert.FromBase64String(contentinfo);
            (byte[] PlainContent, string _) = Helpers.ZIP.ZipHelper.UnZipContend(zipBytes);

            if (!_signatureOptions.EnableSignatureValidation)
            {
                _logger.LogWarning("EnableSignatureValidation=false detectado; se fuerza validación de firma obligatoria para OpenEnvelopeAsync.");
            }

            var validation = _signatureValidator
                .ValidateAsync(new DigitalEnvelopeSignatureValidationRequest(objSignedMessageFirmado, PlainContent))
                .GetAwaiter()
                .GetResult();

            signerThumbprint = validation.SignerCertificateThumbprint;
            signerSerial = validation.SignerCertificateSerialNumber;
            signatureAlgorithm = validation.SignatureAlgorithm;

            if (!validation.IsValid || !validation.IsVerified)
            {
                errorCode = validation.ErrorCode ?? "SIGNATURE_VALIDATION_FAILED";
                failCloseApplied = true;
                throw new DigitalEnvelopeSignatureValidationException(errorCode, validation.ErrorMessage ?? "La firma del sobre digital no es válida.");
            }

            result = "SUCCESS";
            return Task.FromResult(PlainContent);
        }
        catch (DigitalEnvelopeSignatureValidationException ex)
        {
            errorCode ??= ex.ErrorCode;
            failCloseApplied = true;
            throw;
        }
        catch (Exception ex)
        {
            errorCode ??= "SIGNATURE_VALIDATION_FAILED";
            failCloseApplied = true;
            throw new DigitalEnvelopeSignatureValidationException(errorCode, $"No fue posible validar la firma del sobre digital: {ex.Message}");
        }
        finally
        {
            if (_signatureOptions.AuditInvalidSignature || result == "SUCCESS")
            {
                _signatureAuditService
                    .AuditAsync(
                        result,
                        errorCode,
                        signerThumbprint,
                        signerSerial,
                        signatureAlgorithm,
                        failCloseApplied,
                        legacyBypassUsed,
                        validationActor)
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}
