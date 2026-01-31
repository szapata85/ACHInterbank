using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class PermissionsService : IPermissionsService
{
    private readonly AchDbContext _dbContext;

    public PermissionsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<PermissionSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.Permissions
            .OrderBy(p => p.Name)
            .Select(p => new PermissionSummaryDto
            {
                Id = p.Id,
                Name = p.Name ?? string.Empty,
                Description = p.Description
            })
            .ToListAsync(ct);
    }
}
