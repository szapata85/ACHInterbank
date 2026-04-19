using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBatchNumberGenerator
{
    BatchNumberAssignmentResult AssignBatchNumbers(
        IReadOnlyList<AchBatch> orderedBatches,
        string clearingHouseCode,
        DateTime processingDateUtc);
}

public sealed record BatchNumberAssignmentResult(
    IReadOnlyDictionary<int, int> BatchNumberByBatchId,
    string PolicyCode,
    int ScopedGroups);
