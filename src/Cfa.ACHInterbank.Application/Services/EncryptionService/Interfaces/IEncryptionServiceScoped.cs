namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;

public interface IEncryptionServiceScoped
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
