using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase.Repositories.Navigation;

[Scoped]
public class MenuQueryRepository : IMenuQueryRepository
{
    private readonly AchDbContext _dbContext;

    public MenuQueryRepository(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItems
            .AsNoTracking()
            .Include(mi => mi.Menu)
            .Include(mi => mi.MenuItemRoles)
            .ThenInclude(mr => mr.Role)
            .Include(mi => mi.MenuItemPermissions)
            .ThenInclude(mp => mp.Permission)
            .Where(mi => mi.IsActive && (mi.Menu == null || mi.Menu.IsActive))
            .OrderBy(mi => mi.Order)
            .ToListAsync(cancellationToken);
    }
}
