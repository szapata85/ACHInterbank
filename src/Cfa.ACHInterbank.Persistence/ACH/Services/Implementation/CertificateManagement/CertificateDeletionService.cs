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
public class CertificateDeletionService : ICertificateDeletionService
{
    private readonly AchDbContext _context;

    public CertificateDeletionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteCertificateVersionResultDto> DeleteVersionAsync(
        DeleteCertificateVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        var version = await _context.DigitalCertificateVersions
            .Include(x => x.DigitalCertificate)
            .SingleOrDefaultAsync(x => x.Id == request.VersionId, cancellationToken)
            ?? throw new CertificateValidationException("No se encontró el certificado solicitado.");

        var hasOperationalReferences =
            await _context.CertificateUsageLogs.AnyAsync(x => x.CertificateVersionId == version.Id, cancellationToken)
            || await _context.DigitalEnvelopeOperationLogs.AnyAsync(x => x.CertificateVersionId == version.Id, cancellationToken)
            || await _context.CertificateRotationHistories.AnyAsync(
                x => x.PreviousVersionId == version.Id || x.NewVersionId == version.Id,
                cancellationToken)
            || await _context.DigitalCertificateVersions.AnyAsync(
                x => x.ReplacedByVersionId == version.Id,
                cancellationToken);

        if (hasOperationalReferences)
        {
            throw new CertificateConflictException("Este certificado no puede eliminarse porque ya fue utilizado. Debe conservarse para mantener la trazabilidad.");
        }

        var now = DateTime.UtcNow;
        var isOperational = version.Status == CertificateStatus.Active
                            && version.NotBefore <= now
                            && version.NotAfter > now;
        if (isOperational)
        {
            throw new CertificateConflictException("Revoca el certificado antes de eliminarlo de forma segura.");
        }

        var loadAudits = await _context.CertificateLoadAudits
            .Where(x => x.CertificateVersionId == version.Id)
            .ToListAsync(cancellationToken);
        foreach (var audit in loadAudits)
        {
            audit.CertificateThumbprint ??= version.NormalizedThumbprint;
            audit.CertificateDisplayName ??= version.DigitalCertificate.DisplayName;
            audit.CertificateVersionId = null;
        }

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = null,
            Action = "deletion",
            CertificateThumbprint = version.NormalizedThumbprint,
            CertificateDisplayName = version.DigitalCertificate.DisplayName,
            LoadSource = "administration",
            ValidationResult = "SUCCESS",
            LoadedBy = request.DeletedBy
        });

        var aggregate = version.DigitalCertificate;
        _context.DigitalCertificateVersions.Remove(version);
        await _context.SaveChangesAsync(cancellationToken);

        if (!await _context.DigitalCertificateVersions.AnyAsync(
                x => x.DigitalCertificateId == aggregate.Id,
                cancellationToken))
        {
            _context.DigitalCertificates.Remove(aggregate);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (version.EncryptedPrivateMaterial is { Length: > 0 })
        {
            CryptographicOperations.ZeroMemory(version.EncryptedPrivateMaterial);
        }

        return new DeleteCertificateVersionResultDto(request.VersionId, true);
    }
}
