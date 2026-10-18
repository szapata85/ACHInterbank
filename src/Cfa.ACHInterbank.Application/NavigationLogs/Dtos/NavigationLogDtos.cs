namespace Cfa.ACHInterbank.Application.NavigationLogs.Dtos;

/// <summary>Registro de navegación proyectado para consulta.</summary>
public record NavigationLogDto
{
    /// <summary>Identificador del registro de navegación.</summary>
    public Guid Id { get; init; }

    /// <summary>Identificador persistido del usuario, cuando está disponible.</summary>
    public string? UserId { get; init; }

    /// <summary>Nombre de usuario o nombre completo proyectado desde el usuario relacionado, cuando está disponible.</summary>
    public string? Username { get; init; }

    /// <summary>Ruta de navegación persistida.</summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>Fecha y hora persistida de la visita.</summary>
    public DateTime VisitedAt { get; init; }

    /// <summary>Identificador de sesión recibido para la navegación, cuando existe.</summary>
    public string? SessionId { get; init; }

    /// <summary>Duración registrada de la navegación en milisegundos, cuando existe.</summary>
    public int? DurationMs { get; init; }

    /// <summary>Dirección IP capturada para la solicitud, cuando está disponible.</summary>
    public string? IpAddress { get; init; }

    /// <summary>User-Agent capturado para la solicitud, cuando está disponible.</summary>
    public string? UserAgent { get; init; }
}

/// <summary>Datos recibidos para registrar una navegación.</summary>
public record NavigationLogCreate
{
    /// <summary>Ruta visitada. El servicio rechaza una ruta vacía después de normalizarla.</summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>Fecha y hora opcional de la visita; el servicio la convierte a UTC o usa la hora UTC actual cuando no se informa.</summary>
    public DateTime? VisitedAt { get; init; }

    /// <summary>Identificador opcional de sesión; el servicio elimina espacios exteriores y limita su longitud a 100 caracteres.</summary>
    public string? SessionId { get; init; }

    /// <summary>Duración opcional en milisegundos; solo se conserva cuando está entre 1 y 86400000.</summary>
    public int? DurationMs { get; init; }
}

/// <summary>Parámetros internos utilizados por el servicio para consultar registros de navegación.</summary>
public record NavigationLogQuery
{
    /// <summary>Límite inferior opcional de VisitedAt.</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>Límite superior opcional de VisitedAt.</summary>
    public DateTime? EndDate { get; init; }

    /// <summary>Texto opcional buscado sobre UserId y, por relación, Username o FullName.</summary>
    public string? UserId { get; init; }

    /// <summary>Filtro de Username disponible para consumidores internos del servicio; no es un query parameter del endpoint SC-410.</summary>
    public string? Username { get; init; }

    /// <summary>Texto opcional buscado por contenido en la ruta persistida.</summary>
    public string? Route { get; init; }

    /// <summary>Número de página solicitado.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Tamaño de página solicitado.</summary>
    public int PageSize { get; init; } = 50;
}
