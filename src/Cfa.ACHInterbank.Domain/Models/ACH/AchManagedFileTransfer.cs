using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public enum AchManagedFileDirection { Outbound = 1, Inbound = 2 }
public enum AchManagedFileExecutionOrigin { Automatic = 1, Manual = 2 }
public enum AchManagedFileTransferStatus
{
    Ready = 1,
    InProgress = 2,
    Transferred = 3,
    Received = 4,
    Processed = 5,
    Rejected = 6,
    Duplicate = 7,
    RetryPending = 8,
    Uncertain = 9,
    Failed = 10,
    Retired = 11
}

public sealed class AchManagedFileTransfer : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public AchManagedFileDirection Direction { get; set; }
    public string LogicalFileIdentity { get; set; } = string.Empty;
    public string PhysicalFileName { get; set; } = string.Empty;
    public int? AchFileExportId { get; set; }
    public AchFileExport? AchFileExport { get; set; }
    public Guid? IncomingNachaFileIngestionId { get; set; }
    public IncomingNachaFileIngestion? IncomingNachaFileIngestion { get; set; }
    public string ContentSha256 { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public byte[]? RetainedContent { get; set; }
    public DateTime OperationalDate { get; set; }
    public string? AchCycleId { get; set; }
    public AchCycle? AchCycle { get; set; }
    public AchManagedFileExecutionOrigin ExecutionOrigin { get; set; }
    public AchManagedFileTransferStatus Status { get; set; } = AchManagedFileTransferStatus.Ready;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? TransferredAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastError { get; set; }
    public string? ActiveStorageReference { get; set; }
    public string? ArchiveReference { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? CorrectedFromTransferId { get; set; }
    public AchManagedFileTransfer? CorrectedFromTransfer { get; set; }
    public string? OperatorIdentity { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
    public string? RetiredBy { get; set; }
    public string? RetirementReason { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public ICollection<AchManagedFileTransferEvent> Events { get; set; } = new List<AchManagedFileTransferEvent>();
}

public sealed class AchManagedFileTransferEvent : AuditableEntity
{
    public long Id { get; set; }
    public Guid TransferId { get; set; }
    public AchManagedFileTransfer Transfer { get; set; } = null!;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AchManagedFileExecutionOrigin ExecutionOrigin { get; set; }
    public string Actor { get; set; } = string.Empty;
}

public sealed class AchManagedFileTransferConfiguration : AuditableEntity
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public bool AutomaticOutboundEnabled { get; set; }
    public bool AutomaticInboundEnabled { get; set; }
    public bool ManualOutboundAllowed { get; set; } = true;
    public bool ManualInboundAllowed { get; set; } = true;
    public int MaximumRetries { get; set; } = 3;
    public int RetentionDays { get; set; } = 90;
    public string OutboundLocation { get; set; } = "outbound";
    public string InboundLocation { get; set; } = "inbound";
    public string ArchiveLocation { get; set; } = "archive";
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
