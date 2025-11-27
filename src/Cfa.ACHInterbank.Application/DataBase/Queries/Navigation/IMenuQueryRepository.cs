using Cfa.ACHInterbank.Domain.Entities.Navigation;

namespace Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;

public interface IMenuQueryRepository
{
    Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default);
}
