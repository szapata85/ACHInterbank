namespace Cfa.ACHInterbank.Application.ACH.Responses.Operations;

public sealed record AchResponseMappingCommand(
    int ClearingHouseId,
    string ResponseType,
    string ExternalCode,
    string? ExternalCause,
    int InternalStatusId,
    int ExternalServiceStatusId,
    string InternalStatusName,
    string? NormalizedCause,
    string? NormalizedDescription,
    bool RequiresCause,
    bool AllowsNotification,
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    Guid? ExpectedVersion,
    string Reason);

/// <summary>Representa un mapping persistido entre una respuesta externa y estados internos de procesamiento.</summary>
/// <param name="Id">Identificador numérico del mapping.</param>
/// <param name="ClearingHouseId">Identificador de la cámara compensadora asociada.</param>
/// <param name="ClearingHouseCode">Código de la cámara compensadora asociada.</param>
/// <param name="ResponseType">Tipo de respuesta persistido.</param>
/// <param name="ExternalCode">Código de estado externo normalizado.</param>
/// <param name="ExternalCause">Causal externa normalizada cuando existe.</param>
/// <param name="InternalStatusId">Identificador del estado interno configurado.</param>
/// <param name="ExternalServiceStatusId">Identificador del estado del servicio externo configurado.</param>
/// <param name="InternalStatusName">Nombre del estado interno configurado.</param>
/// <param name="NormalizedCause">Causal normalizada configurada cuando existe.</param>
/// <param name="NormalizedDescription">Descripción normalizada configurada cuando existe.</param>
/// <param name="RequiresCause">Indica si el mapping está configurado para requerir causal.</param>
/// <param name="AllowsNotification">Indica si el mapping está configurado para permitir notificación.</param>
/// <param name="Priority">Prioridad del mapping.</param>
/// <param name="EffectiveFrom">Inicio de vigencia persistido.</param>
/// <param name="EffectiveTo">Fin de vigencia persistido cuando existe.</param>
/// <param name="IsActive">Indica si el mapping está activo.</param>
/// <param name="Version">Versión usada por las operaciones de control optimista.</param>
public sealed record AchResponseMappingModel(
    int Id, int ClearingHouseId, string ClearingHouseCode, string ResponseType, string ExternalCode,
    string? ExternalCause, int InternalStatusId, int ExternalServiceStatusId, string InternalStatusName,
    string? NormalizedCause, string? NormalizedDescription, bool RequiresCause, bool AllowsNotification,
    int Priority, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsActive, Guid Version);

/// <summary>Evento de auditoría asociado a mappings, respuestas u otras entidades del flujo de respuestas ACH.</summary>
/// <param name="Id">Identificador numérico del evento.</param>
/// <param name="EntityType">Tipo de entidad auditada.</param>
/// <param name="EntityId">Identificador textual de la entidad auditada.</param>
/// <param name="Action">Acción registrada por la auditoría.</param>
/// <param name="PreviousState">Estado anterior cuando fue registrado.</param>
/// <param name="NewState">Estado nuevo cuando fue registrado.</param>
/// <param name="Actor">Actor asociado al cambio.</param>
/// <param name="Reason">Motivo registrado para la operación.</param>
/// <param name="CorrelationId">Identificador de correlación persistido con el evento.</param>
/// <param name="OccurredAtUtc">Fecha y hora UTC del evento.</param>
/// <param name="SanitizedMetadata">Metadata sanitizada adicional cuando existe.</param>
public sealed record AchResponseAuditModel(
    long Id, string EntityType, string EntityId, string Action, string? PreviousState, string? NewState,
    string Actor, string Reason, string CorrelationId, DateTime OccurredAtUtc, string? SanitizedMetadata);

/// <summary>Representa una respuesta ACH marcada como huérfana y su estado de resolución manual.</summary>
/// <param name="Id">Identificador del registro huérfano.</param>
/// <param name="AchResponseId">Identificador de la respuesta ACH asociada.</param>
/// <param name="ClearingHouseId">Identificador de la cámara compensadora canónica de la respuesta.</param>
/// <param name="ResponseType">Tipo de respuesta conservado al crear el huérfano.</param>
/// <param name="ExternalIdentifiers">Identificadores externos conservados por el servicio.</param>
/// <param name="ExternalCode">Código externo de la respuesta.</param>
/// <param name="ReceivedAtUtc">Fecha de recepción de la respuesta asociada.</param>
/// <param name="OperationalDate">Fecha operativa de la respuesta asociada.</param>
/// <param name="CorrelationId">Identificador de correlación del caso.</param>
/// <param name="OrphanReason">Motivo registrado al crear el huérfano.</param>
/// <param name="CandidateReferences">Referencias candidatas conservadas cuando fueron informadas.</param>
/// <param name="ResolutionStatus">Estado de resolución manual persistido, por ejemplo Pending, InReview, Resolved o Rejected según las transiciones ejecutadas.</param>
/// <param name="ResolvedReference">Referencia funcional asociada cuando la resolución la establece.</param>
/// <param name="ResolvedAtUtc">Fecha y hora de resolución cuando el caso fue finalizado.</param>
/// <param name="Version">Versión usada por el control optimista de las operaciones manuales.</param>
public sealed record AchResponseOrphanModel(
    Guid Id, Guid AchResponseId, int ClearingHouseId, string ResponseType, string ExternalIdentifiers,
    string ExternalCode, DateTime ReceivedAtUtc, DateTime OperationalDate, string CorrelationId,
    string OrphanReason, string? CandidateReferences, string ResolutionStatus, string? ResolvedReference,
    DateTime? ResolvedAtUtc, Guid Version);

/// <summary>Representa un intento persistido de reproceso de una respuesta ACH.</summary>
/// <param name="Id">Identificador numérico del intento.</param>
/// <param name="AchResponseId">Identificador de la respuesta ACH asociada.</param>
/// <param name="AttemptNumber">Número secuencial del intento dentro de la respuesta.</param>
/// <param name="Status">Estado persistido del intento.</param>
/// <param name="RequestedBy">Actor que solicitó el reproceso.</param>
/// <param name="Reason">Motivo registrado para el reproceso.</param>
/// <param name="CorrelationId">Identificador de correlación persistido con el intento.</param>
/// <param name="RequestedAtUtc">Fecha y hora UTC de solicitud.</param>
/// <param name="CompletedAtUtc">Fecha y hora UTC de finalización cuando existe.</param>
/// <param name="Result">Resultado textual persistido cuando existe.</param>
/// <param name="CommandId">Identificador idempotente de la solicitud de reproceso.</param>
/// <param name="ClaimedBy">Actor o proceso que reclamó el intento cuando existe.</param>
/// <param name="StartedAtUtc">Fecha y hora UTC de inicio cuando existe.</param>
/// <param name="ResultCode">Código de resultado persistido cuando existe.</param>
/// <param name="ErrorDetailSanitized">Detalle de error sanitizado cuando existe.</param>
public sealed record AchResponseReprocessModel(
    long Id, Guid AchResponseId, int AttemptNumber, string Status, string RequestedBy, string Reason,
    string CorrelationId, DateTime RequestedAtUtc, DateTime? CompletedAtUtc, string? Result, Guid CommandId,
    string? ClaimedBy = null, DateTime? StartedAtUtc = null, string? ResultCode = null,
    string? ErrorDetailSanitized = null);

public sealed record AchResponseReconciliationCaseModel(
    Guid Id, int ClearingHouseId, Guid? AchResponseId, string ExceptionType, string Status, string Reference,
    string? Details, DateTime DetectedAtUtc, string? Resolution, string? ResolutionReason,
    string? ResolvedBy, DateTime? ResolvedAtUtc, string CorrelationId, Guid Version);

public sealed record ManualResolutionCommand(
    Guid ExpectedVersion, string Reason, string? FunctionalReference, bool Reject, string CorrelationId);

public sealed record ReprocessCommand(Guid CommandId, Guid ExpectedVersion, string Reason, string CorrelationId);

public sealed record ReconciliationResolutionCommand(
    Guid ExpectedVersion, string Resolution, string Reason, string CorrelationId);

public class AchResponseOperationException : Exception
{
    public AchResponseOperationException(string message) : base(message) { }
}

public sealed class AchResponseNotFoundException : AchResponseOperationException
{
    public AchResponseNotFoundException(string message) : base(message) { }
}

public sealed class AchResponseConflictException : AchResponseOperationException
{
    public AchResponseConflictException(string message, Guid? currentVersion = null) : base(message)
        => CurrentVersion = currentVersion;
    public Guid? CurrentVersion { get; }
}
