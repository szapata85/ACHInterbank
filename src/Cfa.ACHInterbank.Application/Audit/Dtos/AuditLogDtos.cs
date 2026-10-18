namespace Cfa.ACHInterbank.Application.Audit.Dtos;

/// <summary>Registro de auditoría proyectado para consulta.</summary>
public record AuditLogDto
{
    /// <summary>Identificador del registro de auditoría.</summary>
    public Guid Id { get; init; }

    /// <summary>Nombre de la entidad sobre la que se registró el cambio.</summary>
    public string EntityName { get; init; } = string.Empty;

    /// <summary>Identificador persistido de la entidad afectada.</summary>
    public string EntityId { get; init; } = string.Empty;

    /// <summary>Acción registrada para el cambio auditado.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Identificador o nombre proyectado del actor que realizó el cambio.</summary>
    public string ChangedBy { get; init; } = string.Empty;

    /// <summary>Fecha y hora persistida del cambio.</summary>
    public DateTime ChangedAt { get; init; }

    /// <summary>Representación persistida de los campos modificados, cuando existe.</summary>
    public string? ChangedFields { get; init; }
}

/// <summary>Parámetros internos utilizados por el servicio para consultar registros de auditoría.</summary>
public record AuditLogQuery
{
    /// <summary>Límite inferior opcional de ChangedAt.</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>Límite superior opcional de ChangedAt.</summary>
    public DateTime? EndDate { get; init; }

    /// <summary>Texto opcional buscado por contenido en ChangedBy.</summary>
    public string? ChangedBy { get; init; }

    /// <summary>Acción opcional comparada por igualdad sin distinguir mayúsculas.</summary>
    public string? Action { get; init; }

    /// <summary>Número de página solicitado.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Tamaño de página solicitado.</summary>
    public int PageSize { get; init; } = 50;
}
