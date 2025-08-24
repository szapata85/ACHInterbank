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
    private readonly IRsaKeyProvider _keys;

    public CryptoServiceScoped(IRsaKeyProvider keys) => _keys = keys;

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

    private byte[] GenerarIVDesdeIdentifier(string identifier)
    {
        // Aquí puedes derivar el IV desde el identifier como hash o truncamiento
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
        return hash.Take(16).ToArray(); // AES IV = 16 bytes
    }

    public Task<byte[]> OpenEnvelopeAsync(byte[] contenidoBytes, string FileName)
    {
        //X509Certificate2 certificadoFirmante = _keys.ObtenerCertificate("CertSign");
        X509Certificate2 certificadoReceptor = _keys.ObtenerCertificate("CertSign");
        string sobre = Encoding.UTF8.GetString(contenidoBytes);


        DigitalEnvelopeModel objsobre = DeserializeXml<DigitalEnvelopeModel>(sobre);

        byte[] encryptedKey = Convert.FromBase64String(objsobre.RecipientInfo.EncryptedKey);
        byte[] encryptedContent = Convert.FromBase64String(objsobre.EncryptedContentInfo.EncryptedContent);


        // Desencriptar llave simétrica con clave privada del receptor
        RSA rsaReceptor = certificadoReceptor.GetRSAPrivateKey()!;
        byte[] aesKey = rsaReceptor.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);

        byte[] iv = GenerarIVDesdeIdentifier(objsobre.Identifier); // Método adicional

        byte[] contenidoFirmadoBytes = DescifrarAES(encryptedContent, aesKey, iv);

        string xmlFirmadoStr = Encoding.UTF8.GetString(contenidoFirmadoBytes);
        xmlFirmadoStr = xmlFirmadoStr.Substring(xmlFirmadoStr.IndexOf("\n") + 1);

        SignedData objSignedMessageFirmado = DeserializeXml<SignedData>(xmlFirmadoStr);

        string contentinfo = objSignedMessageFirmado.ContentInfo.Replace("\n", "");

        byte[] zipBytes = Convert.FromBase64String(contentinfo);

        (byte[], string) contenidoOriginal = Helpers.ZIP.ZipHelper.UnZipContend(zipBytes);

        string firmaB64 = objSignedMessageFirmado.EncryptedDigest.Replace("\n", "");

        byte[] hash = SHA256.Create().ComputeHash(contenidoOriginal.Item1);

        byte[] firma = Convert.FromBase64String(firmaB64);

        //X509Certificate2 certificadoFirmante = new X509Certificate2(Convert.FromBase64String(objSignedMessageFirmado.SignerInfo.Certificate));

        X509Certificate2 certificadoFirmante = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(objSignedMessageFirmado.SignerInfo.Certificate), password: null);
        RSA rsaFirmante = certificadoFirmante.GetRSAPublicKey()!;



        //Process.Start("explorer.exe", pathENVRelative);
        //bool VERIFICADO = false;
        //if (rsaFirmante.VerifyHash(hash, firma, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
        //{
        //    VERIFICADO = true;
        //    string filePath = @$"{pathENVRelative}\{contenidoOriginal.Item2}";
        //    File.WriteAllBytes(filePath, contenidoOriginal.Item1);


        //    Process.Start("explorer.exe", pathENVRelative);
        //};

        //rsaFirmante.VerifyHash(hash, firma, HashAlgorithmName.SHA256 , RSASignaturePadding.Pkcs1);


        byte[] contenidobytesResp = contenidoOriginal.Item1;


        return Task.FromResult(contenidobytesResp);
    }
}

