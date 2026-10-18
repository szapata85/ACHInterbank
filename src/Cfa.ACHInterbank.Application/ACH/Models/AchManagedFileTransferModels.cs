using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchManagedMftArtifact(string FileName, byte[] Content, string ContentSha256, string ClaimReference)
{
    /// <summary>Marca UTC observable del archivo en la frontera CFA, cuando el transporte la provee.</summary>
    public DateTime? BoundaryLastWriteTimeUtc { get; init; }
}
public sealed record AchManagedMftResult(bool Succeeded, bool Retryable, bool Uncertain, string Code, string Message, string? StorageReference);

/// <summary>Resumen operativo de una transferencia administrada de archivos ACH Colombia.</summary>
/// <param name="Id">Identificador único de la transferencia.</param>
/// <param name="FileName">Nombre físico persistido para el archivo transferido.</param>
/// <param name="Direction">Dirección de la transferencia.</param>
/// <param name="OperationalDate">Fecha operativa asociada a la transferencia.</param>
/// <param name="CycleId">Identificador del ciclo ACH asociado cuando existe.</param>
/// <param name="Status">Estado persistido de la transferencia.</param>
/// <param name="ExecutionOrigin">Origen manual o automático de la ejecución.</param>
/// <param name="AttemptCount">Cantidad de intentos registrados para la transferencia.</param>
/// <param name="UpdatedAtUtc">Fecha y hora UTC de la última actualización persistida.</param>
/// <param name="Archived">Indica si la transferencia posee fecha de archivado.</param>
/// <param name="Retired">Indica si la transferencia posee fecha de retiro.</param>
public sealed record AchManagedFileTransferSummary(
    Guid Id, string FileName, AchManagedFileDirection Direction, DateTime OperationalDate, string? CycleId,
    AchManagedFileTransferStatus Status, AchManagedFileExecutionOrigin ExecutionOrigin, int AttemptCount,
    DateTime UpdatedAtUtc, bool Archived, bool Retired)
{
    /// <summary>Etiqueta operativa segura para presentar el estado sin atribuir aceptación externa.</summary>
    public string StatusLabel { get; init; } = string.Empty;

    /// <summary>Estado de evidencia externa; solo es NotConfirmed cuando existe publicación o resultado incierto que requiere confirmación externa.</summary>
    public string ExternalConfirmationStatus { get; init; } = "NotApplicable";

    /// <summary>Mensaje explícito sobre la ausencia de evidencia de recepción por ACH Colombia.</summary>
    public string? ExternalConfirmationMessage { get; init; }
}

/// <summary>Evento persistido en la historia de una transferencia administrada.</summary>
/// <param name="Id">Identificador del evento.</param>
/// <param name="OccurredAtUtc">Fecha y hora UTC en que ocurrió el evento.</param>
/// <param name="EventType">Tipo de evento registrado por el servicio.</param>
/// <param name="Result">Resultado textual persistido para el evento.</param>
/// <param name="Message">Mensaje persistido asociado al evento.</param>
/// <param name="ExecutionOrigin">Origen de ejecución asociado al evento.</param>
/// <param name="Actor">Actor registrado para el evento.</param>
public sealed record AchManagedFileTransferEventDto(
    long Id, DateTime OccurredAtUtc, string EventType, string Result, string Message,
    AchManagedFileExecutionOrigin ExecutionOrigin, string Actor)
{
    /// <summary>Nombre legible del paso para la interfaz operativa.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Resultado legible del paso para la interfaz operativa.</summary>
    public string DisplayResult { get; init; } = string.Empty;

    /// <summary>Mensaje legible que no atribuye recepción o aceptación externa sin evidencia.</summary>
    public string DisplayMessage { get; init; } = string.Empty;

    /// <summary>Actor presentado sin exponer nombres internos de tareas automáticas.</summary>
    public string ActorDisplay { get; init; } = string.Empty;
}

/// <summary>Detalle contractual de una transferencia administrada de archivos ACH Colombia.</summary>
/// <param name="Id">Identificador único de la transferencia.</param>
/// <param name="FileName">Nombre físico persistido para el archivo.</param>
/// <param name="Direction">Dirección de la transferencia.</param>
/// <param name="OperationalDate">Fecha operativa asociada.</param>
/// <param name="CycleId">Identificador del ciclo ACH asociado cuando existe.</param>
/// <param name="Status">Estado persistido de la transferencia.</param>
/// <param name="ExecutionOrigin">Origen manual o automático de la ejecución.</param>
/// <param name="FileSize">Tamaño persistido del archivo en bytes.</param>
/// <param name="ContentSha256">Hash SHA-256 persistido del contenido.</param>
/// <param name="AttemptCount">Cantidad de intentos registrados.</param>
/// <param name="CreatedAtUtc">Fecha y hora UTC de creación.</param>
/// <param name="TransferredAtUtc">Fecha y hora UTC de transferencia cuando fue registrada.</param>
/// <param name="ProcessedAtUtc">Fecha y hora UTC de procesamiento cuando fue registrada.</param>
/// <param name="LastError">Último detalle de error persistido cuando existe.</param>
/// <param name="Archived">Indica si la transferencia está archivada.</param>
/// <param name="ArchivedAtUtc">Fecha y hora UTC de archivado cuando existe.</param>
/// <param name="Retired">Indica si la transferencia fue retirada.</param>
/// <param name="RetiredAtUtc">Fecha y hora UTC de retiro cuando existe.</param>
/// <param name="RetirementReason">Motivo de retiro persistido cuando existe.</param>
/// <param name="CorrectedFromTransferId">Transferencia saliente previa corregida cuando existe.</param>
/// <param name="History">Eventos persistidos asociados a la transferencia.</param>
public sealed record AchManagedFileTransferDetail(
    Guid Id, string FileName, AchManagedFileDirection Direction, DateTime OperationalDate, string? CycleId,
    AchManagedFileTransferStatus Status, AchManagedFileExecutionOrigin ExecutionOrigin, long FileSize,
    string ContentSha256, int AttemptCount, DateTime CreatedAtUtc, DateTime? TransferredAtUtc,
    DateTime? ProcessedAtUtc, string? LastError, bool Archived, DateTime? ArchivedAtUtc,
    bool Retired, DateTime? RetiredAtUtc, string? RetirementReason, Guid? CorrectedFromTransferId,
    IReadOnlyList<AchManagedFileTransferEventDto> History)
{
    /// <summary>Fecha y hora UTC del último intento registrado, cuando existe.</summary>
    public DateTime? LastAttemptAtUtc { get; init; }

    /// <summary>Código del último error persistido, cuando existe.</summary>
    public string? LastErrorCode { get; init; }

    /// <summary>Identificador de correlación persistido para la transferencia.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Identificador del artefacto exportado ACH asociado, cuando existe.</summary>
    public int? AchFileExportId { get; init; }

    /// <summary>Identificador de la ingestión NACHA entrante asociada, cuando existe.</summary>
    public Guid? IncomingNachaFileIngestionId { get; init; }

    /// <summary>Identificadores de transacciones ACH vinculadas al artefacto exportado.</summary>
    public IReadOnlyList<int> TransactionIds { get; init; } = [];

    /// <summary>Indica si el contenido retenido continúa disponible para descarga o reproceso.</summary>
    public bool ContentAvailable { get; init; }

    /// <summary>Indica si el servicio calcula que la transferencia puede reintentarse.</summary>
    public bool CanRetry { get; init; }

    /// <summary>Indica si el servicio calcula que la transferencia puede reprocesarse.</summary>
    public bool CanReprocess { get; init; }

    /// <summary>Indica si el servicio calcula que la transferencia puede archivarse.</summary>
    public bool CanArchive { get; init; }

    /// <summary>Indica si el servicio calcula que la transferencia puede retirarse.</summary>
    public bool CanRetire { get; init; }

    /// <summary>Código de la cámara del flujo. Para este contrato es ACHCOL.</summary>
    public string ClearingHouseCode { get; init; } = "ACHCOL";

    /// <summary>Nombre legible de la cámara.</summary>
    public string ClearingHouseName { get; init; } = "ACH Colombia";

    /// <summary>Etiqueta operativa segura del estado persistido.</summary>
    public string StatusLabel { get; init; } = string.Empty;

    /// <summary>NotConfirmed para outbound mientras no exista evidencia externa verificable.</summary>
    public string ExternalConfirmationStatus { get; init; } = "NotApplicable";

    /// <summary>Explicación legible sobre la evidencia externa realmente disponible.</summary>
    public string? ExternalConfirmationMessage { get; init; }

    /// <summary>Referencia de ubicación activa sanitizada, sin ruta sensible completa.</summary>
    public string? SanitizedActiveReference { get; init; }

    /// <summary>Referencia de archivo sanitizada, sin ruta sensible completa.</summary>
    public string? SanitizedArchiveReference { get; init; }
}

/// <summary>Semántica de presentación del lifecycle MFT. No cambia estados persistidos.</summary>
public static class AchManagedFileTransferPresentation
{
    public const string ExternalNotConfirmed = "NotConfirmed";
    public const string ExternalNotApplicable = "NotApplicable";

    public static string StatusLabel(AchManagedFileDirection direction, AchManagedFileTransferStatus status)
        => status switch
        {
            AchManagedFileTransferStatus.Ready => direction == AchManagedFileDirection.Outbound ? "Generado" : "Pendiente",
            AchManagedFileTransferStatus.InProgress => direction == AchManagedFileDirection.Outbound ? "Publicación iniciada" : "Procesamiento en curso",
            AchManagedFileTransferStatus.Transferred => "Publicado para envío",
            AchManagedFileTransferStatus.Received => "Recibido desde la frontera CFA",
            AchManagedFileTransferStatus.Processed => "Procesado",
            AchManagedFileTransferStatus.Rejected => "Rechazado",
            AchManagedFileTransferStatus.Duplicate => "Duplicado",
            AchManagedFileTransferStatus.RetryPending => "Reintento pendiente",
            AchManagedFileTransferStatus.Uncertain => "Pendiente de confirmación",
            AchManagedFileTransferStatus.Failed => direction == AchManagedFileDirection.Outbound ? "No se pudo publicar" : "Fallido",
            AchManagedFileTransferStatus.Retired => "Retirado",
            _ => status.ToString()
        };

    public static string ExternalConfirmationStatus(AchManagedFileDirection direction, AchManagedFileTransferStatus status)
        => direction == AchManagedFileDirection.Outbound
            && status is AchManagedFileTransferStatus.Transferred or AchManagedFileTransferStatus.Uncertain
                ? ExternalNotConfirmed
                : ExternalNotApplicable;

    public static string? ExternalConfirmationMessage(AchManagedFileDirection direction, AchManagedFileTransferStatus status)
        => direction == AchManagedFileDirection.Outbound
            ? status switch
            {
                AchManagedFileTransferStatus.Transferred => "Recepción por ACH Colombia no confirmada.",
                AchManagedFileTransferStatus.Uncertain => "La publicación en CFA está pendiente de confirmación; la recepción por ACH Colombia no está confirmada.",
                _ => null
            }
            : null;

    public static string EventName(string eventType)
        => eventType switch
        {
            "Generated" => "Generado",
            "Encrypted" => "Cifrado",
            "OutboundPrepared" => "Archivo preparado",
            "PublicationStarted" => "Publicación iniciada",
            "PublishedToMftBoundary" => "Publicado para envío",
            "PublicationUncertain" => "Publicación pendiente de confirmación",
            "PublicationFailed" => "No se pudo publicar",
            "RetryPending" => "Reintento pendiente",
            "OutboundAttempt" => "Intento de publicación",
            "ReceivedFromMftBoundary" or "InboundClaimed" => "Recibido desde la frontera CFA",
            "InboundRecovery" => "Recepción reanudada",
            "InboundRecoveryPending" or "InboundArchivePending" => "Pendiente de recuperación",
            "InboundOutsideExpectedWindow" => "Recibido fuera del horario esperado",
            "InboundProcessingFailed" => "No se pudo procesar",
            "InboundProcessingStarted" => "Procesamiento iniciado",
            "InboundProcessingFinished" => "Procesamiento finalizado",
            "Processed" => "Procesado",
            "InboundRejected" or "Rejected" => "Rechazado",
            "DuplicateDetected" or "Duplicate" => "Duplicado",
            "Archived" => "Archivado",
            "Retired" => "Retirado",
            "Downloaded" => "Descargado",
            "ReprocessStarted" => "Reproceso iniciado",
            "ReprocessFinished" => "Reproceso finalizado",
            "Failed" => "Fallido",
            _ => "Evento de trazabilidad"
        };

    public static string EventResult(string result)
        => result switch
        {
            "Started" => "Iniciado",
            "Succeeded" => "Correcto",
            "Processed" => "Procesado",
            "Ignored" => "Ignorado",
            "Uncertain" => "Pendiente de confirmación",
            "RetryPending" => "Reintento pendiente",
            "Rejected" => "Rechazado",
            "Duplicate" => "Duplicado",
            "Failed" => "Fallido",
            _ => "Registrado"
        };

    public static string EventMessage(string eventType, string result)
        => eventType switch
        {
            "Generated" => "Archivo NACHA-M generado.",
            "Encrypted" => "Archivo cifrado para intercambio.",
            "OutboundPrepared" => "Archivo preparado para publicación en la frontera CFA.",
            "PublicationStarted" => "Publicación en la frontera CFA iniciada.",
            "PublishedToMftBoundary" => "Publicado en la frontera CFA. La recepción por ACH Colombia no está confirmada.",
            "PublicationUncertain" => "No fue posible confirmar la publicación en la frontera CFA.",
            "PublicationFailed" => "No se pudo publicar en la frontera CFA.",
            "RetryPending" => result == "RetryPending" ? "La operación quedó pendiente de reintento." : "Reintento pendiente.",
            "OutboundAttempt" when result == "Started" => "Publicación en la frontera CFA iniciada.",
            "OutboundAttempt" when result == "Succeeded" => "Publicado en la frontera CFA. La recepción por ACH Colombia no está confirmada.",
            "OutboundAttempt" when result == "Uncertain" => "No fue posible confirmar la publicación en la frontera CFA.",
            "OutboundAttempt" when result == "Failed" => "No se pudo publicar en la frontera CFA.",
            "ReceivedFromMftBoundary" or "InboundClaimed" => "Archivo recibido desde la frontera CFA.",
            "InboundRecovery" => "Se reanudó una recepción previamente registrada.",
            "InboundRecoveryPending" => "El archivo quedó pendiente de recuperación.",
            "InboundArchivePending" => "El resultado fue guardado y el archivo quedó pendiente de recuperación para archivado.",
            "InboundOutsideExpectedWindow" => "Archivo recibido fuera del horario esperado.",
            "InboundProcessingFailed" => "No se pudo procesar el archivo.",
            "InboundProcessingStarted" or "ReprocessStarted" => "Procesamiento del archivo iniciado.",
            "InboundProcessingFinished" or "ReprocessFinished" => result switch
            {
                "Processed" => "Procesamiento completado correctamente.",
                "Duplicate" => "Procesamiento completado: archivo duplicado.",
                "RetryPending" => "Procesamiento incompleto: reintento pendiente.",
                "Rejected" => "Procesamiento completado: archivo rechazado.",
                "Failed" => "No fue posible completar el procesamiento.",
                _ => "Procesamiento finalizado."
            },
            "Processed" => "Archivo procesado.",
            "Duplicate" or "DuplicateDetected" => "Archivo duplicado identificado; no se creó un efecto funcional adicional.",
            "Rejected" or "InboundRejected" => "Archivo rechazado durante el procesamiento.",
            "Failed" => "No fue posible procesar el archivo.",
            "Archived" => "Archivo conservado en el archivo operativo.",
            "Retired" => "Archivo retirado del almacenamiento activo; su historial se conserva.",
            "Downloaded" => "Archivo descargado por un operador autorizado.",
            _ => "Evento registrado en la trazabilidad."
        };

    public static string ActorDisplay(AchManagedFileExecutionOrigin origin, string actor)
        => origin == AchManagedFileExecutionOrigin.Automatic
            ? "Automatización ACH Colombia"
            : string.IsNullOrWhiteSpace(actor) ? "Operador" : actor;
}

public sealed record AchManagedFileTransferQuery(
    DateTime? From = null, DateTime? To = null, AchManagedFileDirection? Direction = null,
    AchManagedFileTransferStatus? Status = null, string? CycleId = null,
    AchManagedFileExecutionOrigin? ExecutionOrigin = null,
    string? FileName = null, Guid? TransferId = null, bool? Archived = null,
    int PageNumber = 1, int PageSize = 50);

/// <summary>Configuración operativa del intercambio administrado de archivos ACH Colombia.</summary>
/// <param name="AutomaticOutboundEnabled">Habilita o deshabilita la ejecución automática del flujo saliente.</param>
/// <param name="AutomaticInboundEnabled">Habilita o deshabilita la ejecución automática del flujo entrante.</param>
/// <param name="ManualOutboundAllowed">Indica si el servicio permite ejecución manual saliente.</param>
/// <param name="ManualInboundAllowed">Indica si el servicio permite ejecución manual entrante.</param>
/// <param name="MaximumRetries">Máximo de reintentos configurado; el servicio de actualización admite valores entre 0 y 20.</param>
/// <param name="RetentionDays">Días de retención configurados; el servicio de actualización admite valores entre 1 y 3650.</param>
/// <param name="OutboundLocation">Ubicación operativa saliente; el servicio exige texto no vacío y conserva hasta 120 caracteres.</param>
/// <param name="InboundLocation">Ubicación operativa entrante; el servicio exige texto no vacío y conserva hasta 120 caracteres.</param>
/// <param name="ArchiveLocation">Ubicación operativa de archivo; el servicio exige texto no vacío y conserva hasta 120 caracteres.</param>
/// <param name="ConcurrencyToken">Token de concurrencia optimista que debe corresponder a la versión persistida al actualizar.</param>
public sealed record AchManagedFileTransferConfigurationDto(
    bool AutomaticOutboundEnabled, bool AutomaticInboundEnabled, bool ManualOutboundAllowed,
    bool ManualInboundAllowed, int MaximumRetries, int RetentionDays, string OutboundLocation,
    string InboundLocation, string ArchiveLocation, Guid ConcurrencyToken);

/// <summary>Perfil administrativo efectivo del intercambio administrado ACH Colombia.</summary>
/// <param name="ProfileName">Nombre persistido del perfil.</param>
/// <param name="Provider">Proveedor configurado para el perfil.</param>
/// <param name="Protocol">Protocolo configurado para el perfil.</param>
/// <param name="ProfileEnabled">Indica si el perfil está habilitado.</param>
/// <param name="Endpoint">Endpoint configurado, cuando existe.</param>
/// <param name="Port">Puerto configurado, cuando existe.</param>
/// <param name="Principal">Principal o usuario técnico configurado, cuando existe.</param>
/// <param name="AutomaticOutboundEnabled">Indica si está habilitada la ejecución automática saliente.</param>
/// <param name="AutomaticInboundEnabled">Indica si está habilitada la ejecución automática entrante.</param>
/// <param name="ManualOutboundAllowed">Indica si está permitida la ejecución manual saliente.</param>
/// <param name="ManualInboundAllowed">Indica si está permitida la ejecución manual entrante.</param>
/// <param name="MaximumRetries">Máximo de reintentos configurado.</param>
/// <param name="RetryDelaySeconds">Demora configurada entre reintentos, en segundos.</param>
/// <param name="RetentionDays">Días de retención configurados.</param>
/// <param name="OutboundLocation">Ubicación operativa saliente.</param>
/// <param name="InboundLocation">Ubicación operativa entrante.</param>
/// <param name="ArchiveLocation">Ubicación operativa de archivo.</param>
/// <param name="CredentialConfigured">Indica si existe una credencial protegida configurada; no revela su contenido.</param>
/// <param name="CredentialType">Tipo de credencial configurado, cuando existe; no contiene el secreto.</param>
/// <param name="CredentialUpdatedAtUtc">Fecha y hora UTC de la última actualización de credencial, cuando existe.</param>
/// <param name="ConcurrencyToken">Token de concurrencia optimista vigente.</param>
public sealed record AchManagedMftAdministrationDto(
    string ProfileName, string Provider, string Protocol, bool ProfileEnabled, string? Endpoint, int? Port,
    string? Principal, bool AutomaticOutboundEnabled, bool AutomaticInboundEnabled, bool ManualOutboundAllowed,
    bool ManualInboundAllowed, int MaximumRetries, int RetryDelaySeconds, int RetentionDays,
    string OutboundLocation, string InboundLocation, string ArchiveLocation, bool CredentialConfigured,
    string? CredentialType, DateTime? CredentialUpdatedAtUtc, Guid ConcurrencyToken)
{
    /// <summary>Fingerprint SHA-256 de la host key del SFTP corporativo CFA. No contiene secretos.</summary>
    public string? SftpHostKeyFingerprint { get; init; }

    /// <summary>Timeout técnico de conexión/operación SFTP, en segundos.</summary>
    public int SftpTimeoutSeconds { get; init; } = 30;
}

/// <summary>Solicitud para actualizar el perfil administrativo del intercambio ACH Colombia.</summary>
/// <param name="ProfileName">Nombre requerido del perfil; el servicio exige texto no vacío y conserva hasta 120 caracteres.</param>
/// <param name="Provider">Proveedor requerido; el servicio exige texto no vacío y conserva hasta 60 caracteres.</param>
/// <param name="Protocol">Protocolo requerido; el servicio exige texto no vacío y conserva hasta 40 caracteres.</param>
/// <param name="ProfileEnabled">Indica si el perfil debe quedar habilitado.</param>
/// <param name="Endpoint">Endpoint opcional; el servicio normaliza vacío a null y conserva hasta 300 caracteres.</param>
/// <param name="Port">Puerto opcional; cuando se informa el servicio admite valores entre 1 y 65535.</param>
/// <param name="Principal">Principal opcional; el servicio normaliza vacío a null y conserva hasta 160 caracteres.</param>
/// <param name="AutomaticOutboundEnabled">Habilita o deshabilita ejecución automática saliente.</param>
/// <param name="AutomaticInboundEnabled">Habilita o deshabilita ejecución automática entrante.</param>
/// <param name="ManualOutboundAllowed">Permite o impide ejecución manual saliente.</param>
/// <param name="ManualInboundAllowed">Permite o impide ejecución manual entrante.</param>
/// <param name="MaximumRetries">Máximo de reintentos; el servicio admite valores entre 0 y 20.</param>
/// <param name="RetryDelaySeconds">Demora entre reintentos en segundos; el servicio admite valores entre 0 y 86400.</param>
/// <param name="RetentionDays">Días de retención; el servicio admite valores entre 1 y 3650.</param>
/// <param name="OutboundLocation">Ubicación saliente requerida; el servicio conserva hasta 120 caracteres.</param>
/// <param name="InboundLocation">Ubicación entrante requerida; el servicio conserva hasta 120 caracteres.</param>
/// <param name="ArchiveLocation">Ubicación de archivo requerida; el servicio conserva hasta 120 caracteres.</param>
/// <param name="ConcurrencyToken">Token de concurrencia que debe corresponder a la versión persistida.</param>
public sealed record UpdateAchManagedMftAdministrationRequest(
    string ProfileName, string Provider, string Protocol, bool ProfileEnabled, string? Endpoint, int? Port,
    string? Principal, bool AutomaticOutboundEnabled, bool AutomaticInboundEnabled, bool ManualOutboundAllowed,
    bool ManualInboundAllowed, int MaximumRetries, int RetryDelaySeconds, int RetentionDays,
    string OutboundLocation, string InboundLocation, string ArchiveLocation, Guid ConcurrencyToken)
{
    public string? SftpHostKeyFingerprint { get; init; }
    public int SftpTimeoutSeconds { get; init; } = 30;
}

/// <summary>Solicitud para establecer la credencial protegida del perfil ACH Colombia.</summary>
/// <param name="CredentialType">Tipo de credencial. El servicio exige texto no vacío y conserva hasta 40 caracteres.</param>
/// <param name="Secret">Valor secreto requerido. Se recibe para protección y persistencia, y no se devuelve en la respuesta administrativa.</param>
/// <param name="ConcurrencyToken">Token de concurrencia obtenido al consultar la configuración administrativa.</param>
public sealed record SetAchManagedMftCredentialRequest(string CredentialType, string Secret, Guid ConcurrencyToken);

/// <summary>Resultado seguro de una prueba administrativa de conectividad con la frontera MFT CFA.</summary>
public sealed record AchManagedMftConnectionTestResult(bool Succeeded, string Code, string Message);

public sealed record AchManagedMftEffectiveConfiguration(bool Enabled, string OutboundPath, string InboundPath, string ProcessingPath, string ArchivePath, long MaximumFileBytes)
{
    public string Provider { get; init; } = "ManagedFolder";
    public string Protocol { get; init; } = "ManagedFile";
    public string? Endpoint { get; init; }
    public int? Port { get; init; }
    public string? Principal { get; init; }
    public string? CredentialType { get; init; }
    public string? ProtectedCredential { get; init; }
    public string? SftpHostKeyFingerprint { get; init; }
    public int SftpTimeoutSeconds { get; init; } = 30;
}

/// <summary>Resultado agregado de una ejecución de intercambio administrado ACH Colombia.</summary>
/// <param name="Processed">Cantidad de artefactos considerados por la ejecución.</param>
/// <param name="Succeeded">Cantidad de artefactos cuyo procesamiento o transferencia terminó con éxito.</param>
/// <param name="Failed">Cantidad de artefactos contabilizados como fallidos por la ejecución.</param>
/// <param name="TransferIds">Identificadores de transferencias involucradas en la ejecución.</param>
public sealed record AchManagedFileExecutionResult(int Processed, int Succeeded, int Failed, IReadOnlyList<Guid> TransferIds);

/// <summary>Contenido descargable retenido para una transferencia administrada.</summary>
/// <param name="FileName">Nombre de archivo que el controller entrega a <c>File(...)</c> como nombre de descarga.</param>
/// <param name="ContentType">Media type que el controller entrega a <c>File(...)</c>.</param>
/// <param name="Content">Bytes retenidos que se devuelven al cliente.</param>
public sealed record AchManagedFileDownload(string FileName, string ContentType, byte[] Content);
