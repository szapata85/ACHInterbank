using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace Cfa.ACHInterbank.External.Security.Certificates;

[
    Scoped
]
public class RsaKeyProvider : IRsaKeyProvider
{
    private readonly CertificatesSettings _certificateSettings;
    private readonly IDigitalEnvelopeCertificateResolver _certificateResolver;

    public RsaKeyProvider(
        IDigitalEnvelopeCertificateResolver certificateResolver,
        IOptions<CertificatesSettings> certificateOptions)
    {
        _certificateResolver = certificateResolver;
        _certificateSettings = certificateOptions?.Value ?? throw new ArgumentNullException(nameof(certificateOptions));
    }


    public X509Certificate2 ObtenerCertificate(string Key_cert)
    {
        var fromResolver = _certificateResolver.ResolveAsync(Key_cert).GetAwaiter().GetResult();
        if (fromResolver.Success && fromResolver.Certificate != null)
        {
            if (fromResolver.Source == DigitalEnvelopeCertificateSource.CertificateManagement)
            {
                throw new InvalidOperationException("CERTIFICATE_RUNTIME_SECRET_REF_NOT_ALLOWED: runtime core no soporta el uso de SecretRef para certificados de operación.");
            }

            return fromResolver.Certificate;
        }

        var model = Key_cert switch
        {
            "CertSign" => _certificateSettings.CertSign,
            "CertCrypt" => _certificateSettings.CertCrypt,
            _ => null
        };
        var certificate = model?.FirstOrDefault()
            ?? throw new InvalidOperationException($"No existe configuración de certificado para '{Key_cert}'.");

        // Se define el tipo de almacén y su ubicación
        var storeName = Enum.Parse<StoreName>(certificate.Almacen!);
        var storeLocation = Enum.Parse<StoreLocation>(certificate.Ubicacion!);

        using (var store = new X509Store(storeName, storeLocation))
        {
            store.Open(OpenFlags.ReadOnly);

            // Se busca el certificado según el criterio
            var findType = X509FindType.FindBySerialNumber;
            if (certificate.BuscarPor == BuscarPorEnum.Subject)
            {
                findType = X509FindType.FindBySubjectName;
            }
            else if (certificate.BuscarPor == BuscarPorEnum.Issuer)
            {
                findType = X509FindType.FindByIssuerName;
            }
            else if (certificate.BuscarPor == BuscarPorEnum.SerialNumber)
            {
                findType = X509FindType.FindBySerialNumber;
            }


            // Agrega más casos si necesitas buscar por otras propiedades

            var certCollection = store.Certificates.Find(findType, certificate.ValorDeBusqueda!, false);

            if (certCollection.Count == 0)
            {
                throw new InvalidOperationException($"No se encontró el certificado con {certificate.BuscarPor}={certificate.ValorDeBusqueda}.");
            }

            // Retorna el primer certificado encontrado
            return certCollection[0];
        }
    }

    public X509Certificate2 ObtenerCertificateForDecrypt(string? recipientIssuer, string? recipientSerial, string? recipientThumbprint = null)
    {
        var hasHistoricalCriteria = !string.IsNullOrWhiteSpace(recipientIssuer)
                                    || !string.IsNullOrWhiteSpace(recipientSerial)
                                    || !string.IsNullOrWhiteSpace(recipientThumbprint);

        var result = _certificateResolver.ResolveHistoricalDecryptAsync(
                new HistoricalDecryptCertificateCriteria(
                    recipientIssuer,
                    recipientSerial,
                    recipientThumbprint,
                    "RsaKeyProvider.ObtenerCertificateForDecrypt"))
            .GetAwaiter()
            .GetResult();

        if (result.Success && result.Certificate != null)
        {
            if (result.Source == DigitalEnvelopeCertificateSource.CertificateManagement)
            {
                throw new InvalidOperationException("CERTIFICATE_SECRET_PROVIDER_NOT_SUPPORTED_FOR_RUNTIME: decrypt histórico desde SecretRef no está permitido en runtime core.");
            }

            return result.Certificate;
        }

        if (hasHistoricalCriteria)
        {
            throw new InvalidOperationException($"No fue posible resolver certificado histórico para decrypt. Error={result.ErrorCode ?? "HISTORICAL_DECRYPT_FAILED"}.");
        }

        return ObtenerCertificate("CertDecrypt");
    }

    //public X509Certificate2 ObtenerCertificado(string Key_cert)
    //{


    //// Se define el tipo de almacén y su ubicación
    //var storeName = Enum.Parse<StoreName>(_appSettings.Certificados!.Almacen);
    //var storeLocation = Enum.Parse<StoreLocation>(_appSettings.Certificados!.Ubicacion);

    //using (var store = new X509Store(storeName, storeLocation))
    //{
    //    store.Open(OpenFlags.ReadOnly);

    //    // Se busca el certificado según el criterio
    //    var findType = X509FindType.FindByThumbprint;
    //    if (config.BuscarPor == BuscarPorEnum.Subject)
    //    {
    //        findType = X509FindType.FindBySubjectName;
    //    }
    //    // Agrega más casos si necesitas buscar por otras propiedades

    //    var certCollection = store.Certificates.Find(findType, config.ValorDeBusqueda, false);

    //    if (certCollection.Count == 0)
    //    {
    //        throw new InvalidOperationException($"No se encontró el certificado con {config.BuscarPor}={config.ValorDeBusqueda}.");
    //    }

    //    // Retorna el primer certificado encontrado
    //    return certCollection[0];
    //}
    //}



    //public RSA GetPublicRsa() => _publicRsa;
    //public RSA GetPrivateRsa() => _privateRsa;
    //public string GetKeyId() => _keyId;


}
