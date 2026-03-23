using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public class TransactionPolicyOptions
{
    public TransactionLimitRule Defaults { get; set; } = new();
    public List<TransactionLimitRule> Limits { get; set; } = [];
}

public class TransactionLimitRule
{
    public int? ClearingHouseId { get; set; }
    public string? CycleName { get; set; }
    public TransactionTypeEnum? TransactionType { get; set; }
    public bool? IsPrenotification { get; set; }
    public List<AccountTypeEnum> AllowedAccountTypes { get; set; } = [];
    public decimal? MaxAmountPerTransaction { get; set; }
    public decimal? MaxAmountPerCycle { get; set; }
    public int? MaxTransactionsPerCycle { get; set; }
}
