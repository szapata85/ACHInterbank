using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchFileExportAuditService(AchDbContext context) : IAchFileExportAuditService
{
    public async Task RecordGeneratedFileAsync(
        string cycleId,
        int clearingHouseId,
        string exportKind,
        string fileName,
        int totalRecords,
        int totalTransactions,
        bool isEncrypted,
        CancellationToken ct = default)
    {
        context.Set<AchFileExport>().Add(new AchFileExport
        {
            AchCycleId = cycleId,
            ClearingHouseId = clearingHouseId,
            ExportKind = exportKind,
            FileName = fileName,
            TotalRecords = totalRecords,
            TotalTransactions = totalTransactions,
            IsEncrypted = isEncrypted,
            GeneratedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync(ct);
    }
}
