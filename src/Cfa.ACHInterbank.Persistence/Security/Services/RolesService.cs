using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class RolesService : IRolesService
{
    private readonly AchDbContext _dbContext;

    public RolesService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<RoleSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .Select(r => new RoleSummaryDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Description = r.Description,
                Permissions = r.RolePermissions
                    .Select(rp => rp.Permission!.Name ?? string.Empty)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList()
            })
            .ToListAsync(ct);
    }
}
