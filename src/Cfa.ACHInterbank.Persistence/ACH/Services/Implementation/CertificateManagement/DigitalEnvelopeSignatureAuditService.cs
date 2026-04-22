using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

[Scoped]
public class DigitalEnvelopeSignatureAuditService : IDigitalEnvelopeSignatureAuditService
{
    private readonly AchDbContext _context;
    private readonly DigitalEnvelopeCertificateOptions _certificateOptions;

    public DigitalEnvelopeSignatureAuditService(
        AchDbContext context,
        IOptions<DigitalEnvelopeCertificateOptions> certificateOptions)
    {
        _context = context;
        _certificateOptions = certificateOptions.Value ?? new DigitalEnvelopeCertificateOptions();
    }

    public async Task AuditAsync(
        string result,
        string? errorCode,
        string? signerThumbprint,
        string? signerSerialNumber,
        string? signatureAlgorithm,
        bool failCloseApplied,
        bool legacyBypassUsed,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var extra = $"thumb={Mask(signerThumbprint)};serial={Mask(signerSerialNumber)};alg={signatureAlgorithm ?? "n/a"};failClose={failCloseApplied};legacyBypass={legacyBypassUsed}";
        _context.DigitalEnvelopeOperationLogs.Add(new DigitalEnvelopeOperationLog
        {
            Direction = "InboundSignatureValidation",
            ClearingHouseId = _certificateOptions.DefaultClearingHouseId,
            Environment = ParseEnvironment(_certificateOptions.Environment),
            Purpose = CertificatePurpose.InboundSignatureValidation,
            Result = result,
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : $"{errorCode}|{extra}",
            Actor = actor,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static CertificateEnvironment ParseEnvironment(string? environment)
    {
        return Enum.TryParse<CertificateEnvironment>(environment, true, out var parsed)
            ? parsed
            : CertificateEnvironment.Test;
    }

    private static string Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "n/a";
        }

        return value.Length <= 6 ? "****" : $"****{value[^6..]}";
    }
}
