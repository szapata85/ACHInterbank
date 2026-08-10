using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchFileTransportResult : AuditableEntity
{
    public Guid Id { get; set; }
    public int? AchFileExportId { get; set; }
    public AchFileExport? AchFileExport { get; set; }
    public string ExternalEventId { get; set; } = string.Empty;
    public string FunctionalIdentityHash { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string TransmissionReference { get; set; } = string.Empty;
    public AchOutboundReturnOutcome Outcome { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public AchResponseCorrelationStatus CorrelationStatus { get; set; }
    public bool Applied { get; set; }
    public bool RequiresManualReview { get; set; }
}
