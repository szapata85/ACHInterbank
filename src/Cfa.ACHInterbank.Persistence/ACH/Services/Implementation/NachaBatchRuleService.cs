using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaBatchRuleService : INachaBatchRuleService
{
    private const string MultiCreditDescription = "MULTICREDIT";

    public string ResolveBatchDescription(AchBatch batch, IReadOnlyCollection<AchTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(transactions);

        var currentDescription = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();

        if (ShouldUseMulticredit(transactions, currentDescription))
        {
            return MultiCreditDescription;
        }

        return currentDescription;
    }

    public DateTime ResolveDescriptiveDate(AchBatch batch, IReadOnlyCollection<AchTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(transactions);

        if (!RequiresDescriptiveDate(transactions))
        {
            return batch.EffectiveEntryDate;
        }

        if (batch.EffectiveEntryDate == default)
        {
            throw new InvalidOperationException("El registro tipo 5 requiere fecha descriptiva para prenotificaciones crédito y monetarias crédito.");
        }

        return batch.EffectiveEntryDate;
    }

    public bool RequiresDescriptiveDate(IReadOnlyCollection<AchTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        return transactions.Any(tx => tx.Type == TransactionTypeEnum.Credit);
    }

    private static bool ShouldUseMulticredit(IReadOnlyCollection<AchTransaction> transactions, string currentDescription)
    {
        if (transactions.Count == 0)
        {
            return false;
        }

        var hasCredit = transactions.Any(tx => tx.Type == TransactionTypeEnum.Credit);
        if (!hasCredit)
        {
            return false;
        }

        return currentDescription.Contains("PSE", StringComparison.OrdinalIgnoreCase)
               || transactions.Any(tx =>
                   (tx.Reference?.Contains("PSE", StringComparison.OrdinalIgnoreCase) ?? false)
                   || string.Equals(tx.SourceInstitution?.Name, "PSE", StringComparison.OrdinalIgnoreCase));
    }
}
