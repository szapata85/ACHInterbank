using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchIncomingReturnIngestionService(AchDbContext context) : IAchIncomingReturnIngestionService
{
    public async Task<AchIncomingReturnIngestionResult> IngestAsync(AchIncomingReturnIngestionRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<AchIncomingReturnIngestionFailure>();
        var items = new List<AchIncomingReturnItem>();

        if (string.IsNullOrWhiteSpace(request.RawContent))
        {
            failures.Add(new("FILE_EMPTY", "El archivo entrante está vacío.", nameof(request.RawContent)));
            return new(false, 0, 0, 0, 0, items, failures);
        }

        var records = ChunkRecords(request.RawContent);
        foreach (var record in records.Where(r => r.Length >= 30 && r.StartsWith("7") && r.Substring(1, 2) == "99"))
        {
            var reason = record.Substring(3, 5).Trim();
            var originalTrace = record.Substring(8, 15).Trim();
            var trace = record.Length >= 106 ? record.Substring(91, 15).Trim() : null;

            if (string.IsNullOrWhiteSpace(reason))
            {
                failures.Add(new("RETURN_REASON_MISSING", "No se encontró causal de devolución.", nameof(reason), trace));
            }

            if (string.IsNullOrWhiteSpace(originalTrace))
            {
                failures.Add(new("ORIGINAL_TRACE_MISSING", "No se encontró traza original para vincular la devolución.", nameof(originalTrace), trace));
                items.Add(new(trace, null, reason, null, null, null, null, false, record));
                continue;
            }

            var originalTx = await context.AchTransactions
                .AsNoTracking()
                .Include(t => t.AchCycle)
                .FirstOrDefaultAsync(t => t.TraceNumber == originalTrace || t.OriginalTraceRef == originalTrace, cancellationToken);

            if (originalTx is null)
            {
                failures.Add(new("ORIGINAL_TRANSACTION_NOT_FOUND", "No se encontró la transacción original de la devolución.", nameof(originalTrace), trace));
                items.Add(new(trace, originalTrace, reason, null, null, null, null, false, record));
                continue;
            }

            var clearingHouseId = originalTx.AchCycle?.ClearingHouseId;
            if (!clearingHouseId.HasValue || clearingHouseId.Value <= 0)
            {
                failures.Add(new("CLEARING_HOUSE_MISSING", "No se pudo resolver la cámara de la transacción original.", "ClearingHouseId", trace));
            }

            items.Add(new(trace, originalTrace, reason, originalTx.Id, clearingHouseId, originalTx.Type.ToString(), originalTx.State.ToString(), true, record));
        }

        var parsed = items.Count;
        var linked = items.Count(x => x.IsLinked);
        var unlinked = parsed - linked;
        return new(failures.Count == 0, records.Count, parsed, linked, unlinked, items, failures);
    }

    private static List<string> ChunkRecords(string rawContent)
    {
        var clean = rawContent.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var records = new List<string>();
        for (int i = 0; i + 106 <= clean.Length; i += 106)
        {
            records.Add(clean.Substring(i, 106));
        }
        return records;
    }
}
