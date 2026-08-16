namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ReturnEligibleTransactionDto(
    int Id,
    string TraceNumber,
    decimal Amount,
    string TransactionCode,
    string Reference,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    string OriginatingDfi,
    string ReceivingDfi,
    string AchCycleId,
    DateTime EffectiveEntryDate,
    bool IsPrenotification,
    bool IsEligible,
    string? ValidationMessage);

public sealed record ReturnSelectionItemDto(
    int TransactionId,
    string ReturnReasonCode,
    CenitIncomingReturnOperationalEvidence? CenitOperationalEvidence = null);

public sealed record GenerateReturnsFileRequest(
    string CycleId,
    IReadOnlyList<ReturnSelectionItemDto> Items,
    string? ReturnCycleId = null);

public sealed record GenerateReturnsFileResponse(string FileName, string ContentType, byte[] Content, int TotalRecords, int TotalReturns);
