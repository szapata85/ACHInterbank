using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchReturnRejectionReportFilter
{
    public DateTime? Date { get; init; }
    public string? Causal { get; init; }
    public int? ClearingHouseId { get; init; }
    public AchTransferStateEnum? State { get; init; }
    public string? Reference { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>Fila de un reporte de devoluciones o rechazos ACH.</summary>
public sealed class AchReturnRejectionReportRowDto
{
    /// <summary>Identificador interno de la transacción.</summary>
    public int TransactionId { get; init; }
    /// <summary>Fecha efectiva de la transacción.</summary>
    public DateTime EffectiveEntryDate { get; init; }
    /// <summary>Identificador externo de la transacción.</summary>
    public string TransactionExternalId { get; init; } = string.Empty;
    /// <summary>Referencia de negocio de la transacción.</summary>
    public string Reference { get; init; } = string.Empty;
    /// <summary>Valor monetario de la transacción.</summary>
    public decimal Amount { get; init; }
    /// <summary>Estado ACH actual de la transacción.</summary>
    public AchTransferStateEnum State { get; init; }
    /// <summary>Código de causal almacenado en ReturnReasonCode.</summary>
    public string CausalCode { get; init; } = string.Empty;
    /// <summary>Descripción de la causal obtenida del catálogo ReturnReasons; vacío cuando no existe coincidencia.</summary>
    public string CausalDescription { get; init; } = string.Empty;
    /// <summary>Nombre de la cámara asociada al ciclo.</summary>
    public string ClearingHouseName { get; init; } = string.Empty;
    /// <summary>Identificador del ciclo ACH.</summary>
    public string AchCycleId { get; init; } = string.Empty;
    /// <summary>Nombre del ciclo ACH.</summary>
    public string AchCycleName { get; init; } = string.Empty;
    /// <summary>Referencia de traza de la transacción original.</summary>
    public string OriginalTraceRef { get; init; } = string.Empty;
    /// <summary>Identificador de la transacción original encontrada por TraceNumber, cuando existe.</summary>
    public int? OriginalTransactionId { get; init; }
    /// <summary>Referencia de la transacción original encontrada por TraceNumber, cuando existe.</summary>
    public string? OriginalTransactionReference { get; init; }
}

/// <summary>Totales agregados del conjunto filtrado de devoluciones o rechazos.</summary>
public sealed class AchReturnRejectionReportTotalsDto
{
    /// <summary>Cantidad total de registros que cumplen los filtros, antes de paginar.</summary>
    public int TotalRecords { get; init; }
    /// <summary>Suma de Amount de los registros que cumplen los filtros.</summary>
    public decimal TotalAmount { get; init; }
}

/// <summary>Respuesta paginada de devoluciones o rechazos ACH.</summary>
public sealed class AchReturnRejectionReportResponseDto
{
    /// <summary>Filas de la página efectiva solicitada.</summary>
    public IReadOnlyList<AchReturnRejectionReportRowDto> Items { get; init; } = [];
    /// <summary>Totales calculados sobre el conjunto filtrado completo.</summary>
    public AchReturnRejectionReportTotalsDto Totals { get; init; } = new();
    /// <summary>Número de página efectivo después de normalizar el valor solicitado.</summary>
    public int Page { get; init; }
    /// <summary>Tamaño de página efectivo después de aplicar la normalización y el máximo de 200.</summary>
    public int PageSize { get; init; }
    /// <summary>Cantidad total de registros del conjunto filtrado antes de paginar.</summary>
    public int Total { get; init; }
}
