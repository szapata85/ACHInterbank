namespace Cfa.ACHInterbank.Application.Common;

/// <summary>Respuesta paginada utilizada por consultas de la aplicación.</summary>
/// <typeparam name="T">Tipo de cada elemento de la página.</typeparam>
public record PagedResponse<T>
{
    /// <summary>Elementos incluidos en la página efectiva.</summary>
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();

    /// <summary>Total de elementos que cumplen los filtros antes de paginar.</summary>
    public int Total { get; init; }

    /// <summary>Número de página efectivo.</summary>
    public int Page { get; init; }

    /// <summary>Tamaño de página efectivo.</summary>
    public int PageSize { get; init; }
}
