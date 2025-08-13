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

    public async Task<DigitalEnvelopeModel> CreateEnvelopeAsync(byte[] plaintext, IDictionary<string, string>? aad = null, CancellationToken ct = default)
    {
        // 1) Generar AES256 y nonce
        var aesKey = RandomNumberGenerator.GetBytes(32);
        var nonce = RandomNumberGenerator.GetBytes(12);

        // 2) Cifrar con AES-GCM
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using (var aesGcm = new AesGcm(aesKey))
        {
            byte[]? aadBytes = null;
            if (aad is not null && aad.Count > 0)
                aadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(aad));

            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, aadBytes);
        }

        // 3) Envolver clave (aesKey||nonce) con RSA-OAEP-SHA256
        var publicRsa = _keys.GetPublicRsa();
        var wrapped = publicRsa.Encrypt(aesKey.Concat(nonce).ToArray(), RSAEncryptionPadding.OaepSHA256);

        // 4) Construir sobre
        var envelope = new DigitalEnvelopeModel
        {
            KeyId = _keys.GetKeyId(),
            EncryptedKey = Convert.ToBase64String(wrapped),
            Ciphertext = Convert.ToBase64String(ciphertext),
            Tag = Convert.ToBase64String(tag),
            Nonce = Convert.ToBase64String(nonce),
            Aad = aad,
        };

        // 5) Firma opcional
        envelope.SigAlg = "RSA-PSS-SHA256";
        envelope.Signature = await SignEnvelopeAsync(envelope, ct);

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

