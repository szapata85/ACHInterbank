namespace Cfa.ACHInterbank.Api.Contracts.AchResponses;

/// <summary>Contrato para crear o reemplazar un mapping operativo de estados de respuesta ACH.</summary>
/// <param name="ClearingHouseId">Identificador de la cámara compensadora; el servicio exige un valor mayor que cero y una cámara activa.</param>
/// <param name="ResponseType">Tipo de respuesta interpretado por el servicio mediante el enum TipoRespuestaAch sin distinguir mayúsculas y minúsculas.</param>
/// <param name="ExternalCode">Código de estado externo; es obligatorio y el servicio lo normaliza con Trim y mayúsculas.</param>
/// <param name="ExternalCause">Causal externa opcional; cuando tiene valor el servicio la normaliza con Trim y mayúsculas.</param>
/// <param name="InternalStatusId">Identificador del estado interno asociado al mapping.</param>
/// <param name="ExternalServiceStatusId">Identificador del estado del servicio externo asociado al mapping.</param>
/// <param name="InternalStatusName">Nombre del estado interno; es obligatorio y se persiste sin espacios exteriores.</param>
/// <param name="NormalizedCause">Causal normalizada opcional; cuando tiene valor el servicio la normaliza con Trim y mayúsculas.</param>
/// <param name="NormalizedDescription">Descripción normalizada opcional; cuando tiene valor se persiste sin espacios exteriores.</param>
/// <param name="RequiresCause">Indica el valor configurado para requerimiento de causal en el mapping.</param>
/// <param name="AllowsNotification">Indica el valor configurado para permitir notificación desde el mapping.</param>
/// <param name="Priority">Prioridad usada por el servicio al ordenar y verificar solapamientos de mappings activos.</param>
/// <param name="EffectiveFrom">Inicio de vigencia; el servicio lo trata como UTC cuando el Kind no es UTC.</param>
/// <param name="EffectiveTo">Fin de vigencia opcional; no puede ser anterior a EffectiveFrom.</param>
/// <param name="IsActive">Estado activo solicitado para el mapping.</param>
/// <param name="ExpectedVersion">Versión optimista opcional al crear y obligatoria al actualizar un mapping existente.</param>
/// <param name="Reason">Motivo de auditoría; el servicio exige un valor no vacío.</param>
public sealed record AchResponseMappingWriteRequest(
    int ClearingHouseId, string ResponseType, string ExternalCode, string? ExternalCause,
    int InternalStatusId, int ExternalServiceStatusId, string InternalStatusName,
    string? NormalizedCause, string? NormalizedDescription, bool RequiresCause, bool AllowsNotification,
    int Priority, DateTime EffectiveFrom, DateTime? EffectiveTo, bool IsActive,
    Guid? ExpectedVersion, string Reason);

/// <summary>Contrato de transición que aplica control optimista y conserva un motivo auditable.</summary>
/// <param name="ExpectedVersion">Versión que debe coincidir con la versión persistida de la entidad.</param>
/// <param name="Reason">Motivo de la operación; el servicio exige un valor no vacío.</param>
/// <param name="CorrelationId">Identificador de correlación opcional; cuando se informa prevalece sobre el header X-Correlation-ID.</param>
public sealed record VersionedReasonRequest(Guid ExpectedVersion, string Reason, string? CorrelationId = null);

/// <summary>Contrato para registrar una respuesta ACH como huérfana.</summary>
/// <param name="Reason">Motivo del registro huérfano; el servicio exige un valor no vacío.</param>
/// <param name="CandidateReferences">Referencias candidatas opcionales que se conservan como evidencia del caso.</param>
/// <param name="CorrelationId">Identificador de correlación opcional; cuando se informa prevalece sobre el header X-Correlation-ID.</param>
public sealed record CreateOrphanRequest(string Reason, string? CandidateReferences, string? CorrelationId = null);

/// <summary>Contrato para resolver o rechazar una respuesta ACH huérfana en revisión.</summary>
/// <param name="ExpectedVersion">Versión que debe coincidir con la versión persistida del registro huérfano.</param>
/// <param name="Reason">Motivo de la resolución; el servicio exige un valor no vacío.</param>
/// <param name="FunctionalReference">Referencia funcional opcional; es obligatoria cuando Reject es false y debe identificar una sola transacción en la misma cámara.</param>
/// <param name="Reject">Indica si la revisión termina como Rejected en lugar de asociarse a una referencia funcional y terminar como Resolved.</param>
/// <param name="CorrelationId">Identificador de correlación opcional; cuando se informa prevalece sobre el header X-Correlation-ID.</param>
public sealed record ResolveOrphanRequest(Guid ExpectedVersion, string Reason, string? FunctionalReference,
    bool Reject, string? CorrelationId = null);

/// <summary>Contrato para solicitar el reproceso de una respuesta ACH.</summary>
/// <param name="CommandId">Identificador idempotente de la solicitud de reproceso; una repetición devuelve el intento ya asociado al mismo comando.</param>
/// <param name="ExpectedVersion">Versión que debe coincidir con la versión persistida de la respuesta para crear un intento nuevo.</param>
/// <param name="Reason">Motivo de reproceso; el servicio exige un valor no vacío.</param>
/// <param name="CorrelationId">Identificador de correlación opcional; cuando se informa prevalece sobre el header X-Correlation-ID.</param>
public sealed record ReprocessResponseRequest(Guid CommandId, Guid ExpectedVersion, string Reason, string? CorrelationId = null);

public sealed record ResolveReconciliationRequest(Guid ExpectedVersion, string Resolution, string Reason,
    string? CorrelationId = null);
