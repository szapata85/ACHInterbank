using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public record TransactionPersistResult
{
    public AchTransaction Transaction { get; init; } = null!;
    public AchBatch Batch { get; init; } = null!;
}
