using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

internal static class CertificateManagementMapper
{
    public static CertificateVersionDto ToDto(this DigitalCertificateVersion v, int warningDays = 30, bool canDelete = false)
        => new(
            v.Id,
            v.DigitalCertificate.Code,
            v.DigitalCertificate.DisplayName,
            v.ClearingHouseId,
            v.Environment,
            v.Purpose,
            v.HolderType,
            v.Status,
            v.VersionNumber,
            v.Subject,
            v.Issuer,
            v.SerialNumber,
            v.Thumbprint,
            v.FingerprintSha256,
            v.NotBefore,
            v.NotAfter,
            v.HasPrivateKey,
            v.KeyAlgorithm,
            v.KeySize,
            v.SignatureAlgorithm,
            v.SecretRef,
            v.UploadedAtUtc,
            v.UploadedBy,
            v.ActivatedAtUtc,
            v.RevokedAtUtc,
            v.FileName,
            v.FinancialInstitutionId,
            v.FinancialInstitution?.Name,
            v.ClearingHouse?.Name,
            CertificateFunctionalStatusEvaluator.Evaluate(v, DateTime.UtcNow, warningDays),
            CertificateFunctionalStatusEvaluator.DaysRemaining(v.NotAfter, DateTime.UtcNow),
            v.RevocationReason,
            v.RevokedBy,
            canDelete);
}

internal static class CertificateFunctionalStatusEvaluator
{
    public static CertificateFunctionalStatus Evaluate(
        DigitalCertificateVersion version,
        DateTime utcNow,
        int warningDays)
    {
        if (version.Status == CertificateStatus.Revoked) return CertificateFunctionalStatus.Revoked;
        if (version.Status == CertificateStatus.Replaced) return CertificateFunctionalStatus.Replaced;
        if (utcNow < version.NotBefore) return CertificateFunctionalStatus.PendingValidity;
        if (utcNow > version.NotAfter) return CertificateFunctionalStatus.Expired;
        if (version.Status != CertificateStatus.Active) return CertificateFunctionalStatus.Inactive;
        return version.NotAfter <= utcNow.AddDays(Math.Max(0, warningDays))
            ? CertificateFunctionalStatus.ExpiringSoon
            : CertificateFunctionalStatus.Valid;
    }

    public static int? DaysRemaining(DateTime notAfter, DateTime utcNow)
    {
        if (notAfter < utcNow) return 0;
        return Math.Max(0, (int)Math.Ceiling((notAfter - utcNow).TotalDays));
    }
}
