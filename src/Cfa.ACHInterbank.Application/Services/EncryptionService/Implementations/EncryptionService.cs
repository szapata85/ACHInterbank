using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Implementations
{
    [Scoped]
    public class EncryptionService : IDisposable, IEncryptionService
    {

        #region Diposable

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {

                }
                disposed = true;
            }
        }

        // Destructor
        ~EncryptionService()
        {
            Dispose(false);
        }

        #endregion Diposable

        private readonly Lazy<byte[]> _key;
        public EncryptionService(IOptions<ApplicationEncryptionOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var configuredKey = options.Value.Key;
            _key = new Lazy<byte[]>(
                () => ResolveKey(configuredKey),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private static byte[] ResolveKey(string? configuredKey)
        {
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                throw new InvalidOperationException("appSettings:key es obligatorio para el servicio de cifrado.");
            }

            var key = Encoding.UTF8.GetBytes(configuredKey);
            if (key.Length is not (16 or 24 or 32))
            {
                throw new InvalidOperationException("appSettings:key debe codificar una llave AES de 16, 24 o 32 bytes.");
            }

            return key;
        }

        public string Encrypt(string plaintext)
        {
            using var aes = Aes.Create();
            aes.Key = _key.Value;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

            // Combine IV and encrypted data
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string ciphertext)
        {
            var fullCipher = Convert.FromBase64String(ciphertext);

            using var aes = Aes.Create();
            aes.Key = _key.Value;
            aes.IV = new byte[16];
            Array.Copy(fullCipher, 0, aes.IV, 0, aes.IV.Length);

            var encryptedData = new byte[fullCipher.Length - aes.IV.Length];
            Array.Copy(fullCipher, aes.IV.Length, encryptedData, 0, encryptedData.Length);

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
