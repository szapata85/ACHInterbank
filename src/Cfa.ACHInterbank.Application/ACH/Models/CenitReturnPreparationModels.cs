namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>La ejecución debe corresponder exactamente al último resultado del detector I02.</summary>
public sealed record PrepareCenitReturnRequest(int OriginalTransactionId, Guid ExpectedExecutionId);

/// <summary>Reserva administrativa; nunca constituye orden financiera ni archivo transmisible.</summary>
public sealed record CenitReturnPreparationItem(
    Guid Id, int OriginalTransactionId, Guid SourceExecutionId, Guid SourceQueueId,
    string OriginalCycleId, string SuggestedReturnCycleId, string ReasonCode,
    DateTime OriginalValueDate, DateTime SuggestedValueDate,
    DateTime PreparedAtUtc, string Status, bool ProcessingConnected);
