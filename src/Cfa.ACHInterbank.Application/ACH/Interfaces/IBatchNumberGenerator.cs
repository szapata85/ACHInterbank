using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBatchNumberGenerator
{
    Task<BatchNumberAssignmentResult> AssignBatchNumbersAsync(
        IReadOnlyList<AchBatch> orderedBatches,
        string clearingHouseCode,
        DateTime processingDateUtc,
        CancellationToken ct = default);
}

public sealed record BatchNumberAssignmentResult(
    IReadOnlyDictionary<int, int> BatchNumberByBatchId,
    string PolicyCode,
    int ScopedGroups,
    IReadOnlyList<BatchNumberScopeTrace> ScopeTrace);

public sealed record BatchNumberScopeTrace(
    string PolicyCode,
    string Scope,
    int PreviousValue,
    int AssignedValue,
    bool WasCreated,
    int ReservedCount);
