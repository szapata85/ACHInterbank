using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class IncomingNachaHistoricalCorrectionService
{
    private const string TargetClearingHouseCode = "CENIT";
    private static readonly DateTime TargetProcessingDate = new(2026, 7, 13, 0, 0, 0, DateTimeKind.Utc);
    private const int TargetCycleNumber = 2;
    private const string CycleCorrectionEventType = "CycleResolutionCorrected";
    private const string MappingCorrectionEventType = "ProcTransaccionesMappingCorrection";
    private const string CorrectedBy = "cycle-resolution-remediation";
    private const string MappingCorrectedBy = "mapping-remediation";
    private const string LocalBatchCompanyName = "LOCAL LIVE CENIT";
    private const string LocalTransactionMarkerPrefix = "local-live-";
    private const string MappingParameter = "NCTAORIG";

    private readonly AchDbContext _context;

    public IncomingNachaHistoricalCorrectionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task ApplyAsync(Guid ingestionId, CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);

            var ingestion = await _context.IncomingNachaFileIngestions
                .FirstOrDefaultAsync(x => x.Id == ingestionId, ct)
                ?? throw new InvalidOperationException($"No existe la ingesta {ingestionId}.");

            var correctedCycle = await ResolveTargetCycleAsync(ct);
            var previousCycleId = ingestion.ResolvedAchCycleId;
            var previousCycleNumber = await ResolveCycleNumberAsync(previousCycleId, ct);

            var relatedQueueRows = await _context.IncomingNachaDispatchQueue
                .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
                .ToListAsync(ct);

            var relatedLinks = await _context.IncomingNachaTransactionLinks
                .Include(x => x.EntryDetail)
                .Where(x => x.IncomingNachaFileIngestionId == ingestionId)
                .ToListAsync(ct);

            var relatedTransactionIds = relatedLinks
                .Where(x => x.AchTransactionId > 0)
                .Select(x => x.AchTransactionId)
                .Distinct()
                .ToList();

            var relatedTransactions = await _context.AchTransactions
                .Include(x => x.AchBatch)
                .Where(x => relatedTransactionIds.Contains(x.Id))
                .ToListAsync(ct);

            var relatedBatchIds = relatedTransactions
                .Where(x => x.AchBatchId > 0)
                .Select(x => x.AchBatchId)
                .Distinct()
                .ToList();

            var relatedBatches = await _context.AchBatches
                .Where(x => relatedBatchIds.Contains(x.Id))
                .ToListAsync(ct);

            var cycleChanged = !string.Equals(ingestion.ResolvedAchCycleId, correctedCycle.Id, StringComparison.Ordinal);
            ingestion.ResolvedAchCycleId = correctedCycle.Id;

            foreach (var queue in relatedQueueRows)
            {
                if (!string.Equals(queue.AchCycleId, correctedCycle.Id, StringComparison.Ordinal))
                {
                    queue.AchCycleId = correctedCycle.Id;
                }
            }

            foreach (var transactionRow in relatedTransactions)
            {
                if (!string.Equals(transactionRow.AchCycleId, correctedCycle.Id, StringComparison.Ordinal))
                {
                    transactionRow.AchCycleId = correctedCycle.Id;
                }
            }

            foreach (var batch in relatedBatches)
            {
                if (!IsLocalLiveBatch(batch))
                {
                    continue;
                }

                if (!string.Equals(batch.AchCycleId, correctedCycle.Id, StringComparison.Ordinal))
                {
                    batch.AchCycleId = correctedCycle.Id;
                }
            }

            if (cycleChanged && !await HasEventAsync(ingestionId, CycleCorrectionEventType, ct))
            {
                _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
                {
                    IncomingNachaFileIngestionId = ingestionId,
                    EventType = CycleCorrectionEventType,
                    EventStatus = "Completed",
                    Message = "Se corrigió el ciclo de la ingesta NACHA.",
                    EvidenceJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        EventType = CycleCorrectionEventType,
                        EventStatus = "Completed",
                        PreviousCycleNumber = previousCycleNumber,
                        CorrectedCycleNumber = TargetCycleNumber,
                        FileName = ingestion.FileName,
                        Reason = "CENIT_CYCLE_IS_SECOND_FILENAME_SEGMENT",
                        CorrectedBy
                    }),
                    OccurredAtUtc = DateTime.UtcNow,
                    RaisedBy = CorrectedBy
                });
            }

            if (!await HasEventAsync(ingestionId, MappingCorrectionEventType, ct))
            {
                _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
                {
                    IncomingNachaFileIngestionId = ingestionId,
                    EventType = MappingCorrectionEventType,
                    EventStatus = "Completed",
                    Message = "Se corrigió el origen canónico de NCTAORIG.",
                    EvidenceJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        EventType = MappingCorrectionEventType,
                        EventStatus = "Completed",
                        Parameter = MappingParameter,
                        PreviousSource = "AchTransaction.SourceAccountNumber",
                        CorrectedSource = "EntryDetails.AccountNumber",
                        AffectedHistoricalIngestionId = ingestionId,
                        HistoricalEvidenceModified = false,
                        CorrectedBy = MappingCorrectedBy
                    }),
                    OccurredAtUtc = DateTime.UtcNow,
                    RaisedBy = MappingCorrectedBy
                });
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
    }

    private async Task<AchCycle> ResolveTargetCycleAsync(CancellationToken ct)
    {
        var candidates = await _context.AchCycles
            .Include(x => x.ClearingHouse)
            .Where(x => x.ProcessingDate.Date == TargetProcessingDate.Date
                && x.ClearingHouse != null
                && x.ClearingHouse.Code == TargetClearingHouseCode)
            .ToListAsync(ct);

        var matches = candidates
            .Where(x => TryExtractCycleNumber(x) == TargetCycleNumber)
            .ToList();

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException("No existe el ciclo CENIT objetivo para la remediación histórica."),
            _ => throw new InvalidOperationException("El ciclo CENIT objetivo es ambiguo para la remediación histórica.")
        };
    }

    private static int? TryExtractCycleNumber(AchCycle cycle)
        => ExternalFileNameSupport.TryExtractPositiveCycleNumber(cycle.CycleName, out var number) ? number : null;

    private async Task<int?> ResolveCycleNumberAsync(string? cycleId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cycleId))
        {
            return null;
        }

        var cycle = await _context.AchCycles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cycleId, ct);
        return cycle is null
            ? null
            : TryExtractCycleNumber(cycle);
    }

    private async Task<bool> HasEventAsync(Guid ingestionId, string eventType, CancellationToken ct)
        => await _context.IncomingNachaProcessingEvents.AsNoTracking().AnyAsync(x =>
            x.IncomingNachaFileIngestionId == ingestionId
            && x.EventType == eventType
            && x.EventStatus == "Completed", ct);

    private static bool IsLocalLiveBatch(AchBatch batch)
        => string.Equals(batch.CompanyName, LocalBatchCompanyName, StringComparison.OrdinalIgnoreCase)
           && !string.IsNullOrWhiteSpace(batch.CompanyIdentification)
           && batch.CompanyIdentification.StartsWith("L", StringComparison.OrdinalIgnoreCase)
           && !string.IsNullOrWhiteSpace(batch.OriginOrOdfi);
}
