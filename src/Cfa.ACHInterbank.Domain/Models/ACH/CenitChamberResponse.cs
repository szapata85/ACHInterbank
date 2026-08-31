using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public sealed class CenitChamberResponse : AuditableEntity
{
    public Guid Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public int? AchFileExportId { get; set; }
    public AchFileExport? AchFileExport { get; set; }
    public int? AchTransactionId { get; set; }
    public AchTransaction? AchTransaction { get; set; }
    public string SourceResponseId { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public CenitChamberResponseType ResponseType { get; set; }
    public CenitChamberResponseState ResultingState { get; set; }
    public string? ReasonCode { get; set; }
    public string? Description { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public CenitChamberCorrelationOutcome CorrelationOutcome { get; set; }
    public string RawTechnicalReference { get; set; } = string.Empty;
    public string ContentSha256 { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? RelatedOutboundFileName { get; set; }
    public string? RelatedReference { get; set; }
    public string? TransactionTraceNumber { get; set; }
    public string? ProblemCode { get; set; }
    public bool IsApplied { get; set; }
}
