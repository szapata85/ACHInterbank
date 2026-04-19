using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class DailyResetBatchNumberGenerator(IBatchNumberSequenceStore store) : IBatchNumberGenerator
{
    private const string DailyResetPolicyCode = "DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI";

    public async Task<BatchNumberAssignmentResult> AssignBatchNumbersAsync(
        IReadOnlyList<AchBatch> orderedBatches,
        string clearingHouseCode,
        DateTime processingDateUtc,
        CancellationToken ct = default)
    {
        var byBatchId = new Dictionary<int, int>(orderedBatches.Count);
        var scopeTrace = new List<BatchNumberScopeTrace>();

        var date = DateOnly.FromDateTime(processingDateUtc.Date);
        var normalizedChamber = string.IsNullOrWhiteSpace(clearingHouseCode)
            ? "ACH"
            : clearingHouseCode.Trim().ToUpperInvariant();

        var groups = orderedBatches
            .OrderBy(x => x.Id)
            .GroupBy(x => (x.OriginOrOdfi ?? string.Empty).Trim().ToUpperInvariant())
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var group in groups)
        {
            var scope = new BatchNumberSequenceScope(
                PolicyCode: DailyResetPolicyCode,
                ClearingHouseId: normalizedChamber,
                OriginatingDfi: group.Key,
                ProcessingDate: date);

            var reservation = await store.ReserveRangeAsync(scope, group.Count(), ct);
            var current = reservation.StartValue;

            foreach (var batch in group.OrderBy(x => x.Id))
            {
                byBatchId[batch.Id] = current;
                current++;
            }

            scopeTrace.Add(new BatchNumberScopeTrace(
                PolicyCode: DailyResetPolicyCode,
                Scope: scope.ToScopeKey(),
                PreviousValue: reservation.PreviousValue,
                AssignedValue: reservation.EndValue,
                WasCreated: reservation.WasCreated,
                ReservedCount: reservation.ReservedCount));
        }

        return new BatchNumberAssignmentResult(
            BatchNumberByBatchId: byBatchId,
            PolicyCode: DailyResetPolicyCode,
            ScopedGroups: groups.Count,
            ScopeTrace: scopeTrace);
    }
}
