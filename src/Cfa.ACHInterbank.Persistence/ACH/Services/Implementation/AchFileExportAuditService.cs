using System.Collections.Concurrent;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
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
        var transactionIds = await context.AchTransactions
            .AsNoTracking()
            .Where(x => x.AchCycleId == cycleId && NachaExportEligibility.ExportableStates.Contains(x.State))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToArrayAsync(ct);
        await RecordGeneratedFileAsync(
            cycleId,
            clearingHouseId,
            exportKind,
            fileName,
            totalRecords,
            totalTransactions,
            isEncrypted,
            transactionIds,
            null,
            ct);
    }

    public async Task RecordGeneratedFileAsync(
        string cycleId,
        int clearingHouseId,
        string exportKind,
        string fileName,
        int totalRecords,
        int totalTransactions,
        bool isEncrypted,
        IReadOnlyCollection<int> achTransactionIds,
        string? contentSha256,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(achTransactionIds);
        var requestedIds = achTransactionIds.Distinct().OrderBy(x => x).ToArray();
        if (requestedIds.Length != achTransactionIds.Count)
        {
            throw new InvalidOperationException("La membresía del archivo contiene transacciones duplicadas.");
        }

        var idempotencyKey = $"{cycleId}|{exportKind}|{isEncrypted}";
        var exportLock = ExportLocks.GetOrAdd(idempotencyKey, static _ => new SemaphoreSlim(1, 1));
        await exportLock.WaitAsync(ct);
        try
        {
            var existingExport = await context.Set<AchFileExport>()
                .AsNoTracking()
                .Include(x => x.Transactions)
                .SingleOrDefaultAsync(x => x.AchCycleId == cycleId
                               && x.ExportKind == exportKind
                               && x.FileName == fileName
                               && x.IsEncrypted == isEncrypted, ct);
            if (existingExport is not null)
            {
                var existingIds = existingExport.Transactions.Select(x => x.AchTransactionId).OrderBy(x => x).ToArray();
                if (!existingIds.SequenceEqual(requestedIds)
                    || (!string.IsNullOrWhiteSpace(contentSha256)
                        && !string.Equals(existingExport.ContentSha256, contentSha256, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException("El mismo nombre de archivo ya fue registrado con una membresía diferente. El caso requiere revisión.");
                }
                return;
            }

            var snapshots = await context.AchTransactions
                .AsNoTracking()
                .Where(x => requestedIds.Contains(x.Id) && x.AchCycleId == cycleId)
                .OrderBy(x => x.AchBatchId)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.AchCycleId,
                    x.AchBatchId,
                    x.TraceNumber,
                    x.Amount
                })
                .ToListAsync(ct);
            if (snapshots.Count != requestedIds.Length)
            {
                throw new InvalidOperationException("No fue posible verificar todas las transacciones incluidas en el archivo dentro del ciclo indicado.");
            }

            var nextVersion = (await context.Set<AchFileExport>()
                .Where(x => x.AchCycleId == cycleId
                    && x.ExportKind == exportKind
                    && x.IsEncrypted == isEncrypted
                    && x.Version.HasValue)
                .MaxAsync(x => (int?)x.Version, ct) ?? 0) + 1;
            var includedAtUtc = DateTime.UtcNow;
            var export = new AchFileExport
            {
                AchCycleId = cycleId,
                ClearingHouseId = clearingHouseId,
                ExportKind = exportKind,
                FileName = fileName,
                TotalRecords = totalRecords,
                TotalTransactions = totalTransactions,
                IsEncrypted = isEncrypted,
                GeneratedAtUtc = includedAtUtc,
                Version = nextVersion,
                LifecycleStatus = isEncrypted
                    ? AchFileExportLifecycleStatus.Protected
                    : AchFileExportLifecycleStatus.Generated,
                ContentSha256 = string.IsNullOrWhiteSpace(contentSha256) ? null : contentSha256.ToUpperInvariant()
            };

            export.Transactions = snapshots.Select((x, index) => new AchFileExportTransaction
            {
                AchTransactionId = x.Id,
                AchCycleId = x.AchCycleId,
                AchBatchId = x.AchBatchId,
                FileSequence = index + 1,
                TraceNumber = x.TraceNumber,
                Amount = x.Amount,
                IncludedAtUtc = includedAtUtc
            }).ToList();

            context.Set<AchFileExport>().Add(export);

            await context.SaveChangesAsync(ct);
        }
        finally
        {
            exportLock.Release();
        }
    }
}
