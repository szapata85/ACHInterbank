using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaType7RecordCandidate
{
    public required AchBatch Batch { get; init; }
    public required AchTransaction Transaction { get; init; }
    public required AchTransactionAddenda Addenda { get; init; }
    public required IReadOnlyDictionary<string, object?> FieldValues { get; init; }
}
