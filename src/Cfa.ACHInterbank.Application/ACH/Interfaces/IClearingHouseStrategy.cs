using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseStrategy
{
    string Name { get; }
    string OutputFormat { get; }
    IReadOnlyCollection<TransactionTypeEnum> SupportedTransactionTypes { get; }
    bool ValidateTransaction(AchTransaction transaction);
    string BuildFileName(AchCycle cycle, DateTime generatedAtUtc);
    byte[] GenerateCycleFile(AchCycle cycle);
}
