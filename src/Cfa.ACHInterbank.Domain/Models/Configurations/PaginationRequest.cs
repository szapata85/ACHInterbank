namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public class PaginationRequest
{
    private const int DefaultPageSize = 20;
    private const int DefaultPage = 1;
    private const int MaxPageSize = 100;

    private int _page = DefaultPage;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? DefaultPage : value;
    }

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
