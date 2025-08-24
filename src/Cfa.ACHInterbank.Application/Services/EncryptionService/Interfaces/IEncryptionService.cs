namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
