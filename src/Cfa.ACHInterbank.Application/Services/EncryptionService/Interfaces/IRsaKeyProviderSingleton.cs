using System.Security.Cryptography;

namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;

public interface IRsaKeyProviderSingleton
{
    RSA GetPublicRsa();   // para cifrar (OAEP) y verificar (PSS)
    RSA GetPrivateRsa();  // para descifrar (OAEP) y firmar (PSS)
    string GetKeyId();    // KeyId del par de claves/cert
}
