using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Claims;

namespace Cfa.ACHInterbank.Application.ACH.Claims.Models;

public sealed record CreateDev14ClaimCaseRequest(
    IReadOnlyCollection<int> TransactionIds,
    string? ExternalReference = null);

public sealed record RecordClaimProgressRequest(string? Observation = null);
public sealed record ResolveClaimCaseRequest(string? ResolutionNotes = null);
public sealed record ExpireClaimCaseRequest(string? Observation = null);

public sealed class ClaimCaseQuery
{
    public ClaimCaseStatus? Status { get; set; }
    public ClaimCauseCode? CauseCode { get; set; }
    public int? TransactionId { get; set; }
    public int? ParticipantId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed record ClaimCaseQueryResult(
    IReadOnlyList<ClaimCaseListItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalItems);

public sealed record ClaimParticipantDto(
    int Id,
    string Name,
    string RoutingNumber,
    string TransitCode);

public sealed record ClaimCaseListItemDto(
    int Id,
    ClaimCauseCode CauseCode,
    ClaimCaseStatus Status,
    DateTimeOffset FiledAt,
    DateTimeOffset? DeadlineAt,
    DateTimeOffset? ClosedAt,
    string? ExternalReference,
    ClaimParticipantDto RequestingParticipant,
    ClaimParticipantDto RespondingParticipant,
    int TransactionCount);

public sealed record ClaimTransactionDto(
    int Id,
    string TraceNumber,
    string TransactionExternalId,
    decimal Amount,
    TransactionTypeEnum Type,
    ClaimParticipantDto SourceInstitution,
    ClaimParticipantDto DestinationInstitution);

public sealed record ClaimCaseEventDto(
    long Id,
    ClaimCaseEventType EventType,
    DateTimeOffset OccurredAt,
    string? Observation,
    string? Actor);

public sealed record ClaimCaseDetailDto(
    int Id,
    ClaimCauseCode CauseCode,
    ClaimCaseStatus Status,
    DateTimeOffset FiledAt,
    DateTimeOffset? DeadlineAt,
    DateTimeOffset? ClosedAt,
    string? ExternalReference,
    ClaimParticipantDto RequestingParticipant,
    ClaimParticipantDto RespondingParticipant,
    IReadOnlyList<ClaimTransactionDto> Transactions,
    IReadOnlyList<ClaimCaseEventDto> History,
    string? ResolutionNotes);

public sealed class ClaimCaseConflictException : InvalidOperationException
{
    public ClaimCaseConflictException(string message) : base(message)
    {
    }
}
