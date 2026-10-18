namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

/// <summary>
/// Representa una instancia operativa de ciclo ACH y la metadata calculada para su ventana actual.
/// </summary>
public class AchCycleDto
{
    /// <summary>Identificador estable de la instancia de ciclo.</summary>
    public string Id { get; set; } = null!;

    /// <summary>Nombre final del ciclo proveniente de su configuración canónica.</summary>
    public string CycleName { get; set; } = null!;

    /// <summary>Fecha operativa a la que corresponde la instancia de ciclo.</summary>
    public DateTime ProcessingDate { get; set; }

    /// <summary>Hora de inicio de la ventana operativa del ciclo.</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Hora de fin de la ventana operativa del ciclo.</summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>Hora límite de recepción configurada para el ciclo.</summary>
    public TimeSpan CutoffTime { get; set; }

    /// <summary>Hora configurada para la liberación de salidas del ciclo.</summary>
    public TimeSpan OutputReleaseTime { get; set; }

    /// <summary>Inicio opcional de la ventana configurada para la planilla de compensación.</summary>
    public TimeSpan? CompensationStatementStartTime { get; set; }

    /// <summary>Fin opcional de la ventana configurada para la planilla de compensación.</summary>
    public TimeSpan? CompensationStatementEndTime { get; set; }

    /// <summary>Inicio opcional de la ventana configurada para el pago de compensación.</summary>
    public TimeSpan? CompensationPaymentStartTime { get; set; }

    /// <summary>Fin opcional de la ventana configurada para el pago de compensación.</summary>
    public TimeSpan? CompensationPaymentEndTime { get; set; }

    /// <summary>Inicio opcional de la ventana configurada para liberación de salidas.</summary>
    public TimeSpan? OutputReleaseStartTime { get; set; }

    /// <summary>Fin opcional de la ventana configurada para liberación de salidas.</summary>
    public TimeSpan? OutputReleaseEndTime { get; set; }

    /// <summary>Indica si el ciclo está configurado para reprogramarse cuando la fecha corresponda a un festivo.</summary>
    public bool RescheduleOnHoliday { get; set; }

    /// <summary>Identificador de la cámara compensadora a la que pertenece el ciclo.</summary>
    public int ClearingHouseId { get; set; }

    /// <summary>Identificador de la configuración canónica de ciclo que originó esta instancia, cuando está vinculada.</summary>
    public int? ClearingHouseCycleConfigId { get; set; }

    /// <summary>Nombre de la cámara compensadora asociada, cuando está disponible.</summary>
    public string? ClearingHouseName { get; set; }

    /// <summary>Estado operativo calculado para la ventana actual del ciclo.</summary>
    public string OperationalStatus { get; set; } = string.Empty;

    /// <summary>Indica si el instante actual se encuentra dentro de la ventana operativa calculada del ciclo.</summary>
    public bool AcceptsTransactions { get; set; }

    /// <summary>Indica si el nombre del ciclo lo identifica como ciclo de reproceso o contingencia según la lógica actual.</summary>
    public bool IsContingencyCycle { get; set; }

    /// <summary>Representación legible de la ventana local calculada y su zona horaria.</summary>
    public string WindowLabel { get; set; } = string.Empty;
}

/// <summary>
/// Define los datos usados para crear o actualizar una instancia operativa de ciclo y resolver su configuración canónica.
/// </summary>
public class AchCycleRequest
{
    /// <summary>Nombre usado como criterio de resolución cuando se necesita desambiguar una configuración; el nombre final persistido proviene de la configuración canónica.</summary>
    public string CycleName { get; set; } = null!;

    /// <summary>Fecha operativa para la que se resolverá la configuración del ciclo.</summary>
    public DateTime ProcessingDate { get; set; }

    /// <summary>Hora de inicio usada para buscar una configuración coincidente cuando no se selecciona una configuración explícita.</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Hora de fin usada para buscar una configuración coincidente cuando no se selecciona una configuración explícita.</summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>Hora de corte usada para buscar una configuración coincidente cuando no se selecciona una configuración explícita.</summary>
    public TimeSpan CutoffTime { get; set; }

    /// <summary>Indica si la instancia debe conservar la configuración de reprogramación por festivos.</summary>
    public bool RescheduleOnHoliday { get; set; }

    /// <summary>Identificador de la cámara compensadora para la que se resolverá el ciclo.</summary>
    public int ClearingHouseId { get; set; }

    /// <summary>Configuración canónica de ciclo seleccionada explícitamente; si se omite, el servicio intenta resolver una coincidencia única.</summary>
    public int? ClearingHouseCycleConfigId { get; set; }
}

/// <summary>
/// Resume un ciclo candidato a exportación y el estado de sus artefactos de salida.
/// </summary>
public class AchCycleExportDto
{
    /// <summary>Identificador de la instancia de ciclo.</summary>
    public string Id { get; set; } = null!;

    /// <summary>Identificador del ciclo usado por los procesos de exportación.</summary>
    public string CycleId { get; set; } = null!;

    /// <summary>Nombre del ciclo.</summary>
    public string CycleName { get; set; } = null!;

    /// <summary>ProcessingDate de la instancia de ciclo.</summary>
    public DateTime ProcessingDate { get; set; }

    /// <summary>Identificador de la cámara compensadora del ciclo.</summary>
    public int ClearingHouseId { get; set; }

    /// <summary>Nombre de la cámara compensadora, cuando está disponible.</summary>
    public string? ClearingHouseName { get; set; }

    /// <summary>Cantidad de lotes asociados al ciclo.</summary>
    public int BatchCount { get; set; }

    /// <summary>Cantidad de transacciones del ciclo que se encuentran en estados elegibles para exportación.</summary>
    public int TransactionCount { get; set; }

    /// <summary>Indica si el ciclo tiene lotes y transacciones en estado exportable.</summary>
    public bool IsExportable { get; set; } = true;

    /// <summary>Motivo por el que el ciclo no está disponible para exportación, cuando aplica.</summary>
    public string? ExportUnavailableReason { get; set; }

    /// <summary>Identificador utilizado por el flujo de exportación; actualmente coincide con el identificador del ciclo.</summary>
    public string? ExportIdentifier { get; set; }

    /// <summary>Indica si existe un archivo generado no cifrado para el ciclo.</summary>
    public bool HasGeneratedFile { get; set; }

    /// <summary>Indica si existe un sobre digital cifrado generado para el ciclo.</summary>
    public bool HasDigitalEnvelope { get; set; }

    /// <summary>Nombre del archivo generado más reciente, cuando existe.</summary>
    public string? FileName { get; set; }

    /// <summary>Fecha y hora UTC del archivo generado no cifrado más reciente, cuando existe.</summary>
    public DateTime? GeneratedAtUtc { get; set; }

    /// <summary>Fecha y hora UTC del sobre digital cifrado más reciente, cuando existe.</summary>
    public DateTime? ProtectedAtUtc { get; set; }
}

/// <summary>
/// Describe el resultado de intentar vincular un ciclo histórico con una configuración canónica.
/// </summary>
/// <param name="CycleId">Identificador del ciclo inspeccionado.</param>
/// <param name="ClearingHouseCycleConfigId">Identificador de la configuración resuelta, cuando existe una coincidencia única.</param>
/// <param name="Status">Estado de la decisión de vínculo: Unique, Ambiguous o Unmatched según la lógica actual.</param>
/// <param name="Detail">Detalle técnico de la decisión tomada para el ciclo.</param>
public sealed record AchCycleConfigurationLinkRepairItem(
    string CycleId,
    int? ClearingHouseCycleConfigId,
    string Status,
    string Detail);

/// <summary>
/// Resume una ejecución de reparación de vínculos entre ciclos históricos y configuraciones canónicas.
/// </summary>
/// <param name="Completed">Indica si la reparación pudo completarse sin asociaciones ambiguas.</param>
/// <param name="InspectedCount">Cantidad de ciclos sin vínculo de configuración que fueron inspeccionados.</param>
/// <param name="RepairedCount">Cantidad de ciclos vinculados a una configuración única.</param>
/// <param name="AmbiguousCount">Cantidad de ciclos con más de una configuración histórica candidata.</param>
/// <param name="UnmatchedCount">Cantidad de ciclos para los que no se encontró una configuración histórica aplicable.</param>
/// <param name="Items">Detalle de los ciclos reparados, ambiguos o sin coincidencia.</param>
public sealed record AchCycleConfigurationLinkRepairResult(
    bool Completed,
    int InspectedCount,
    int RepairedCount,
    int AmbiguousCount,
    int UnmatchedCount,
    IReadOnlyList<AchCycleConfigurationLinkRepairItem> Items);
