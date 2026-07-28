using System.Collections.Concurrent;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchFileExportAuditService(AchDbContext context) : IAchFileExportAuditService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ExportLocks = new(StringComparer.Ordinal);

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
        var idempotencyKey = $"{cycleId}|{exportKind}|{isEncrypted}|{fileName}";
        var exportLock = ExportLocks.GetOrAdd(idempotencyKey, static _ => new SemaphoreSlim(1, 1));
        await exportLock.WaitAsync(ct);
        try
        {
            var alreadyRecorded = await context.Set<AchFileExport>()
                .AsNoTracking()
                .AnyAsync(x => x.AchCycleId == cycleId
                               && x.ExportKind == exportKind
                               && x.FileName == fileName
                               && x.IsEncrypted == isEncrypted, ct);
            if (alreadyRecorded)
            {
                return;
            }

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
        finally
        {
            exportLock.Release();
        }
    }
}
