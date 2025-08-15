using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;

public class CryptoServiceScoped : ICryptoServiceScoped
{
    private readonly IRsaKeyProviderSingleton _keys;

    public CryptoServiceScoped(IRsaKeyProviderSingleton keys) => _keys = keys;

    //public async Task<DigitalEnvelopeModel> CreateEnvelopeAsync(byte[] plaintext, IDictionary<string, string>? aad = null, CancellationToken ct = default)
    public async Task<byte[]> CreateEnvelopeAsync(byte[] contenidoBytes, string FileName)
    {
        X509Certificate2 certificadoFirmante = _keys.ObtenerCertificate("CertSign");
        X509Certificate2 certificadoReceptor = _keys.ObtenerCertificate("CertCrypt");

        string? mensajeFirmado = CrearMensajeFirmado(contenidoBytes, certificadoFirmante, FileName);

        string? SobreDigitalFirmado = CrearSobreDigital(mensajeFirmado, certificadoReceptor, certificadoFirmante);

        byte[] contenidobytesResp = Encoding.UTF8.GetBytes(SobreDigitalFirmado);

        //string tempath = Path.GetTempPath();

        //string filePath = @$"{tempath}\{FileName}.ENV";

        //File.WriteAllBytes(filePath, contenidobytes);

        return contenidobytesResp;
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
        RSA rsa = certificadoFirmante.GetRSAPrivateKey()!;
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
            ContentInfo = Convert.ToBase64String(Helpers.ZIP.ZipHelper.CoprimeContend(contenidoBytes, FileName)),
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


    private static byte[] CifrarAES(byte[] data, byte[] key, byte[] iv)
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

    private static byte[] DescifrarAES(byte[] data, byte[] key, byte[] iv)
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

    //public Task<string> SignEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default)
    //{
    //    var privateRsa = _keys.GetPrivateRsa();

    //    // Canonical JSON sin Signature (evita ambigüedades)
    //    var clone = new DigitalEnvelopeModel
    //    {
    //        Version = envelope.Version,
    //        EncAlg = envelope.EncAlg,
    //        KeyAlg = envelope.KeyAlg,
    //        KeyId = envelope.KeyId,
    //        EncryptedKey = envelope.EncryptedKey,
    //        Ciphertext = envelope.Ciphertext,
    //        Tag = envelope.Tag,
    //        Nonce = envelope.Nonce,
    //        SigAlg = envelope.SigAlg,
    //        Aad = envelope.Aad
    //    };

    //    var json = JsonSerializer.Serialize(clone, new JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    //    var data = Encoding.UTF8.GetBytes(json);

    //    var sig = privateRsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    //    return Task.FromResult(Convert.ToBase64String(sig));
    //}

    //public Task<bool> VerifyEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default)
    //{
    //    if (string.IsNullOrWhiteSpace(envelope.Signature)) return Task.FromResult(false);

    //    var publicRsa = _keys.GetPublicRsa();
    //    var clone = new DigitalEnvelopeModel
    //    {
    //        Version = envelope.Version,
    //        EncAlg = envelope.EncAlg,
    //        KeyAlg = envelope.KeyAlg,
    //        KeyId = envelope.KeyId,
    //        EncryptedKey = envelope.EncryptedKey,
    //        Ciphertext = envelope.Ciphertext,
    //        Tag = envelope.Tag,
    //        Nonce = envelope.Nonce,
    //        SigAlg = envelope.SigAlg,
    //        Aad = envelope.Aad
    //    };

    //    var json = JsonSerializer.Serialize(clone, new JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    //    var data = Encoding.UTF8.GetBytes(json);
    //    var sigBytes = Convert.FromBase64String(envelope.Signature);

    //    var ok = publicRsa.VerifyData(data, sigBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    //    return Task.FromResult(ok);
    //}

    //public Task<byte[]> OpenEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default)
    //{
    //    // (Opcional) verificar firma antes de abrir
    //    //if (!string.IsNullOrEmpty(envelope.Signature))
    //    //{
    //    //    var verify = VerifyEnvelopeAsync(envelope, ct).GetAwaiter().GetResult();
    //    //    if (!verify) throw new CryptographicException("Firma inválida del sobre.");
    //    //}

    //    var privateRsa = _keys.GetPrivateRsa();

    //    var wrapped = Convert.FromBase64String(envelope.EncryptedKey);
    //    var combined = privateRsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);

    //    var aesKey = combined[..32];
    //    var nonce = combined[32..];

    //    var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
    //    var tag = Convert.FromBase64String(envelope.Tag);

    //    var plaintext = new byte[ciphertext.Length];

    //    using (var aesGcm = new AesGcm(aesKey))
    //    {
    //        byte[]? aadBytes = null;
    //        if (envelope.Aad is not null && envelope.Aad.Count > 0)
    //            aadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope.Aad));

    //        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aadBytes);
    //    }

    //    return Task.FromResult(plaintext);
    //}

    public Task<byte[]> OpenEnvelopeAsync(byte[] contenidoBytes, string FileName)
    {
        throw new NotImplementedException();
    }
}

