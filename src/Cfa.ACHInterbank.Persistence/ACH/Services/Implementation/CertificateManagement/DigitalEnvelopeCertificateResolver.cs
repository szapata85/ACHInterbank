using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

[Scoped]
public class DigitalEnvelopeCertificateResolver : IDigitalEnvelopeCertificateResolver
{
    private readonly ICertificateSelectionService _certificateSelectionService;
    private readonly IDigitalEnvelopeCertificateRepository _legacyRepository;
    private readonly ICertificateUsageLogger _certificateUsageLogger;
    private readonly AchDbContext _context;
    private readonly DigitalEnvelopeCertificateOptions _options;
    private readonly ILogger<DigitalEnvelopeCertificateResolver> _logger;

    public DigitalEnvelopeCertificateResolver(
        ICertificateSelectionService certificateSelectionService,
        IDigitalEnvelopeCertificateRepository legacyRepository,
        ICertificateUsageLogger certificateUsageLogger,
        AchDbContext context,
        IOptions<DigitalEnvelopeCertificateOptions> options,
        ILogger<DigitalEnvelopeCertificateResolver> logger)
    {
        _certificateSelectionService = certificateSelectionService;
        _legacyRepository = legacyRepository;
        _certificateUsageLogger = certificateUsageLogger;
        _context = context;
        _logger = logger;
        _options = options.Value ?? new DigitalEnvelopeCertificateOptions();
    }

    public async Task<DigitalEnvelopeCertificateResolutionResult> ResolveAsync(string keyCert, CancellationToken cancellationToken = default)
    {
        var descriptor = ResolveDescriptor(keyCert);
        var warnings = new List<string>();

        if (_options.UseCertificateManagement)
        {
            var cmResult = await TryResolveFromCertificateManagementAsync(descriptor, warnings, cancellationToken);
            if (cmResult != null)
            {
                return cmResult;
            }

            if (!_options.AllowLegacyCertificateFallback)
            {
                await LogResolutionAsync(descriptor, null, "ERROR", "CERT_MGMT_UNAVAILABLE", cancellationToken);
                if (_options.FailIfCertificateManagementUnavailable)
                {
                    throw new InvalidOperationException($"No se encontró certificado activo en Certificate Management para propósito {descriptor.Purpose}.");
                }

                return new DigitalEnvelopeCertificateResolutionResult(
                    false,
                    null,
                    null,
                    DigitalEnvelopeCertificateSource.None,
                    descriptor.Purpose,
                    null,
                    null,
                    null,
                    "CERT_MGMT_UNAVAILABLE",
                    "No se encontró certificado activo en Certificate Management.",
                    warnings);
            }

            warnings.Add("Se utilizó fallback legacy por ausencia o no disponibilidad de certificado activo en Certificate Management.");
        }

        var legacy = await ResolveLegacyAsync(descriptor, warnings, cancellationToken);
        if (legacy.Success)
        {
            return legacy;
        }

        await LogResolutionAsync(descriptor, null, "ERROR", legacy.ErrorCode, cancellationToken);
        return legacy;
    }

    private async Task<DigitalEnvelopeCertificateResolutionResult?> TryResolveFromCertificateManagementAsync(
        (CertificatePurpose Purpose, CertificateHolderType HolderType, bool RequiresPrivateKey, DigitalEnvelopeCertificateType LegacyType, string OperationType) descriptor,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var env = ParseEnvironment(_options.Environment);
        var selected = await _certificateSelectionService.SelectActiveAsync(
            _options.DefaultClearingHouseId,
            env,
            descriptor.Purpose,
            descriptor.HolderType,
            cancellationToken);

        if (selected == null)
        {
            return null;
        }

        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == selected.Id, cancellationToken);

        if (version == null || version.RawPublicCertificate == null || version.RawPublicCertificate.Length == 0)
        {
            warnings.Add("Certificate Management no tiene material de certificado utilizable para el propósito solicitado.");
            return null;
        }

        var cert = TryLoadCertificate(version.RawPublicCertificate, descriptor.RequiresPrivateKey);
        if (cert == null || (descriptor.RequiresPrivateKey && !cert.HasPrivateKey))
        {
            warnings.Add("Certificate Management no tiene private key material disponible para este propósito.");
            return null;
        }

        await _certificateUsageLogger.LogUsageAsync(
            selected.Id,
            descriptor.OperationType,
            Guid.NewGuid().ToString("N"),
            "SUCCESS",
            null,
            "DigitalEnvelopeCertificateResolver",
            cancellationToken);

        await LogResolutionAsync(descriptor, selected.Id, "SUCCESS", null, cancellationToken);

        return new DigitalEnvelopeCertificateResolutionResult(
            true,
            cert,
            selected.Id,
            DigitalEnvelopeCertificateSource.CertificateManagement,
            descriptor.Purpose,
            selected.Thumbprint,
            selected.SerialNumber,
            selected.Subject,
            null,
            null,
            warnings);
    }

    private async Task<DigitalEnvelopeCertificateResolutionResult> ResolveLegacyAsync(
        (CertificatePurpose Purpose, CertificateHolderType HolderType, bool RequiresPrivateKey, DigitalEnvelopeCertificateType LegacyType, string OperationType) descriptor,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var stored = await _legacyRepository.GetLatestAsync(descriptor.LegacyType, cancellationToken);
        if (stored == null)
        {
            return new DigitalEnvelopeCertificateResolutionResult(
                false,
                null,
                null,
                DigitalEnvelopeCertificateSource.None,
                descriptor.Purpose,
                null,
                null,
                null,
                "LEGACY_CERT_NOT_FOUND",
                "No existe certificado legacy para el tipo solicitado.",
                warnings);
        }

        X509Certificate2? cert;
        try
        {
            cert = string.IsNullOrWhiteSpace(stored.Password)
                ? X509CertificateLoader.LoadCertificate(stored.RawData)
                : X509CertificateLoader.LoadPkcs12(stored.RawData, stored.Password, X509KeyStorageFlags.MachineKeySet);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar certificado legacy para propósito {Purpose}.", descriptor.Purpose);
            return new DigitalEnvelopeCertificateResolutionResult(
                false,
                null,
                null,
                DigitalEnvelopeCertificateSource.None,
                descriptor.Purpose,
                null,
                null,
                null,
                "LEGACY_CERT_LOAD_ERROR",
                "No fue posible cargar certificado legacy.",
                warnings);
        }

        var result = _options.UseCertificateManagement ? "FALLBACK_LEGACY" : "SUCCESS";
        var errorCode = _options.UseCertificateManagement ? "CERT_MGMT_FALLBACK" : null;
        await LogResolutionAsync(descriptor, null, result, errorCode, cancellationToken);

        return new DigitalEnvelopeCertificateResolutionResult(
            true,
            cert,
            null,
            DigitalEnvelopeCertificateSource.Legacy,
            descriptor.Purpose,
            cert.Thumbprint,
            cert.SerialNumber,
            cert.Subject,
            null,
            null,
            warnings);
    }

    private static X509Certificate2? TryLoadCertificate(byte[] raw, bool requiresPrivateKey)
    {
        try
        {
            if (!requiresPrivateKey)
            {
                return X509CertificateLoader.LoadCertificate(raw);
            }

            return X509CertificateLoader.LoadPkcs12(raw, password: null, keyStorageFlags: X509KeyStorageFlags.EphemeralKeySet);
        }
        catch
        {
            try
            {
                return X509CertificateLoader.LoadCertificate(raw);
            }
            catch
            {
                return null;
            }
        }
    }

    private async Task LogResolutionAsync(
        (CertificatePurpose Purpose, CertificateHolderType HolderType, bool RequiresPrivateKey, DigitalEnvelopeCertificateType LegacyType, string OperationType) descriptor,
        int? certificateVersionId,
        string result,
        string? errorCode,
        CancellationToken cancellationToken)
    {
        if (!_options.LogCertificateSource)
        {
            return;
        }

        _context.DigitalEnvelopeOperationLogs.Add(new DigitalEnvelopeOperationLog
        {
            Direction = "CertificateResolution",
            ClearingHouseId = _options.DefaultClearingHouseId,
            Environment = ParseEnvironment(_options.Environment),
            Purpose = descriptor.Purpose,
            CertificateVersionId = certificateVersionId,
            Result = result,
            ErrorCode = errorCode,
            Actor = "DigitalEnvelopeCertificateResolver",
            OccurredAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static CertificateEnvironment ParseEnvironment(string environment)
    {
        return Enum.TryParse<CertificateEnvironment>(environment, true, out var parsed)
            ? parsed
            : CertificateEnvironment.Test;
    }

    private static (CertificatePurpose Purpose, CertificateHolderType HolderType, bool RequiresPrivateKey, DigitalEnvelopeCertificateType LegacyType, string OperationType) ResolveDescriptor(string keyCert)
    {
        if (keyCert.Equals("CertCrypt", StringComparison.OrdinalIgnoreCase))
            return (CertificatePurpose.OutboundEncryption, CertificateHolderType.ClearingHouse, false, DigitalEnvelopeCertificateType.EncryptionPublic, "EnvelopeOutboundEncrypt");

        if (keyCert.Equals("CertDecrypt", StringComparison.OrdinalIgnoreCase))
            return (CertificatePurpose.InboundDecryption, CertificateHolderType.Participant, true, DigitalEnvelopeCertificateType.SigningKeyPair, "EnvelopeInboundDecrypt");

        if (keyCert.Equals("CertValidate", StringComparison.OrdinalIgnoreCase))
            return (CertificatePurpose.InboundSignatureValidation, CertificateHolderType.ClearingHouse, false, DigitalEnvelopeCertificateType.EncryptionPublic, "EnvelopeInboundSignatureValidation");

        return (CertificatePurpose.OutboundSigning, CertificateHolderType.Participant, true, DigitalEnvelopeCertificateType.SigningKeyPair, "EnvelopeOutboundSign");
    }
}
