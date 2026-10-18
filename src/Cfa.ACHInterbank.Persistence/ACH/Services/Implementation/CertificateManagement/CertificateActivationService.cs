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
public class CertificateActivationService : ICertificateActivationService
{
    private readonly AchDbContext _context;
    private readonly ICertificateValidationService _validator;

    public CertificateActivationService(AchDbContext context, ICertificateValidationService validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<CertificateVersionDto> ActivateVersionAsync(ActivateCertificateVersionRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateForActivationAsync(request.VersionId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(" | ", validation.Errors));
        }

        var version = await _context.DigitalCertificateVersions
            .Include(x => x.DigitalCertificate)
            .FirstAsync(x => x.Id == request.VersionId, cancellationToken);

        var currentActive = await _context.DigitalCertificateVersions
            .Where(x => x.ClearingHouseId == version.ClearingHouseId
                        && x.Environment == version.Environment
                        && x.Purpose == version.Purpose
                        && x.HolderType == version.HolderType
                        && x.Status == CertificateStatus.Active
                        && x.Id != version.Id)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var activeVersion in currentActive)
        {
            activeVersion.Status = CertificateStatus.Replaced;
            activeVersion.ReplacedByVersionId = version.Id;
            _context.CertificateRotationHistories.Add(new CertificateRotationHistory
            {
                PreviousVersionId = activeVersion.Id,
                NewVersionId = version.Id,
                Reason = "Activation replacement",
                RotatedBy = request.ActivatedBy
            });
        }

        version.Status = CertificateStatus.Active;
        version.ActivatedAtUtc = DateTime.UtcNow;

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = version.Id,
            LoadSource = "activation",
            ValidationResult = "SUCCESS",
            LoadedBy = request.ActivatedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        return version.ToDto();
    }

    public async Task<CertificateVersionDto> RevokeVersionAsync(RevokeCertificateVersionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new CertificateValidationException("Ingresa el motivo de la revocación.");
        }
        if (request.Reason.Trim().Length > 500)
        {
            throw new CertificateValidationException("El motivo de la revocación no puede superar 500 caracteres.");
        }

        var version = await _context.DigitalCertificateVersions
            .Include(x => x.DigitalCertificate)
            .FirstAsync(x => x.Id == request.VersionId, cancellationToken);
        version.Status = CertificateStatus.Revoked;
        version.RevokedAtUtc = DateTime.UtcNow;
        version.RevocationReason = request.Reason.Trim();
        version.RevokedBy = request.RevokedBy;

        _context.CertificateLoadAudits.Add(new CertificateLoadAudit
        {
            CertificateVersionId = version.Id,
            Action = "revocation",
            CertificateThumbprint = version.NormalizedThumbprint,
            CertificateDisplayName = version.DigitalCertificate.DisplayName,
            LoadSource = "revocation",
            ValidationResult = "SUCCESS",
            ValidationErrorsJson = System.Text.Json.JsonSerializer.Serialize(new { reason = request.Reason.Trim() }),
            LoadedBy = request.RevokedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        return version.ToDto();
    }
}
