using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Buffers.Binary;
using System.Text;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Microsoft.AspNetCore.DataProtection;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

public sealed class DataProtectionCertificatePrivateMaterialProtector : ICertificatePrivateMaterialProtector
{
    private const string Purpose = "Cfa.ACHInterbank.CertificatePrivateMaterial.v1";
    private readonly IDataProtector _protector;

    public DataProtectionCertificatePrivateMaterialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public byte[] Protect(byte[] rawPkcs12, string password)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var payload = new byte[sizeof(int) + passwordBytes.Length + rawPkcs12.Length];
        try
        {
            BinaryPrimitives.WriteInt32LittleEndian(payload, passwordBytes.Length);
            passwordBytes.CopyTo(payload, sizeof(int));
            rawPkcs12.CopyTo(payload, sizeof(int) + passwordBytes.Length);
            return _protector.Protect(payload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(payload);
        }
    }

    public X509Certificate2 Unprotect(byte[] protectedMaterial)
    {
        var payload = _protector.Unprotect(protectedMaterial);
        byte[]? pkcs12 = null;
        char[]? password = null;
        try
        {
            if (payload.Length < sizeof(int)) throw new CryptographicException("Protected certificate payload is invalid.");
            var passwordLength = BinaryPrimitives.ReadInt32LittleEndian(payload);
            if (passwordLength < 0 || passwordLength > payload.Length - sizeof(int))
                throw new CryptographicException("Protected certificate payload is invalid.");

            var passwordBytes = payload.AsSpan(sizeof(int), passwordLength);
            password = new char[Encoding.UTF8.GetCharCount(passwordBytes)];
            Encoding.UTF8.GetChars(passwordBytes, password);
            pkcs12 = payload[(sizeof(int) + passwordLength)..];
            return X509CertificateLoader.LoadPkcs12(pkcs12, password, X509KeyStorageFlags.EphemeralKeySet);
        }
        finally
        {
            if (pkcs12 is not null) CryptographicOperations.ZeroMemory(pkcs12);
            if (password is not null) Array.Clear(password);
            CryptographicOperations.ZeroMemory(payload);
        }
    }
}
