using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly AchDbContext _dbContext;

    public RolesController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleSummaryDto>>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
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
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }
}

public record RoleSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IEnumerable<string> Permissions { get; init; } = Enumerable.Empty<string>();
}
