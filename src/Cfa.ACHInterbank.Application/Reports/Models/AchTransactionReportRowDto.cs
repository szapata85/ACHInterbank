using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

/// <summary>Fila del reporte de transacciones ACH enviadas o recibidas.</summary>
public sealed class AchTransactionReportRowDto
{
    /// <summary>Identificador interno de la transacción.</summary>
    public int TransactionId { get; init; }
    /// <summary>Fecha efectiva de la transacción.</summary>
    public DateTime EffectiveEntryDate { get; init; }
    /// <summary>Identificador externo de la transacción.</summary>
    public string TransactionExternalId { get; init; } = string.Empty;
    /// <summary>Referencia de negocio de la transacción.</summary>
    public string Reference { get; init; } = string.Empty;
    /// <summary>Valor monetario registrado para la transacción.</summary>
    public decimal Amount { get; init; }
    /// <summary>Tipo de transacción ACH.</summary>
    public TransactionTypeEnum TransactionType { get; init; }
    /// <summary>Estado ACH actual de la transacción.</summary>
    public AchTransferStateEnum State { get; init; }
    /// <summary>Nombre de la cámara asociada al ciclo.</summary>
    public string ClearingHouseName { get; init; } = string.Empty;
    /// <summary>Identificador del ciclo ACH.</summary>
    public string AchCycleId { get; init; } = string.Empty;
    /// <summary>Nombre del ciclo ACH.</summary>
    public string AchCycleName { get; init; } = string.Empty;
    /// <summary>Identificador del lote ACH.</summary>
    public int BatchId { get; init; }
    /// <summary>Número secuencial del lote ACH.</summary>
    public int BatchSequenceNumber { get; init; }
    /// <summary>Nombre de la institución origen.</summary>
    public string SourceBankName { get; init; } = string.Empty;
    /// <summary>Nombre de la institución destino.</summary>
    public string DestinationBankName { get; init; } = string.Empty;
    /// <summary>Nombre del archivo NACHA más reciente asociado al ciclo y cámara de la transacción, o guion cuando no existe.</summary>
    public string NachaFileName { get; init; } = string.Empty;
}

/// <summary>Totales agregados del conjunto filtrado de transacciones.</summary>
public sealed class AchTransactionReportTotalsDto
{
    /// <summary>Cantidad total de registros que cumplen los filtros, antes de paginar.</summary>
    public int TotalRecords { get; init; }
    /// <summary>Suma de valores de transacciones tipo Credit dentro del conjunto filtrado.</summary>
    public decimal TotalCreditAmount { get; init; }
    /// <summary>Suma de valores de transacciones tipo Debit dentro del conjunto filtrado.</summary>
    public decimal TotalDebitAmount { get; init; }
}

/// <summary>Respuesta paginada del reporte de transacciones ACH.</summary>
public sealed class AchTransactionReportResponseDto
{
    /// <summary>Filas de la página efectiva solicitada.</summary>
    public IReadOnlyList<AchTransactionReportRowDto> Items { get; init; } = [];
    /// <summary>Totales calculados sobre el conjunto filtrado completo.</summary>
    public AchTransactionReportTotalsDto Totals { get; init; } = new();
    /// <summary>Número de página efectivo después de normalizar el valor solicitado.</summary>
    public int Page { get; init; }
    /// <summary>Tamaño de página efectivo después de aplicar la normalización y el máximo de 200.</summary>
    public int PageSize { get; init; }
    /// <summary>Cantidad total de registros del conjunto filtrado antes de paginar.</summary>
    public int Total { get; init; }
}
