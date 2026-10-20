namespace Cfa.ACHInterbank.Domain.Models.Configurations;

/// <summary>
/// Define los parámetros comunes de paginación utilizados por consultas administrativas.
/// </summary>
public class PaginationRequest
{
    private const int DefaultPageSize = 20;
    private const int DefaultPage = 1;
    private const int MaxPageSize = 100;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Número de página solicitado. Los valores menores que 1 se normalizan a la página 1.
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? DefaultPage : value;
    }

    /// <summary>
    /// Cantidad de elementos por página. Los valores menores que 1 usan 20 y los mayores que 100 se limitan a 100.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value < 1)
            {
                _pageSize = DefaultPageSize;
                return;
            }

            _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
    }
}
