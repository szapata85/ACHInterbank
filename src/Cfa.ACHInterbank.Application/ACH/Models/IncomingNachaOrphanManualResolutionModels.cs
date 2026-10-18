namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum IncomingNachaOrphanResolutionAction
{
    MarkAsIgnored = 1,
    MarkAsRejected = 2,
    KeepPending = 3,
    LinkToTransaction = 4
}

public sealed class IncomingNachaOrphanManualResolutionRequest
{
    public Guid? IncomingNachaTransactionLinkId { get; init; }
    public Guid? IncomingNachaFileIngestionId { get; init; }
    public int? EntryDetailId { get; init; }
    public int? AddendaRecordId { get; init; }
    public IncomingNachaOrphanResolutionAction ResolutionAction { get; init; } = IncomingNachaOrphanResolutionAction.MarkAsIgnored;
    public int? ResolvedAchTransactionId { get; init; }
    public string ResolutionReason { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
    public string ResolvedBy { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>Resultado de la resolución manual de una devolución entrante.</summary>
/// <param name="IsResolved">Indica si la solicitud terminó con una resolución aplicada o un replay equivalente.</param>
/// <param name="Status">Código textual del resultado producido por el servicio.</param>
/// <param name="ProcessingEventId">Identificador del evento de procesamiento creado, cuando existe.</param>
/// <param name="AchTransactionStateEventId">Identificador del evento de estado de la transacción ACH, cuando fue creado o recuperado.</param>
/// <param name="Message">Descripción funcional del resultado.</param>
public sealed record IncomingNachaOrphanManualResolutionResult(
    bool IsResolved,
    string Status,
    Guid? ProcessingEventId,
    long? AchTransactionStateEventId,
    string Message)
{
    /// <summary>Indica que la misma relación ya se había aplicado y no se generaron efectos adicionales.</summary>
    public bool IsIdempotentReplay { get; init; }
}
