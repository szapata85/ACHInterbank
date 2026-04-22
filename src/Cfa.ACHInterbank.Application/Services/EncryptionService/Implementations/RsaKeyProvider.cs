using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Cfa.ACHInterbank.Application.Services.EncryptionService.Implementations;

[
    Scoped
]
public class RsaKeyProvider : IRsaKeyProvider
{
    private readonly AppSettings _appSettings = AppSettings.Settings;
    private readonly IDigitalEnvelopeCertificateResolver _certificateResolver;

    public RsaKeyProvider(IDigitalEnvelopeCertificateResolver certificateResolver)
    {
        _certificateResolver = certificateResolver;
    }


    public X509Certificate2 ObtenerCertificate(string Key_cert)
    {
        var fromResolver = _certificateResolver.ResolveAsync(Key_cert).GetAwaiter().GetResult();
        if (fromResolver.Success && fromResolver.Certificate != null)
        {
            return fromResolver.Certificate;
        }

        var jsonresult = JsonConvert.SerializeObject(_appSettings.Certificates);

        JObject? _servicesCertificates = JObject.Parse(jsonresult);
        List<Certificates>? model = JsonConvert.DeserializeObject<List<Certificates>>(_servicesCertificates[Key_cert]!.ToString());

        // Se define el tipo de almacén y su ubicación
        var storeName = Enum.Parse<StoreName>(model![0].Almacen!);
        var storeLocation = Enum.Parse<StoreLocation>(model![0].Ubicacion!);

        using (var store = new X509Store(storeName, storeLocation))
        {
            store.Open(OpenFlags.ReadOnly);

            // Se busca el certificado según el criterio
            var findType = X509FindType.FindBySerialNumber;
            if (model![0].BuscarPor == BuscarPorEnum.Subject)
            {
                findType = X509FindType.FindBySubjectName;
            }
            else if (model![0].BuscarPor == BuscarPorEnum.Issuer)
            {
                findType = X509FindType.FindByIssuerName;
            }
            else if (model![0].BuscarPor == BuscarPorEnum.SerialNumber)
            {
                findType = X509FindType.FindBySerialNumber;
            }


            // Agrega más casos si necesitas buscar por otras propiedades

            var certCollection = store.Certificates.Find(findType, model![0].ValorDeBusqueda!, false);

            if (certCollection.Count == 0)
            {
                throw new InvalidOperationException($"No se encontró el certificado con {model![0].BuscarPor}={model![0].ValorDeBusqueda}.");
            }

            // Retorna el primer certificado encontrado
            return certCollection[0];
        }
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
