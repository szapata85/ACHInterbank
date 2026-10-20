namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchNachaFileReportFilter
{
    public DateTime? Date { get; init; }
    public int? ClearingHouseId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>Fila del reporte de archivos NACHA exportados.</summary>
public sealed class AchNachaFileReportRowDto
{
    /// <summary>Nombre persistido del archivo exportado.</summary>
    public string FileName { get; init; } = string.Empty;
    /// <summary>Instante UTC en que se generó el archivo.</summary>
    public DateTime GeneratedAtUtc { get; init; }
    /// <summary>Nombre de la cámara asociada al archivo.</summary>
    public string ClearingHouseName { get; init; } = string.Empty;
    /// <summary>Tipo de exportación persistido para el archivo.</summary>
    public string ExportKind { get; init; } = string.Empty;
    /// <summary>Total de registros informado por la exportación.</summary>
    public int TotalRecords { get; init; }
    /// <summary>Total de transacciones informado por la exportación.</summary>
    public int TotalTransactions { get; init; }
}

/// <summary>Totales agregados del conjunto filtrado de archivos NACHA.</summary>
public sealed class AchNachaFileReportTotalsDto
{
    /// <summary>Cantidad total de archivos filtrados.</summary>
    public int TotalFiles { get; init; }
    /// <summary>Suma de TotalRecords de todos los archivos filtrados.</summary>
    public int TotalRecords { get; init; }
    /// <summary>Suma de TotalTransactions de todos los archivos filtrados.</summary>
    public int TotalTransactions { get; init; }
}

/// <summary>Respuesta paginada del reporte de archivos NACHA.</summary>
public sealed class AchNachaFileReportResponseDto
{
    /// <summary>Archivos de la página efectiva solicitada.</summary>
    public IReadOnlyList<AchNachaFileReportRowDto> Items { get; init; } = [];
    /// <summary>Totales calculados sobre el conjunto filtrado completo.</summary>
    public AchNachaFileReportTotalsDto Totals { get; init; } = new();
    /// <summary>Número de página efectivo después de normalizar el valor solicitado.</summary>
    public int Page { get; init; }
    /// <summary>Tamaño de página efectivo después de aplicar la normalización y el máximo de 200.</summary>
    public int PageSize { get; init; }
    /// <summary>Cantidad total de archivos del conjunto filtrado antes de paginar.</summary>
    public int Total { get; init; }
}

public sealed class AchCycleReportFilter
{
    public DateTime? Date { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? Name { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>Fila del reporte de ciclos ACH.</summary>
public sealed class AchCycleReportRowDto
{
    /// <summary>Identificador del ciclo ACH.</summary>
    public string CycleId { get; init; } = string.Empty;
    /// <summary>Nombre del ciclo ACH.</summary>
    public string CycleName { get; init; } = string.Empty;
    /// <summary>Fecha de procesamiento del ciclo.</summary>
    public DateTime ProcessingDate { get; init; }
    /// <summary>Hora de inicio configurada para el ciclo.</summary>
    public TimeSpan StartTime { get; init; }
    /// <summary>Hora de finalización configurada para el ciclo.</summary>
    public TimeSpan EndTime { get; init; }
    /// <summary>Hora de corte configurada para el ciclo.</summary>
    public TimeSpan CutoffTime { get; init; }
    /// <summary>Representación HH:mm de StartTime y EndTime separadas por guion.</summary>
    public string Schedule => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    /// <summary>Estado derivado en tiempo de consulta: Programado, En ejecución o Cerrado.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Nombre de la cámara asociada al ciclo.</summary>
    public string ClearingHouseName { get; init; } = string.Empty;
    /// <summary>Cantidad de transacciones asociadas al ciclo.</summary>
    public int TotalTransactions { get; set; }
    /// <summary>Suma de Amount de las transacciones asociadas al ciclo.</summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>Totales agregados de los ciclos filtrados.</summary>
public sealed class AchCycleReportTotalsDto
{
    /// <summary>Cantidad total de ciclos filtrados.</summary>
    public int TotalCycles { get; init; }
    /// <summary>Cantidad total de transacciones asociadas a todos los ciclos filtrados.</summary>
    public int TotalTransactions { get; set; }
    /// <summary>Suma total de Amount de las transacciones asociadas a todos los ciclos filtrados.</summary>
    public decimal TotalAmount { get; set; }
}

/// <summary>Respuesta paginada del reporte de ciclos ACH.</summary>
public sealed class AchCycleReportResponseDto
{
    /// <summary>Ciclos de la página efectiva solicitada.</summary>
    public IReadOnlyList<AchCycleReportRowDto> Items { get; init; } = [];
    /// <summary>Totales calculados sobre todos los ciclos filtrados.</summary>
    public AchCycleReportTotalsDto Totals { get; init; } = new();
    /// <summary>Número de página efectivo después de normalizar el valor solicitado.</summary>
    public int Page { get; init; }
    /// <summary>Tamaño de página efectivo después de aplicar la normalización y el máximo de 200.</summary>
    public int PageSize { get; init; }
    /// <summary>Cantidad total de ciclos del conjunto filtrado antes de paginar.</summary>
    public int Total { get; init; }
}
