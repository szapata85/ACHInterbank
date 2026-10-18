namespace Cfa.ACHInterbank.Application.Reports.Models;

/// <summary>Filtros internos utilizados para obtener el reporte de conciliación ACH.</summary>
public sealed class AchReconciliationReportFilter
{
    /// <summary>Fecha opcional comparada con la fecha efectiva de entrada de la transacción.</summary>
    public DateTime? Date { get; init; }
    /// <summary>Identificador opcional de la cámara compensadora asociada al ciclo ACH.</summary>
    public int? ClearingHouseId { get; init; }
    /// <summary>Identificador opcional del ciclo ACH.</summary>
    public string? AchCycleId { get; init; }
}

/// <summary>Totales agregados del reporte de conciliación ACH.</summary>
public sealed class AchReconciliationTotalsDto
{
    /// <summary>Cantidad de transacciones consideradas enviadas.</summary>
    public int SentCount { get; init; }
    /// <summary>Monto total de transacciones consideradas enviadas.</summary>
    public decimal SentAmount { get; init; }
    /// <summary>Cantidad de transacciones en estado Certified o AppliedTacitly.</summary>
    public int ReceivedCount { get; init; }
    /// <summary>Monto total de transacciones en estado Certified o AppliedTacitly.</summary>
    public decimal ReceivedAmount { get; init; }
    /// <summary>Cantidad de transacciones en estado ReturnedByOperator o ReturnedByEpr.</summary>
    public int ReturnedCount { get; init; }
    /// <summary>Monto total de transacciones en estado ReturnedByOperator o ReturnedByEpr.</summary>
    public decimal ReturnedAmount { get; init; }
}

/// <summary>Diferencias aritméticas entre los totales enviados, recibidos y devueltos.</summary>
public sealed class AchReconciliationDifferencesDto
{
    /// <summary>Diferencia de cantidad entre enviados y recibidos.</summary>
    public int SentVsReceivedCountDiff { get; init; }
    /// <summary>Diferencia de monto entre enviados y recibidos.</summary>
    public decimal SentVsReceivedAmountDiff { get; init; }
    /// <summary>Diferencia de cantidad entre enviados y devueltos.</summary>
    public int SentVsReturnedCountDiff { get; init; }
    /// <summary>Diferencia de monto entre enviados y devueltos.</summary>
    public decimal SentVsReturnedAmountDiff { get; init; }
    /// <summary>Diferencia de cantidad entre recibidos y devueltos.</summary>
    public int ReceivedVsReturnedCountDiff { get; init; }
    /// <summary>Diferencia de monto entre recibidos y devueltos.</summary>
    public decimal ReceivedVsReturnedAmountDiff { get; init; }
}

/// <summary>Inconsistencia detectada durante la conciliación.</summary>
public sealed class AchReconciliationInconsistencyDto
{
    /// <summary>Código estable de la inconsistencia detectada por el servicio.</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Descripción de la inconsistencia.</summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>Cantidad de transacciones afectadas por la inconsistencia.</summary>
    public int AffectedCount { get; init; }
}

/// <summary>Respuesta del reporte de conciliación ACH.</summary>
public sealed class AchReconciliationReportResponseDto
{
    /// <summary>Totales agregados para el alcance filtrado.</summary>
    public AchReconciliationTotalsDto Totals { get; init; } = new();
    /// <summary>Diferencias calculadas entre los totales agregados.</summary>
    public AchReconciliationDifferencesDto Differences { get; init; } = new();
    /// <summary>Inconsistencias detectadas por las reglas implementadas del reporte.</summary>
    public IReadOnlyList<AchReconciliationInconsistencyDto> Inconsistencies { get; init; } = [];
}
