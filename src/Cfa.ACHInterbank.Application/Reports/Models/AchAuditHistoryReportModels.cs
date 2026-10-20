using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.Reports.Models;

/// <summary>Filtros internos del reporte de auditoría ACH.</summary>
public sealed class AchAuditReportFilter
{
    /// <summary>Texto opcional buscado en el usuario que registró el cambio.</summary>
    public string? User { get; init; }
    /// <summary>Texto opcional buscado en la acción auditada.</summary>
    public string? Action { get; init; }
    /// <summary>Texto opcional buscado en el nombre de la entidad auditada.</summary>
    public string? Entity { get; init; }
    /// <summary>Inicio UTC opcional e inclusivo del intervalo de auditoría.</summary>
    public DateTime? FromUtc { get; init; }
    /// <summary>Fin UTC opcional e inclusivo del intervalo de auditoría.</summary>
    public DateTime? ToUtc { get; init; }
    /// <summary>Número de página solicitado.</summary>
    public int Page { get; init; } = 1;
    /// <summary>Tamaño de página solicitado.</summary>
    public int PageSize { get; init; } = 25;
}

/// <summary>Registro individual del reporte de auditoría ACH.</summary>
public sealed class AchAuditReportRowDto
{
    /// <summary>Usuario que registró el cambio.</summary>
    public string User { get; init; } = string.Empty;
    /// <summary>Acción auditada.</summary>
    public string Action { get; init; } = string.Empty;
    /// <summary>Nombre de la entidad auditada.</summary>
    public string Entity { get; init; } = string.Empty;
    /// <summary>Identificador textual de la entidad auditada.</summary>
    public string EntityId { get; init; } = string.Empty;
    /// <summary>Fecha y hora UTC del cambio.</summary>
    public DateTime DateUtc { get; init; }
}

/// <summary>Respuesta paginada del reporte de auditoría ACH.</summary>
public sealed class AchAuditReportResponseDto
{
    /// <summary>Registros de auditoría de la página efectiva.</summary>
    public IReadOnlyList<AchAuditReportRowDto> Items { get; init; } = [];
    /// <summary>Número de página efectivo.</summary>
    public int Page { get; init; }
    /// <summary>Tamaño de página efectivo.</summary>
    public int PageSize { get; init; }
    /// <summary>Total de registros que cumplen los filtros antes de paginar.</summary>
    public int Total { get; init; }
}

/// <summary>Filtros internos del histórico de cambios de estado ACH.</summary>
public sealed class AchHistoryReportFilter
{
    /// <summary>Inicio UTC opcional e inclusivo del intervalo.</summary>
    public DateTime? FromUtc { get; init; }
    /// <summary>Fin UTC opcional e inclusivo del intervalo.</summary>
    public DateTime? ToUtc { get; init; }
    /// <summary>Identificador opcional de transacción ACH.</summary>
    public int? TransactionId { get; init; }
    /// <summary>Estado destino opcional del evento.</summary>
    public AchTransferStateEnum? ToState { get; init; }
    /// <summary>Origen opcional del evento de cambio de estado.</summary>
    public AchStateEventSourceEnum? Source { get; init; }
    /// <summary>Número de página solicitado.</summary>
    public int Page { get; init; } = 1;
    /// <summary>Tamaño de página solicitado.</summary>
    public int PageSize { get; init; } = 25;
}

/// <summary>Evento individual del histórico de cambios de estado ACH.</summary>
public sealed class AchHistoryReportRowDto
{
    /// <summary>Identificador de la transacción ACH.</summary>
    public int TransactionId { get; init; }
    /// <summary>Estado previo registrado en el evento.</summary>
    public AchTransferStateEnum FromState { get; init; }
    /// <summary>Estado destino registrado en el evento.</summary>
    public AchTransferStateEnum ToState { get; init; }
    /// <summary>Origen registrado para el cambio de estado.</summary>
    public AchStateEventSourceEnum Source { get; init; }
    /// <summary>Código de razón del evento cuando existe.</summary>
    public string? ReasonCode { get; init; }
    /// <summary>Fecha y hora UTC del evento.</summary>
    public DateTime DateUtc { get; init; }
    /// <summary>Usuario que realizó el cambio cuando el origen de datos lo expone; la implementación actual del histórico devuelve null.</summary>
    public string? ChangedBy { get; init; }
}

/// <summary>Respuesta paginada del histórico de cambios de estado ACH.</summary>
public sealed class AchHistoryReportResponseDto
{
    /// <summary>Eventos de la página efectiva.</summary>
    public IReadOnlyList<AchHistoryReportRowDto> Items { get; init; } = [];
    /// <summary>Número de página efectivo.</summary>
    public int Page { get; init; }
    /// <summary>Tamaño de página efectivo.</summary>
    public int PageSize { get; init; }
    /// <summary>Total de eventos que cumplen los filtros antes de paginar.</summary>
    public int Total { get; init; }
}
