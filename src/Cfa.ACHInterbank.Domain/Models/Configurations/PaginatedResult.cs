namespace Cfa.ACHInterbank.Domain.Models.Configurations;

/// <summary>Resultado paginado con metadatos de navegación calculados a partir de la página, el tamaño y el total.</summary>
/// <typeparam name="T">Tipo de elemento incluido en la página.</typeparam>
public class PaginatedResult<T>
{
    /// <summary>Elementos incluidos en la página actual.</summary>
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    /// <summary>Número de página efectivo.</summary>
    public int Page { get; set; }
    /// <summary>Tamaño de página efectivo.</summary>
    public int PageSize { get; set; }
    /// <summary>Cantidad total de elementos que cumplen la consulta antes de paginar.</summary>
    public int TotalCount { get; set; }
    /// <summary>Cantidad total de páginas calculada a partir de TotalCount y PageSize; devuelve 0 cuando PageSize es 0.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    /// <summary>Indica si existe una página posterior según Page, PageSize y TotalCount.</summary>
    public bool HasNext => Page * PageSize < TotalCount;
    /// <summary>Indica si la página efectiva es posterior a la primera.</summary>
    public bool HasPrevious => Page > 1;
}
