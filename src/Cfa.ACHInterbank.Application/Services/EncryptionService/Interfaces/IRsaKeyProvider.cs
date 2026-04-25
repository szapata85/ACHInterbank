using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;

public interface IRsaKeyProvider
{
    X509Certificate2 ObtenerCertificate(string Key_cert);
    X509Certificate2 ObtenerCertificateForDecrypt(string? recipientIssuer, string? recipientSerial, string? recipientThumbprint = null);
}
