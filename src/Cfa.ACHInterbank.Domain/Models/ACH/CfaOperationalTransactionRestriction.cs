using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CfaOperationalTransactionRestriction : AuditableEntity
{
    public int Id { get; set; }

    public int? ClearingHouseId { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }

    public int? ClearingHouseCycleConfigId { get; set; }
    public ClearingHouseCycleConfig? ClearingHouseCycleConfig { get; set; }

    public TransactionTypeEnum? TransactionType { get; set; }
    public bool? IsPrenotification { get; set; }

    public decimal? MaxAmountPerTransaction { get; set; }
    public decimal? MaxAmountPerCycle { get; set; }
    public int? MaxTransactionsPerCycle { get; set; }

    // null = inherit/no override; empty = explicit deny-all for account types.
    public List<AccountTypeEnum>? AllowedAccountTypes { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public byte[] RowVersion { get; set; } = [];

    public bool IsEffectiveAt(DateTime instant)
        => IsActive
           && EffectiveFrom <= instant
           && (!EffectiveTo.HasValue || EffectiveTo.Value >= instant);
}
