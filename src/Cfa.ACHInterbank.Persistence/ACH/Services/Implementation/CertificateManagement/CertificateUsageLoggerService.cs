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
public class CertificateUsageLoggerService : ICertificateUsageLogger
{
    private readonly AchDbContext _context;

    public CertificateUsageLoggerService(AchDbContext context)
    {
        _context = context;
    }

    public async Task LogUsageAsync(int versionId, string operationType, string operationId, string result, string? errorCode, string actor, string? contextJson = null, CancellationToken cancellationToken = default)
    {
        _context.CertificateUsageLogs.Add(new CertificateUsageLog
        {
            CertificateVersionId = versionId,
            OperationType = operationType,
            OperationId = operationId,
            ContextJson = contextJson,
            Result = result,
            ErrorCode = errorCode,
            CreatedByProcess = actor,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
