using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;

/// <summary>Versión de certificado que el servicio expone como utilizable para operaciones de sobre digital.</summary>
/// <param name="Id">Identificador persistido de la versión de certificado.</param>
/// <param name="Code">Código del certificado asociado.</param>
/// <param name="DisplayName">Nombre descriptivo del certificado asociado.</param>
/// <param name="FileName">Nombre de archivo registrado para la versión.</param>
/// <param name="ClearingHouseId">Identificador de la cámara asociada cuando existe.</param>
/// <param name="FinancialInstitutionId">Identificador de la entidad financiera asociada cuando existe.</param>
/// <param name="Environment">Ambiente registrado para la versión.</param>
/// <param name="Purpose">Uso funcional registrado para la versión.</param>
/// <param name="VersionNumber">Número de versión del certificado.</param>
/// <param name="HasPrivateKey">Indica si la versión declara material de llave privada.</param>
/// <param name="ThumbprintMasked">Thumbprint enmascarado devuelto por el servicio.</param>
/// <param name="NotBefore">Inicio de vigencia registrado para la versión.</param>
/// <param name="NotAfter">Fin de vigencia registrado para la versión.</param>
/// <param name="CanEncrypt">Indica si el servicio clasifica esta versión como habilitada para cifrado.</param>
/// <param name="CanDecrypt">Indica si el servicio clasifica esta versión como habilitada para descifrado.</param>
public sealed record ManagedDigitalEnvelopeCertificateDto(
    int Id,
    string Code,
    string DisplayName,
    string FileName,
    int? ClearingHouseId,
    int? FinancialInstitutionId,
    CertificateEnvironment Environment,
    CertificatePurpose Purpose,
    int VersionNumber,
    bool HasPrivateKey,
    string ThumbprintMasked,
    DateTime NotBefore,
    DateTime NotAfter,
    bool CanEncrypt,
    bool CanDecrypt);

public sealed record ManagedDigitalEnvelopeRequest(
    int CertificateVersionId,
    string FileName,
    byte[] Content,
    string Actor,
    int? ClearingHouseId = null,
    string OperationMode = "LIVE");

public sealed record ManagedDigitalEnvelopeResult(
    byte[] Content,
    string FileName,
    string ContentType,
    int CertificateVersionId,
    string Thumbprint,
    string CryptographicProfile);

public interface IManagedDigitalEnvelopeService
{
    Task<IReadOnlyList<ManagedDigitalEnvelopeCertificateDto>> ListUsableCertificatesAsync(
        CancellationToken cancellationToken = default);

    Task<ManagedDigitalEnvelopeResult> EncryptAsync(
        ManagedDigitalEnvelopeRequest request,
        CancellationToken cancellationToken = default);

    Task<ManagedDigitalEnvelopeResult> DecryptAsync(
        ManagedDigitalEnvelopeRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ManagedDigitalEnvelopeException : InvalidOperationException
{
    public ManagedDigitalEnvelopeException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
