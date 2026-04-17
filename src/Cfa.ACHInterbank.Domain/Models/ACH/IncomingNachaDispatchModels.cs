using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

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
    public string MethodName { get; set; } = "Proc_Transacciones";
    public Guid? MappingSetId { get; set; }
    public int? MappingVersion { get; set; }
    public string MappingSnapshotHash { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseHash { get; set; } = string.Empty;
    public string RequestPayloadXml { get; set; } = string.Empty;
    public string ResponsePayloadXml { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string ResponseMessage { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsRetryable { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    public IncomingNachaDispatchQueue DispatchQueue { get; set; } = null!;
}
