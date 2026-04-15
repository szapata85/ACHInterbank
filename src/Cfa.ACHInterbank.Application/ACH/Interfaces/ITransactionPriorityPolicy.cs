using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionPriorityPolicy
{
    Task<int> ResolvePriorityAsync(AchTransaction transaction, CancellationToken ct);
}
