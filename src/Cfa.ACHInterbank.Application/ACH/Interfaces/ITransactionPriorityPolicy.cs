using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ITransactionPriorityPolicy
{
    int ResolvePriority(AchTransaction transaction);
}
