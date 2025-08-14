using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;

public interface IRsaKeyProviderSingleton
{
    X509Certificate2 ObtenerCertificate(string Key_cert);

//    RSA GetPublicRsa();   // para cifrar (OAEP) y verificar (PSS)
//    RSA GetPrivateRsa();  // para descifrar (OAEP) y firmar (PSS)
//    string GetKeyId();    // KeyId del par de claves/cert
}
