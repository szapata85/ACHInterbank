using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public enum IncomingNachaDispatchQueueStatus
{
    Queued = 1,
    Dispatching = 2,
    Dispatched = 3,
    Confirmed = 4,
    RetryPending = 5,
    FailedFinal = 6,
    Blocked = 7,
    WaitingWindow = 8
}

public enum IncomingNachaDispatchEvent
{
    ManualRetry = 1,
    ManualUnblock = 2,
    ManualRequeue = 3,
    ManualMarkFailedFinal = 4
}

public class IncomingNachaDispatchQueue : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncomingNachaFileIngestionId { get; set; }
    public Guid IncomingNachaEntryClassificationId { get; set; }
    public Guid IncomingNachaTransactionLinkId { get; set; }
    public int AchTransactionId { get; set; }
    public string AchCycleId { get; set; } = string.Empty;
    public int ClearingHouseId { get; set; }
    public DateTime OperationalDate { get; set; }
    public IncomingNachaDispatchQueueStatus QueueStatus { get; set; } = IncomingNachaDispatchQueueStatus.Queued;
    public int Priority { get; set; } = 100;
    public string IdempotencyDispatchKey { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string LastErrorCode { get; set; } = string.Empty;
    public string LastErrorMessage { get; set; } = string.Empty;
    public string LastResponseCode { get; set; } = string.Empty;
    public DateTime? ConfirmedAtUtc { get; set; }

    public IncomingNachaFileIngestion Ingestion { get; set; } = null!;
    public IncomingNachaEntryClassification Classification { get; set; } = null!;
    public IncomingNachaTransactionLink TransactionLink { get; set; } = null!;
    public AchTransaction AchTransaction { get; set; } = null!;
    public ICollection<IncomingNachaIntegrationExecution> Executions { get; set; } = new List<IncomingNachaIntegrationExecution>();
}

public class IncomingNachaIntegrationExecution : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DispatchQueueId { get; set; }
    // Proc_Transacciones is entry-scoped; other audited SOAP operations such as
    // Proc_Contrapartidas are not. Keep the link nullable for those operations.
    public int? EntryDetailId { get; set; }
    public int AttemptNumber { get; set; }
    public int ClearingHouseId { get; set; }
    public string MethodName { get; set; } = "Proc_Transacciones";
    public string SoapMethodName { get; set; } = "Proc_Transacciones";
    public string SoapEndpoint { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = string.Empty;
    public Guid? MappingSetId { get; set; }
    public int? MappingVersion { get; set; }
    public string MappingSnapshotHash { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseHash { get; set; } = string.Empty;
    public string RequestPayloadXml { get; set; } = string.Empty;
    public string ResponsePayloadXml { get; set; } = string.Empty;
    public string SoapResponseCode { get; set; } = string.Empty;
    public string SoapResponseDescription { get; set; } = string.Empty;
    public string SoapTechnicalStatus { get; set; } = string.Empty;
    public long? ResponseCatalogId { get; set; }
    public IntegrationResponseCode? ResponseCatalog { get; set; }
    public int? AchReturnCodeId { get; set; }
    public AchReturnCode? AchReturnCode { get; set; }
    public IncomingNachaIndividualProcessingStatus ProcessingStatus { get; set; } = IncomingNachaIndividualProcessingStatus.Processing;
    public IncomingNachaBusinessOutcome BusinessOutcome { get; set; } = IncomingNachaBusinessOutcome.PendingResponse;
    public string ResultCode { get; set; } = string.Empty;
    public string ResultDescription { get; set; } = string.Empty;
    public string ResultSource { get; set; } = "SOAP";
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string TechnicalErrorCode { get; set; } = string.Empty;
    public string TechnicalErrorMessage { get; set; } = string.Empty;
    public IntegrationTransportStatus TransportStatus { get; set; } = IntegrationTransportStatus.NotExecuted;
    public IntegrationResponseBusinessStatus BusinessStatus { get; set; } = IntegrationResponseBusinessStatus.Unknown;
    public bool RetryAllowed { get; set; }
    public bool RequiresManualReview { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsFunctionalRejection { get; set; }
    public bool IsTechnicalFailure { get; set; }
    public string TechnicalException { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    /// <summary>
    /// Compatibility summary for existing queue/read-model consumers. Raw SOAP code is stored in SoapResponseCode.
    /// </summary>
    public string ResponseCode { get; set; } = string.Empty;
    /// <summary>
    /// Compatibility summary for existing queue/read-model consumers. Raw SOAP message is stored in SoapResponseDescription.
    /// </summary>
    public string ResponseMessage { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsRetryable { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    public IncomingNachaDispatchQueue DispatchQueue { get; set; } = null!;
    public EntryDetail EntryDetail { get; set; } = null!;
}

public enum IncomingNachaIndividualProcessingStatus
{
    Pending = 1,
    Scheduled = 2,
    Processing = 3,
    Completed = 4,
    RetryPending = 5,
    TechnicalFailed = 6
}

public enum IncomingNachaBusinessOutcome
{
    PendingResponse = 1,
    Successful = 2,
    Rejected = 3,
    Returned = 4,
    NotProcessed = 5
}
