using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchManagedMftArtifact(string FileName, byte[] Content, string ContentSha256, string ClaimReference);
public sealed record AchManagedMftResult(bool Succeeded, bool Retryable, bool Uncertain, string Code, string Message, string? StorageReference);

public sealed record AchManagedFileTransferSummary(
    Guid Id, string FileName, AchManagedFileDirection Direction, DateTime OperationalDate, string? CycleId,
    AchManagedFileTransferStatus Status, AchManagedFileExecutionOrigin ExecutionOrigin, int AttemptCount,
    DateTime UpdatedAtUtc, bool Archived, bool Retired);

public sealed record AchManagedFileTransferEventDto(
    long Id, DateTime OccurredAtUtc, string EventType, string Result, string Message,
    AchManagedFileExecutionOrigin ExecutionOrigin, string Actor);

public sealed record AchManagedFileTransferDetail(
    Guid Id, string FileName, AchManagedFileDirection Direction, DateTime OperationalDate, string? CycleId,
    AchManagedFileTransferStatus Status, AchManagedFileExecutionOrigin ExecutionOrigin, long FileSize,
    string ContentSha256, int AttemptCount, DateTime CreatedAtUtc, DateTime? TransferredAtUtc,
    DateTime? ProcessedAtUtc, string? LastError, bool Archived, DateTime? ArchivedAtUtc,
    bool Retired, DateTime? RetiredAtUtc, string? RetirementReason, Guid? CorrectedFromTransferId,
    IReadOnlyList<AchManagedFileTransferEventDto> History);

public sealed record AchManagedFileTransferQuery(
    DateTime? From = null, DateTime? To = null, AchManagedFileDirection? Direction = null,
    AchManagedFileTransferStatus? Status = null, string? CycleId = null,
    AchManagedFileExecutionOrigin? ExecutionOrigin = null);

public sealed record AchManagedFileTransferConfigurationDto(
    bool AutomaticOutboundEnabled, bool AutomaticInboundEnabled, bool ManualOutboundAllowed,
    bool ManualInboundAllowed, int MaximumRetries, int RetentionDays, string OutboundLocation,
    string InboundLocation, string ArchiveLocation, Guid ConcurrencyToken);

public sealed record AchManagedMftAdministrationDto(
    string ProfileName, string Provider, string Protocol, bool ProfileEnabled, string? Endpoint, int? Port,
    string? Principal, bool AutomaticOutboundEnabled, bool AutomaticInboundEnabled, bool ManualOutboundAllowed,
    bool ManualInboundAllowed, int MaximumRetries, int RetryDelaySeconds, int RetentionDays,
    string OutboundLocation, string InboundLocation, string ArchiveLocation, bool CredentialConfigured,
    string? CredentialType, DateTime? CredentialUpdatedAtUtc, Guid ConcurrencyToken);

public sealed record UpdateAchManagedMftAdministrationRequest(
    string ProfileName, string Provider, string Protocol, bool ProfileEnabled, string? Endpoint, int? Port,
    string? Principal, bool AutomaticOutboundEnabled, bool AutomaticInboundEnabled, bool ManualOutboundAllowed,
    bool ManualInboundAllowed, int MaximumRetries, int RetryDelaySeconds, int RetentionDays,
    string OutboundLocation, string InboundLocation, string ArchiveLocation, Guid ConcurrencyToken);

public sealed record SetAchManagedMftCredentialRequest(string CredentialType, string Secret);
public sealed record AchManagedMftEffectiveConfiguration(bool Enabled, string OutboundPath, string InboundPath, string ProcessingPath, string ArchivePath, long MaximumFileBytes);

public sealed record AchManagedFileExecutionResult(int Processed, int Succeeded, int Failed, IReadOnlyList<Guid> TransferIds);
public sealed record AchManagedFileDownload(string FileName, string ContentType, byte[] Content);
