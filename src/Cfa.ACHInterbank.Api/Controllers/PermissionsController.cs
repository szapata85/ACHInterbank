using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly AchDbContext _dbContext;

    public PermissionsController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionSummaryDto>>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions
            .OrderBy(p => p.Name)
            .Select(p => new PermissionSummaryDto
            {
                Id = p.Id,
                Name = p.Name ?? string.Empty,
                Description = p.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(permissions);
    }
}

public record PermissionSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
