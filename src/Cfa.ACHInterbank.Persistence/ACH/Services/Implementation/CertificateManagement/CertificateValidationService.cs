using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

[Scoped]
public class CertificateValidationService : ICertificateValidationService
{
    private readonly AchDbContext _context;

    public CertificateValidationService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<CertificateValidationResultDto> ValidateForActivationAsync(int versionId, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == versionId, cancellationToken);

        if (version == null)
        {
            errors.Add("Versión de certificado no encontrada.");
            return new CertificateValidationResultDto(false, errors);
        }

        var now = DateTime.UtcNow;
        if (version.NotAfter <= now) errors.Add("El certificado está expirado.");
        if (version.NotBefore > now) errors.Add("El certificado no está vigente todavía.");

        var requiresPrivate = version.Purpose is CertificatePurpose.OutboundSigning
            or CertificatePurpose.InboundDecryption
            or CertificatePurpose.CfaSigningAndDecryption;
        if (requiresPrivate && !version.HasPrivateKey) errors.Add("El propósito requiere llave privada y el certificado no la tiene.");

        var requiresSecretRef = version.PrivateMaterialStorageMode is CertificateStorageMode.ExternalSecretReference or CertificateStorageMode.KeyVaultReference;
        if (requiresPrivate && requiresSecretRef && string.IsNullOrWhiteSpace(version.SecretRef))
            errors.Add("SecretRef requerido para certificado privado en modo de referencia externa.");

        return new CertificateValidationResultDto(errors.Count == 0, errors);
    }
}
