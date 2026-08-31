using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using System.Text.Json.Serialization;

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
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CenitChamberResponseType ResponseType,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CenitChamberResponseState State,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CenitChamberCorrelationOutcome CorrelationOutcome,
    int? RelatedFileId,
    string? RelatedFileName,
    string? AchCycleId,
    string? XmlNamespace,
    string? MessageGroupId,
    string? MessageStatus,
    DateTime? MessageCreatedAtUtc,
    string? OriginatingSender,
    string? RelatedReference,
    int? RelatedTransactionId,
    string? TransactionTraceNumber,
    string? ReasonCode,
    string? Description,
    DateTime ReceivedAtUtc,
    DateTime? ProcessedAtUtc,
    bool IsApplied,
    string? ProblemCode,
    int ItemSequence,
    int ItemCount);

public sealed record CenitChamberResponsePage(
    IReadOnlyList<CenitChamberResponseResult> Items,
    int Total,
    int Page,
    int PageSize);
