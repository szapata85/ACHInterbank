using Cfa.ACHInterbank.Application.Reports.Export.Models;

namespace Cfa.ACHInterbank.Application.Reports.Export.Interfaces;

public interface IAccountingReviewExportAppService
{
    Task<AccountingReviewExportResult> ExportAsync(AccountingReviewExportApiRequest request, CancellationToken cancellationToken);
}

/// <summary>Solicitud HTTP para generar la exportación de revisión operativa contra terceros.</summary>
public sealed class AccountingReviewExportApiRequest
{
    /// <summary>Formato solicitado: pdf, csv, excel o xlsx. El valor se recorta y compara sin distinguir mayúsculas; el valor predeterminado es pdf.</summary>
    public string Format { get; init; } = "pdf";
    /// <summary>Fecha inicial opcional utilizada por los servicios de datos que alimentan el reporte.</summary>
    public DateTime? DateFrom { get; init; }
    /// <summary>Fecha final opcional utilizada por los servicios de datos que alimentan el reporte.</summary>
    public DateTime? DateTo { get; init; }
    /// <summary>Identificador opcional de cámara compensadora.</summary>
    public int? ClearingHouseId { get; init; }
    /// <summary>Código opcional de cámara incluido en el alcance descriptivo del reporte.</summary>
    public string? ClearingHouseCode { get; init; }
    /// <summary>Identificador opcional de ciclo; se usa para alimentar consultas de transacciones y conciliación.</summary>
    public string? CycleId { get; init; }
    /// <summary>Nombre opcional de ciclo incluido en el alcance descriptivo del reporte.</summary>
    public string? CycleName { get; init; }
    /// <summary>Identificador opcional de archivo incluido en el alcance descriptivo del reporte.</summary>
    public string? FileId { get; init; }
    /// <summary>Nombre opcional de archivo; la implementación actual también lo usa como referencia al consultar transacciones y devoluciones.</summary>
    public string? FileName { get; init; }
    /// <summary>Hash opcional aceptado por el contrato; la implementación actual no lo propaga al alcance construido por el servicio.</summary>
    public string? FileHash { get; init; }
    /// <summary>Identificador opcional de transacción incluido en el alcance descriptivo del reporte.</summary>
    public int? TransactionId { get; init; }
    /// <summary>Estado opcional incluido en el alcance descriptivo del reporte.</summary>
    public string? Status { get; init; }
    /// <summary>Código de causal opcional usado al consultar devoluciones.</summary>
    public string? CauseCode { get; init; }
    /// <summary>Indica si se incorporan filas derivadas de transacciones enviadas.</summary>
    public bool IncludeOutbound { get; init; } = true;
    /// <summary>Indica si se incorporan filas derivadas de transacciones recibidas.</summary>
    public bool IncludeIncoming { get; init; } = true;
    /// <summary>Indica si se incorporan filas derivadas de devoluciones.</summary>
    public bool IncludeReturns { get; init; } = true;
    /// <summary>Indica si, dentro de las devoluciones recuperadas, se incorporan filas correlacionadas como devolución de devolución.</summary>
    public bool IncludeReturnOfReturn { get; init; } = true;
    /// <summary>Indicador de alcance para huérfanos propagado al modelo de reporte.</summary>
    public bool IncludeOrphans { get; init; } = true;
    /// <summary>Indica si se agrega la fila de evidencia para revisión manual sin aplicación contable.</summary>
    public bool IncludeManualAuditOnly { get; init; } = true;
    /// <summary>Indicador de alcance de neteo propagado al reporte; cuando es false la implementación agrega una advertencia.</summary>
    public bool IncludeNetting { get; init; } = true;
    /// <summary>Indicador de alcance de liquidez propagado al reporte; cuando es false la implementación agrega una advertencia.</summary>
    public bool IncludeLiquidity { get; init; } = true;
    /// <summary>Indica si se solicita evidencia CUD; actualmente se representa como boundary operacional y puede generar advertencias.</summary>
    public bool IncludeCudEvidence { get; init; } = true;
    /// <summary>Indica si la exportación incluye la sección de filas.</summary>
    public bool IncludeRows { get; init; } = true;
    /// <summary>Indica si la exportación incluye la sección de diferencias.</summary>
    public bool IncludeDifferences { get; init; } = true;
    /// <summary>Indica si la exportación incluye referencias de evidencia.</summary>
    public bool IncludeEvidence { get; init; } = true;
    /// <summary>Indica si la exportación incluye las banderas de frontera no contable.</summary>
    public bool IncludeBoundaryFlags { get; init; } = true;
    /// <summary>Indica si la exportación incluye advertencias.</summary>
    public bool IncludeWarnings { get; init; } = true;
    /// <summary>Indica si la exportación incluye el resumen.</summary>
    public bool IncludeSummary { get; init; } = true;
    /// <summary>Indica si la exportación incluye el alcance solicitado.</summary>
    public bool IncludeScope { get; init; } = true;
    /// <summary>Identidad declarativa del solicitante; valores vacíos se normalizan a sistema.</summary>
    public string RequestedBy { get; init; } = "sistema";
    /// <summary>Identificador opcional de correlación propagado al alcance del reporte.</summary>
    public string? CorrelationId { get; init; }
    /// <summary>Delimitador usado por la exportación CSV; si llega vacío, el exportador usa punto y coma.</summary>
    public string CsvDelimiter { get; init; } = ";";
}
