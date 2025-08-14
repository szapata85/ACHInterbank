using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Implementation;

public class CryptoServiceScoped : ICryptoServiceScoped
{
    private readonly IRsaKeyProviderSingleton _keys;

    public CryptoServiceScoped(IRsaKeyProviderSingleton keys) => _keys = keys;

    //public async Task<DigitalEnvelopeModel> CreateEnvelopeAsync(byte[] plaintext, IDictionary<string, string>? aad = null, CancellationToken ct = default)
    public async Task<byte[]> CreateEnvelopeAsync(byte[] contenidoBytes)
    {
        // Generar hash del contenido
        byte[] hashContenido;
        using (var sha256 = SHA256.Create())
        {
            hashContenido = sha256.ComputeHash(contenidoBytes);
        }
        
        // Firmar el hash con la clave privada
        RSA rsa = certificadoFirmante.GetRSAPrivateKey();
        byte[] firma = rsa.SignHash(hashContenido, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return envelope;
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

    public Task<byte[]> OpenEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default)
    {
        // (Opcional) verificar firma antes de abrir
        //if (!string.IsNullOrEmpty(envelope.Signature))
        //{
        //    var verify = VerifyEnvelopeAsync(envelope, ct).GetAwaiter().GetResult();
        //    if (!verify) throw new CryptographicException("Firma inválida del sobre.");
        //}

        var privateRsa = _keys.GetPrivateRsa();

        var wrapped = Convert.FromBase64String(envelope.EncryptedKey);
        var combined = privateRsa.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256);

        var aesKey = combined[..32];
        var nonce = combined[32..];

        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
        var tag = Convert.FromBase64String(envelope.Tag);

        var plaintext = new byte[ciphertext.Length];

        using (var aesGcm = new AesGcm(aesKey))
        {
            byte[]? aadBytes = null;
            if (envelope.Aad is not null && envelope.Aad.Count > 0)
                aadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope.Aad));

            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, aadBytes);
        }

        return Task.FromResult(plaintext);
    }
}

