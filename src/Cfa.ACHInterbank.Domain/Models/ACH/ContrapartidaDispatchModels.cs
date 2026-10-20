using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public enum ContrapartidaDispatchItemStateEnum
{
    PendingContrapartidaReport = 1,
    QueuedForContrapartida = 2,
    ReportingContrapartida = 3,
    ReportedToContrapartida = 4,
    ContrapartidaReportFailed = 5,
    RetryPending = 6,
    Retrying = 7
}

public enum ContrapartidaDispatchBatchStatusEnum
{
    Created = 1,
    Processing = 2,
    Completed = 3,
    CompletedWithErrors = 4,
    Failed = 5,
    Cancelled = 6
}

public enum ContrapartidaDispatchBatchTriggerTypeEnum
{
    Scheduled = 1,
    ManualRetry = 2,
    AutomaticRetry = 3
}

public enum ContrapartidaDispatchAttemptResultEnum
{
    Success = 1,
    Failed = 2,
    Partial = 3
}

public class ContrapartidaDispatchBatch : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AchCycleId { get; set; } = string.Empty;
    public int ClearingHouseId { get; set; }
    public int? AchBatchId { get; set; }

    public ContrapartidaDispatchBatchStatusEnum Status { get; set; } = ContrapartidaDispatchBatchStatusEnum.Created;
    public ContrapartidaDispatchBatchTriggerTypeEnum TriggerType { get; set; } = ContrapartidaDispatchBatchTriggerTypeEnum.Scheduled;

    public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public int TotalItems { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public int TotalPartial { get; set; }

    public string RequestedBy { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public Guid? MappingSetId { get; set; }
    public int? MappingVersion { get; set; }
    public string MappingSnapshotHash { get; set; } = string.Empty;
    public string RequestPayloadXml { get; set; } = string.Empty;
    public string ResponsePayloadXml { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;

    public AchCycle? AchCycle { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }
    public AchBatch? AchBatch { get; set; }
    public ICollection<ContrapartidaDispatchAttempt> Attempts { get; set; } = new List<ContrapartidaDispatchAttempt>();
}

public class ContrapartidaDispatchItem : AuditableEntity
{
    public long Id { get; set; }
    public int AchTransactionId { get; set; }
    public string AchCycleId { get; set; } = string.Empty;
    public int ClearingHouseId { get; set; }
    public int AchBatchId { get; set; }

    public ContrapartidaDispatchItemStateEnum State { get; set; } = ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport;
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? LastSuccessAtUtc { get; set; }
    public int AttemptCount { get; set; }

    public string LastResponseCode { get; set; } = string.Empty;
    public string LastErrorCode { get; set; } = string.Empty;
    public string LastErrorMessage { get; set; } = string.Empty;

    public string LastCorrelationId { get; set; } = string.Empty;
    public string LastDispatchedBy { get; set; } = string.Empty;

    public AchTransaction AchTransaction { get; set; } = null!;
    public AchCycle? AchCycle { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }
    public AchBatch? AchBatch { get; set; }

    public ICollection<ContrapartidaDispatchAttempt> Attempts { get; set; } = new List<ContrapartidaDispatchAttempt>();
}

public class ContrapartidaDispatchAttempt : AuditableEntity
{
    public long Id { get; set; }
    public long DispatchItemId { get; set; }
    public Guid? DispatchBatchId { get; set; }

    public int AttemptNumber { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public ContrapartidaDispatchAttemptResultEnum Result { get; set; } = ContrapartidaDispatchAttemptResultEnum.Failed;

    public string CorrelationId { get; set; } = string.Empty;
    public string TriggeredBy { get; set; } = string.Empty;
    public bool RetryEligible { get; set; }

    public string ExternalResponseCode { get; set; } = string.Empty;
    public string ExternalResponseMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;

    public string RequestPayloadXml { get; set; } = string.Empty;
    public string ResponsePayloadXml { get; set; } = string.Empty;

    public string SoapMethodName { get; set; } = string.Empty;
    public string SoapEndpoint { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string SoapResponseCode { get; set; } = string.Empty;
    public string SoapResponseDescription { get; set; } = string.Empty;
    public string SoapTechnicalStatus { get; set; } = string.Empty;
    public long? ResponseCatalogId { get; set; }
    public IntegrationResponseCode? ResponseCatalog { get; set; }
    public IntegrationTransportStatus TransportStatus { get; set; } = IntegrationTransportStatus.NotExecuted;
    public IntegrationResponseBusinessStatus BusinessStatus { get; set; } = IntegrationResponseBusinessStatus.Unknown;
    public bool RetryAllowed { get; set; }
    public bool RequiresManualReview { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsFunctionalRejection { get; set; }
    public bool IsTechnicalFailure { get; set; }
    public string TechnicalException { get; set; } = string.Empty;

    public ContrapartidaDispatchItem DispatchItem { get; set; } = null!;
    public ContrapartidaDispatchBatch? DispatchBatch { get; set; }
}
