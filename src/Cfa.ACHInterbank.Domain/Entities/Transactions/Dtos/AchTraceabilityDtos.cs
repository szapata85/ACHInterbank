using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class AchTraceabilityEventDto
{
    public long Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AchTransferStateEnum FromState { get; set; }
    public AchTransferStateEnum ToState { get; set; }
    public AchStateEventSourceEnum Source { get; set; }
    public string? ReasonCode { get; set; }
    public string? PayloadJson { get; set; }
}

public class AchTraceabilityDetailDto
{
    public int TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string TraceNumber { get; set; } = string.Empty;
    public string OriginalTraceRef { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime EffectiveEntryDate { get; set; }
    public string AchCycleId { get; set; } = string.Empty;
    public string AchCycleName { get; set; } = string.Empty;
    public string ClearingHouseName { get; set; } = string.Empty;
    public string ClearingHouseCode { get; set; } = string.Empty;
    public string CurrentNachaFileName { get; set; } = string.Empty;
    public DateTime? CurrentNachaGeneratedAtUtc { get; set; }
    public string ReturnFileName { get; set; } = string.Empty;
    public string ReturnCycleId { get; set; } = string.Empty;
    public int? ReturnOriginalTransactionId { get; set; }
    public DateTime? ReturnGeneratedAtUtc { get; set; }
    public string SourceInstitutionName { get; set; } = string.Empty;
    public string DestinationInstitutionName { get; set; } = string.Empty;

    public AchTransferStateEnum State { get; set; }
    public DateTime StateChangedAtUtc { get; set; }
    public DateTime? SlaDeadlineAtUtc { get; set; }
    public string ReturnReasonCode { get; set; } = string.Empty;

    public List<AchTraceabilityEventDto> Events { get; set; } = [];
}

public class AchTraceabilityReportRowDto
{
    public int TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string TraceNumber { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AchCycleId { get; set; } = string.Empty;
    public string AchCycleName { get; set; } = string.Empty;
    public string ClearingHouseName { get; set; } = string.Empty;
    public string ClearingHouseCode { get; set; } = string.Empty;
    public string CurrentNachaFileName { get; set; } = string.Empty;
    public DateTime EffectiveEntryDate { get; set; }
    public AchTransferStateEnum State { get; set; }
    public DateTime StateChangedAtUtc { get; set; }
    public string ReturnReasonCode { get; set; } = string.Empty;
    public int EventsCount { get; set; }
    public string SourceInstitutionName { get; set; } = string.Empty;
    public string DestinationInstitutionName { get; set; } = string.Empty;
}
