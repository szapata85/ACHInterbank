using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class DailyResetBatchNumberGenerator : IBatchNumberGenerator
{
    public BatchNumberAssignmentResult AssignBatchNumbers(
        IReadOnlyList<AchBatch> orderedBatches,
        string clearingHouseCode,
        DateTime processingDateUtc)
    {
        var byBatchId = new Dictionary<int, int>(orderedBatches.Count);
        var counters = new Dictionary<(string Chamber, DateOnly Date, string OriginatingDfi), int>();
        var date = DateOnly.FromDateTime(processingDateUtc.Date);

        foreach (var batch in orderedBatches.OrderBy(x => x.Id))
        {
            var key = (
                Chamber: string.IsNullOrWhiteSpace(clearingHouseCode) ? "ACH" : clearingHouseCode.Trim().ToUpperInvariant(),
                Date: date,
                OriginatingDfi: (batch.OriginOrOdfi ?? string.Empty).Trim().ToUpperInvariant());

            var next = counters.TryGetValue(key, out var current) ? current + 1 : 1;
            counters[key] = next;
            byBatchId[batch.Id] = next;
        }

        return new BatchNumberAssignmentResult(
            BatchNumberByBatchId: byBatchId,
            PolicyCode: "DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI",
            ScopedGroups: counters.Count);
    }
}
