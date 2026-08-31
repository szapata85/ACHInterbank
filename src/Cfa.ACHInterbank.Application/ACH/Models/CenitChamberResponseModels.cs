using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CenitChamberResponseImportCommand(
    string SourceResponseId,
    string SourceFileName,
    string MessageType,
    string? Content,
    DateTime ReceivedAtUtc,
    string? RelatedOutboundFileName = null,
    string? RelatedReference = null,
    string? TransactionTraceNumber = null,
    string? AchCycleId = null);

public sealed record CenitChamberResponseResult(
    Guid Id,
    bool IsDuplicate,
    string SourceResponseId,
    string SourceFileName,
    string RawTechnicalReference,
    CenitChamberResponseType ResponseType,
    CenitChamberResponseState State,
    CenitChamberCorrelationOutcome CorrelationOutcome,
    int? RelatedFileId,
    string? RelatedFileName,
    int? RelatedTransactionId,
    string? TransactionTraceNumber,
    string? ReasonCode,
    string? Description,
    DateTime ReceivedAtUtc,
    DateTime? ProcessedAtUtc,
    bool IsApplied,
    string? ProblemCode);

public sealed record CenitChamberResponsePage(
    IReadOnlyList<CenitChamberResponseResult> Items,
    int Total,
    int Page,
    int PageSize);
