namespace Cfa.ACHInterbank.Application.Common;

public record PagedResponse<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
