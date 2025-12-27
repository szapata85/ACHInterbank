using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class AchTransactionListDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public TransactionTypeEnum Type { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public DateTime EffectiveEntryDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string SourceInstitutionName { get; set; } = string.Empty;
    public string DestinationInstitutionName { get; set; } = string.Empty;

    public int AchBatchId { get; set; }
    public int BatchSequenceNumber { get; set; }
    public string BatchCompanyName { get; set; } = string.Empty;
    public DateTime BatchEffectiveEntryDate { get; set; }

    public string AchCycleId { get; set; } = string.Empty;
    public string AchCycleName { get; set; } = string.Empty;
    public string ClearingHouseName { get; set; } = string.Empty;
}
