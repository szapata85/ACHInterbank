using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

/// <summary>Filtro interno utilizado por el generador del PDF de trazabilidad ACH.</summary>
public sealed record TraceabilityReportFilter
{
    /// <summary>Inicio UTC opcional del rango normalizado por el controller.</summary>
    public DateTime? FromUtc { get; init; }
    /// <summary>Fin UTC opcional del rango normalizado por el controller.</summary>
    public DateTime? ToUtc { get; init; }
    /// <summary>Estado ACH opcional.</summary>
    public AchTransferStateEnum? State { get; init; }
    /// <summary>Identificador opcional del ciclo ACH.</summary>
    public string? AchCycleId { get; init; }
}
