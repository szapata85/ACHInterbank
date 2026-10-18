using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

namespace Cfa.ACHInterbank.Application.Scheduler.Models;

/// <summary>Resumen operativo del scheduler y sus instancias registradas.</summary>
/// <param name="TotalInstances">Cantidad total de instancias persistidas para el nombre de scheduler configurado.</param>
/// <param name="ActiveInstances">Instancias sin fecha de detención cuyo último heartbeat está dentro del umbral de actividad configurado.</param>
/// <param name="OfflineInstances">Instancias detenidas o cuyo último heartbeat excede el umbral de desconexión configurado.</param>
/// <param name="RunningJobs">Ejecuciones iniciadas en las últimas 24 horas que permanecen con estado Running.</param>
/// <param name="UpcomingExecutions">Tareas reconocidas por el catálogo que actualmente tienen una próxima ejecución calculada.</param>
/// <param name="RecentFailures">Ejecuciones iniciadas en las últimas 24 horas cuyo estado persistido es Failed.</param>
/// <param name="RecentMisfires">Ejecuciones iniciadas en las últimas 24 horas con estado Misfired o con el indicador MisfireDetected.</param>
/// <param name="SchedulerName">Nombre reportado por la instancia Quartz obtenida mediante ISchedulerFactory.</param>
/// <param name="PersistentStore">Indica si la configuración vigente de Quartz usa almacenamiento persistente.</param>
/// <param name="Clustered">Indica si la configuración vigente de Quartz habilita operación en clúster.</param>
/// <param name="PendingSynchronizations">Cantidad de definiciones cuya marca SchedulerSynchronizationStatus es distinta de Synchronized.</param>
public sealed record SchedulerOverviewDto(
    int TotalInstances,
    int ActiveInstances,
    int OfflineInstances,
    int RunningJobs,
    int UpcomingExecutions,
    int RecentFailures,
    int RecentMisfires,
    string SchedulerName,
    bool PersistentStore,
    bool Clustered,
    int PendingSynchronizations);

/// <summary>Vista operativa y de programación de una tarea administrada por el scheduler.</summary>
/// <param name="TaskCode">Código público de tarea definido por SchedulerTaskCatalog.</param>
/// <param name="Name">Nombre legible definido por SchedulerTaskCatalog.</param>
/// <param name="Description">Descripción funcional definida por SchedulerTaskCatalog.</param>
/// <param name="Status">Estado derivado de la definición persistida: Pausada, Activa o Deshabilitada.</param>
/// <param name="ClearingHouse">Nombres de cámaras asociados a los contextos operativos cuando la tarea usa programación por ciclo; en otro caso es null.</param>
/// <param name="ScheduleDescription">Descripción legible de la programación calculada por el servicio.</param>
/// <param name="CronExpression">Expresión cron persistida cuando la periodicidad utiliza cron; en otros casos puede ser null.</param>
/// <param name="TimeZoneId">Identificador de zona horaria persistido; cuando no existe, el servicio expone America/Bogota.</param>
/// <param name="MisfirePolicy">Política de misfire persistida para la tarea.</param>
/// <param name="MisfireDescription">Descripción legible de la política de misfire aplicada por el servicio.</param>
/// <param name="LastExecutionUtc">Inicio de la ejecución persistida más reciente de la tarea, si existe.</param>
/// <param name="NextExecutionUtc">Próximo instante de disparo obtenido de los triggers Quartz asociados al job, si existe.</param>
/// <param name="LastResult">Nombre del estado de la última ejecución persistida, si existe.</param>
/// <param name="LastDurationMilliseconds">Duración en milisegundos de la última ejecución persistida, cuando está disponible.</param>
/// <param name="LastSchedulerInstance">Nombre de la instancia scheduler que ejecutó la última ejecución, cuando está disponible.</param>
/// <param name="CurrentState">Estado operativo calculado por el servicio: En ejecución, Pausada o En espera.</param>
/// <param name="ManualExecutionEnabled">Indica si el catálogo permite ejecución manual y la definición persistida también la tiene habilitada.</param>
/// <param name="RequestsRecovery">Valor persistido que indica si el job solicita recuperación de Quartz.</param>
/// <param name="AllowsConcurrentExecution">Indica si la política de concurrencia persistida es AllowParallel.</param>
/// <param name="PeriodicityType">Valor numérico de PeriodicityTypeEnum persistido para la tarea.</param>
/// <param name="N">Intervalo configurado para periodicidades que usan un valor N, cuando aplica.</param>
/// <param name="Minute">Minuto configurado para periodicidades horarias, cuando aplica.</param>
/// <param name="TimeOfDay">Hora persistida formateada como HH:mm cuando la periodicidad usa hora del día.</param>
/// <param name="WeeklyDay">Día de la semana configurado para periodicidad semanal, cuando aplica.</param>
/// <param name="MonthDay">Día del mes configurado para periodicidad mensual, cuando aplica.</param>
/// <param name="OnlyBusinessDays">Indica si la política de calendario persistida es OnlyBusinessDays.</param>
/// <param name="StartAt">Fecha/hora inicial persistida para la programación, cuando existe.</param>
/// <param name="EndAt">Fecha/hora final persistida para la programación, cuando existe.</param>
/// <param name="SynchronizationStatus">Marca persistida del estado de sincronización de la definición con Quartz.</param>
/// <param name="LastSynchronizationError">Último mensaje de error de sincronización persistido, si existe.</param>
/// <param name="Category">Categoría funcional definida por SchedulerTaskCatalog.</param>
/// <param name="ProcessType">Tipo de proceso definido por SchedulerTaskCatalog.</param>
/// <param name="SoapService">Servicio SOAP asociado por el catálogo cuando la tarea lo declara.</param>
/// <param name="UsesCycleSchedule">Indica si el catálogo determina que la programación proviene de ciclos de compensación.</param>
/// <param name="CanEditSchedule">Indica si la programación puede editarse directamente; es false para tareas gobernadas por ciclos.</param>
/// <param name="OperationalContexts">Contextos de ciclo calculados para tareas gobernadas por ciclos; en otros casos la colección está vacía.</param>
public sealed record SchedulerTaskDto(
    string TaskCode,
    string Name,
    string Description,
    string Status,
    string? ClearingHouse,
    string ScheduleDescription,
    string? CronExpression,
    string TimeZoneId,
    SchedulerMisfirePolicy MisfirePolicy,
    string MisfireDescription,
    DateTimeOffset? LastExecutionUtc,
    DateTimeOffset? NextExecutionUtc,
    string? LastResult,
    long? LastDurationMilliseconds,
    string? LastSchedulerInstance,
    string CurrentState,
    bool ManualExecutionEnabled,
    bool RequestsRecovery,
    bool AllowsConcurrentExecution,
    int PeriodicityType,
    int? N,
    int? Minute,
    string? TimeOfDay,
    DayOfWeek? WeeklyDay,
    int? MonthDay,
    bool OnlyBusinessDays,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    string SynchronizationStatus,
    string? LastSynchronizationError,
    string Category,
    string ProcessType,
    string? SoapService,
    bool UsesCycleSchedule,
    bool CanEditSchedule,
    IReadOnlyList<SchedulerOperationalContextDto> OperationalContexts);

/// <summary>Contexto operativo de ciclo asociado a una tarea gobernada por ventanas de compensación.</summary>
/// <param name="CycleConfigId">Identificador persistido de la configuración de ciclo.</param>
/// <param name="ClearingHouseCode">Código de la cámara propietaria del ciclo.</param>
/// <param name="ClearingHouseName">Nombre de la cámara propietaria del ciclo.</param>
/// <param name="CycleName">Nombre configurado del ciclo.</param>
/// <param name="WindowDescription">Ventana configurada expresada por el servicio como hora inicial a hora final.</param>
/// <param name="CutoffDescription">Hora de cutoff configurada para el ciclo.</param>
/// <param name="NextValidWindowUtc">Inicio de la próxima ventana operativa vigente calculada por el servicio, si existe.</param>
/// <param name="NextValidWindowEndUtc">Fin de la próxima ventana operativa vigente calculada por el servicio, si existe.</param>
/// <param name="Status">Estado calculado del contexto: cámara inactiva, ciclo inactivo, ventana abierta, sin próxima ventana vigente o programada.</param>
public sealed record SchedulerOperationalContextDto(
    int CycleConfigId,
    string ClearingHouseCode,
    string ClearingHouseName,
    string CycleName,
    string WindowDescription,
    string CutoffDescription,
    DateTimeOffset? NextValidWindowUtc,
    DateTimeOffset? NextValidWindowEndUtc,
    string Status);

/// <summary>Información técnica de la definición y del job Quartz asociados a una tarea.</summary>
/// <param name="TaskCode">Código público de tarea definido por SchedulerTaskCatalog.</param>
/// <param name="HandlerCode">Código interno de handler que el catálogo vincula con la definición persistida.</param>
/// <param name="SoapService">Servicio SOAP asociado por el catálogo cuando existe.</param>
/// <param name="JobName">Nombre de la clave Quartz del job.</param>
/// <param name="JobGroup">Grupo de la clave Quartz del job.</param>
/// <param name="CronExpression">Expresión cron persistida en la definición, cuando existe.</param>
/// <param name="TimeZoneId">Zona horaria persistida; cuando falta, el servicio expone America/Bogota.</param>
/// <param name="MisfirePolicy">Política de misfire persistida.</param>
/// <param name="RequestsRecovery">Indica si la definición solicita recuperación de Quartz.</param>
/// <param name="AllowsConcurrentExecution">Indica si la política de concurrencia persistida es AllowParallel.</param>
/// <param name="Parameters">Parámetros persistidos de la definición, indexados por clave sin distinguir mayúsculas/minúsculas.</param>
/// <param name="TriggerKeys">Claves de los triggers Quartz asociados al job, ordenadas alfabéticamente.</param>
public sealed record SchedulerTechnicalInfoDto(
    string TaskCode,
    string HandlerCode,
    string? SoapService,
    string JobName,
    string JobGroup,
    string? CronExpression,
    string TimeZoneId,
    SchedulerMisfirePolicy MisfirePolicy,
    bool RequestsRecovery,
    bool AllowsConcurrentExecution,
    IReadOnlyDictionary<string, string> Parameters,
    IReadOnlyList<string> TriggerKeys);

/// <summary>Detalle persistido de una ejecución administrada por el scheduler.</summary>
/// <param name="ExecutionId">Identificador público de la ejecución.</param>
/// <param name="TaskCode">Código público de la tarea registrada en la ejecución.</param>
/// <param name="JobName">Nombre Quartz del job registrado en la ejecución.</param>
/// <param name="JobGroup">Grupo Quartz del job registrado en la ejecución.</param>
/// <param name="TriggerName">Nombre del trigger registrado.</param>
/// <param name="TriggerType">Tipo de trigger registrado, por ejemplo el valor persistido para ejecución manual o programada.</param>
/// <param name="FireInstanceId">Identificador de disparo informado por Quartz y persistido en la ejecución.</param>
/// <param name="SchedulerInstanceId">Identificador de la instancia scheduler registrada para la ejecución.</param>
/// <param name="SchedulerInstanceName">Nombre de la instancia scheduler registrada para la ejecución.</param>
/// <param name="RequestedByUserId">Identificador del usuario solicitante cuando la ejecución fue iniciada por un usuario.</param>
/// <param name="RequestedByUserName">Nombre del usuario solicitante cuando está disponible.</param>
/// <param name="RequestReason">Motivo de la solicitud manual cuando existe.</param>
/// <param name="RequestId">Identificador de solicitud persistido cuando existe.</param>
/// <param name="CorrelationId">Identificador de correlación persistido para la ejecución.</param>
/// <param name="ScheduledFireTimeUtc">Instante programado registrado para el disparo.</param>
/// <param name="ActualFireTimeUtc">Instante real de disparo cuando está disponible.</param>
/// <param name="StartedAtUtc">Instante persistido de inicio de ejecución.</param>
/// <param name="FinishedAtUtc">Instante persistido de finalización cuando la ejecución terminó.</param>
/// <param name="DurationMilliseconds">Duración persistida en milisegundos cuando está disponible.</param>
/// <param name="Status">Estado persistido de la ejecución.</param>
/// <param name="IsRecovery">Indica si la ejecución fue marcada como recuperación.</param>
/// <param name="RefireCount">Cantidad de redisparos registrada para la ejecución.</param>
/// <param name="MisfireDetected">Indica si se detectó un misfire para esta ejecución.</param>
/// <param name="ResultSummary">Resumen de salida persistido cuando existe.</param>
/// <param name="ErrorCode">Código de error persistido cuando existe.</param>
/// <param name="ErrorSummary">Mensaje o resumen de error persistido cuando existe.</param>
/// <param name="OriginalFireInstanceId">Identificador del disparo original asociado a una recuperación, cuando existe.</param>
/// <param name="RecoveredByInstanceId">Instancia que ejecutó la recuperación, cuando existe.</param>
/// <param name="RecoveryStartedAtUtc">Instante en que comenzó la recuperación, cuando existe.</param>
/// <param name="RecoveryResult">Resultado persistido de la recuperación, cuando existe.</param>
public sealed record SchedulerExecutionDto(
    Guid ExecutionId,
    string TaskCode,
    string JobName,
    string JobGroup,
    string TriggerName,
    string TriggerType,
    string FireInstanceId,
    string SchedulerInstanceId,
    string SchedulerInstanceName,
    string? RequestedByUserId,
    string? RequestedByUserName,
    string? RequestReason,
    string? RequestId,
    string CorrelationId,
    DateTimeOffset ScheduledFireTimeUtc,
    DateTimeOffset? ActualFireTimeUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    long? DurationMilliseconds,
    SchedulerExecutionStatus Status,
    bool IsRecovery,
    int RefireCount,
    bool MisfireDetected,
    string? ResultSummary,
    string? ErrorCode,
    string? ErrorSummary,
    string? OriginalFireInstanceId,
    string? RecoveredByInstanceId,
    DateTimeOffset? RecoveryStartedAtUtc,
    string? RecoveryResult);

/// <summary>Estado observado de una instancia registrada del scheduler.</summary>
/// <param name="InstanceId">Identificador persistido de la instancia.</param>
/// <param name="InstanceName">Nombre persistido de la instancia.</param>
/// <param name="HostName">Host persistido para la instancia.</param>
/// <param name="StartedAtUtc">Instante persistido de inicio de la instancia.</param>
/// <param name="LastHeartbeatUtc">Instante del último heartbeat persistido.</param>
/// <param name="Status">Estado calculado por el servicio a partir del registro y los umbrales configurados.</param>
/// <param name="IsCurrentInstance">Indica si InstanceId coincide con la instancia Quartz activa que atiende la consulta.</param>
/// <param name="CurrentlyExecutingJobs">Cantidad persistida de trabajos actualmente en ejecución para la instancia.</param>
/// <param name="Version">Versión persistida de la instancia.</param>
public sealed record SchedulerInstanceDto(
    string InstanceId,
    string InstanceName,
    string HostName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastHeartbeatUtc,
    string Status,
    bool IsCurrentInstance,
    int CurrentlyExecutingJobs,
    string Version);

/// <summary>Resultado paginado usado por las consultas de historial del scheduler.</summary>
/// <typeparam name="T">Tipo de elemento contenido en la página.</typeparam>
/// <param name="Items">Elementos de la página actual.</param>
/// <param name="Page">Número de página efectivo después de normalizar el valor solicitado a mínimo 1.</param>
/// <param name="PageSize">Tamaño de página efectivo limitado por el servicio al rango 1..100.</param>
/// <param name="Total">Cantidad total de ejecuciones que cumplen los filtros antes de paginar.</param>
public sealed record SchedulerPagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

/// <summary>Filtros disponibles para consultar el historial persistido de ejecuciones del scheduler.</summary>
public sealed class SchedulerHistoryQuery
{
    /// <summary>Número de página solicitado; el servicio usa 1 cuando el valor es menor que 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Tamaño de página solicitado; el servicio lo limita al rango de 1 a 100.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>Filtra por estado persistido de la ejecución cuando se informa.</summary>
    public SchedulerExecutionStatus? Status { get; init; }

    /// <summary>Incluye ejecuciones cuyo StartedAt sea mayor o igual al instante indicado.</summary>
    public DateTimeOffset? FromUtc { get; init; }

    /// <summary>Incluye ejecuciones cuyo StartedAt sea menor o igual al instante indicado.</summary>
    public DateTimeOffset? ToUtc { get; init; }

    /// <summary>Filtra por coincidencia exacta con el TriggerType persistido cuando se informa.</summary>
    public string? TriggerType { get; init; }

    /// <summary>Filtra por coincidencia exacta con el SchedulerInstanceId persistido cuando se informa.</summary>
    public string? InstanceId { get; init; }

    /// <summary>Filtra por coincidencia exacta con el RequestedByUserName persistido cuando se informa.</summary>
    public string? UserName { get; init; }

    /// <summary>Filtra por coincidencia exacta con el CorrelationId persistido cuando se informa.</summary>
    public string? CorrelationId { get; init; }
}

/// <summary>Solicitud para disparar manualmente una tarea administrada por el scheduler.</summary>
/// <param name="Reason">Motivo obligatorio; el controller exige entre 10 y 500 caracteres después de recortar espacios.</param>
/// <param name="RequestId">Identificador de solicitud obligatorio y distinto de Guid.Empty; el servicio lo usa para detectar solicitudes ya registradas.</param>
public sealed record ExecuteSchedulerTaskRequest(string Reason, Guid RequestId);

public sealed record ExecuteSchedulerTaskCommand(
    string TaskCode,
    string Reason,
    Guid RequestId,
    string? UserId,
    string UserName,
    string CorrelationId);

/// <summary>Resultado lógico de una solicitud de ejecución manual.</summary>
public enum ManualExecutionOutcome
{
    /// <summary>La solicitud fue registrada y se solicitó el disparo del job en Quartz.</summary>
    Accepted,

    /// <summary>RequestId ya estaba registrado y se devuelve la ejecución previamente asociada.</summary>
    Duplicate,

    /// <summary>Existe una ejecución activa incompatible con la política de concurrencia.</summary>
    Conflict,

    /// <summary>La solicitud se registró como rechazada por una regla operativa de la tarea.</summary>
    Rejected,

    /// <summary>El código no está autorizado por el catálogo o no existe la definición persistida.</summary>
    NotFound
}

/// <summary>Respuesta a una solicitud de ejecución manual.</summary>
/// <param name="Outcome">Resultado lógico que el controller traduce al status HTTP correspondiente.</param>
/// <param name="ExecutionId">Identificador de la ejecución creada o de la ejecución duplicada ya registrada, cuando existe.</param>
/// <param name="Message">Mensaje funcional producido por el servicio para el resultado.</param>
/// <param name="ActiveExecutionId">Identificador de la ejecución activa que provoca un conflicto de concurrencia, cuando se conoce.</param>
public sealed record ManualExecutionResult(
    ManualExecutionOutcome Outcome,
    Guid? ExecutionId,
    string Message,
    Guid? ActiveExecutionId = null);

/// <summary>Programación solicitada para actualizar o previsualizar una tarea del scheduler.</summary>
/// <param name="PeriodicityType">Valor numérico de PeriodicityTypeEnum. El servicio exige que corresponda a un valor definido del enum.</param>
/// <param name="N">Intervalo usado por EveryNMinutes; cuando esa periodicidad aplica debe estar entre 1 y 1440.</param>
/// <param name="Minute">Minuto usado por HourlyAtMinute; cuando esa periodicidad aplica debe estar entre 0 y 59.</param>
/// <param name="TimeOfDay">Hora interpretable por TimeOnly para periodicidades DailyAtTime, Weekly o Monthly.</param>
/// <param name="WeeklyDay">Día requerido cuando PeriodicityType corresponde a Weekly.</param>
/// <param name="MonthDay">Día del mes requerido por Monthly; debe estar entre 1 y 31.</param>
/// <param name="CronExpression">Expresión requerida por la periodicidad Cron y validada mediante Quartz CronExpression.IsValidExpression.</param>
/// <param name="TimeZoneId">Identificador de zona horaria que debe ser resoluble mediante TimeZoneInfo.FindSystemTimeZoneById.</param>
/// <param name="MisfirePolicy">Política de misfire que debe corresponder a un valor definido de SchedulerMisfirePolicy.</param>
/// <param name="OnlyBusinessDays">Cuando es true, la actualización persiste CalendarPolicyEnum.OnlyBusinessDays; en caso contrario persiste IgnoreCalendar.</param>
/// <param name="StartAt">Inicio opcional de vigencia. Es obligatorio para Once y Yearly y debe ser anterior a EndAt cuando ambos existen.</param>
/// <param name="EndAt">Fin opcional de vigencia; cuando también existe StartAt debe ser posterior a StartAt.</param>
public sealed record SchedulerScheduleUpdateRequest(
    int PeriodicityType,
    int? N,
    int? Minute,
    string? TimeOfDay,
    DayOfWeek? WeeklyDay,
    int? MonthDay,
    string? CronExpression,
    string TimeZoneId,
    SchedulerMisfirePolicy MisfirePolicy,
    bool OnlyBusinessDays,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt);

public sealed record SchedulerScheduleUpdateCommand(
    string TaskCode,
    SchedulerScheduleUpdateRequest Schedule,
    string? UserId,
    string UserName);

/// <summary>Previsualización calculada para una programación válida del scheduler.</summary>
/// <param name="Description">Descripción legible de la programación calculada por el servicio.</param>
/// <param name="NextExecutionsUtc">Próximas ejecuciones calculadas sin persistir cambios; el servicio solicita hasta cinco instantes y Once devuelve una sola ejecución.</param>
public sealed record SchedulerSchedulePreviewDto(string Description, IReadOnlyList<DateTimeOffset> NextExecutionsUtc);
