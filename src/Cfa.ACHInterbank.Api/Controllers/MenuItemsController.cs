using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("navigation/menu-items")]
[Authorize(Roles = "Admin")]
public class MenuItemsController : ControllerBase
{
    private readonly AchDbContext _dbContext;

    public MenuItemsController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemAdminDto>>> GetMenuItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.MenuItems
            .Include(mi => mi.MenuItemRoles)
            .Include(mi => mi.MenuItemPermissions)
            .AsNoTracking()
            .OrderBy(mi => mi.Order)
            .ToListAsync(cancellationToken);

        var lookup = items.ToDictionary(mi => mi.Id, Map);
        var roots = new List<MenuItemAdminDto>();

        foreach (var item in items)
        {
            var dto = lookup[item.Id];
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
            {
                parent.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        SortChildren(roots);
        return Ok(roots);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost]
    public async Task<ActionResult<MenuItemAdminDto>> CreateMenuItemAsync([FromBody] SaveMenuItemRequest request, CancellationToken cancellationToken)
    {
        var menuItem = new MenuItem
        {
            MenuId = MenuConfiguration.MainMenuId,
            ParentId = request.ParentId,
            Label = request.Label,
            Route = request.Route,
            Icon = request.Icon,
            Order = request.Order,
            Exact = request.Exact,
            IsActive = request.IsActive
        };

        if (request.ParentId.HasValue)
        {
            var exists = await _dbContext.MenuItems.AnyAsync(mi => mi.Id == request.ParentId.Value, cancellationToken);
            if (!exists)
            {
                return BadRequest($"No existe un elemento padre con el Id {request.ParentId}.");
            }
        }

        await _dbContext.MenuItems.AddAsync(menuItem, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await UpdateRelationsAsync(menuItem, request.RoleIds, request.PermissionIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var created = await GetMenuItemAsync(menuItem.Id, cancellationToken);
        return CreatedAtAction(nameof(GetMenuItemsAsync), created);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MenuItemAdminDto>> UpdateMenuItemAsync(int id, [FromBody] SaveMenuItemRequest request, CancellationToken cancellationToken)
    {
        var menuItem = await _dbContext.MenuItems
            .Include(mi => mi.MenuItemRoles)
            .Include(mi => mi.MenuItemPermissions)
            .FirstOrDefaultAsync(mi => mi.Id == id, cancellationToken);

        if (menuItem is null)
        {
            return NotFound();
        }

        if (request.ParentId == id)
        {
            return BadRequest("Un elemento no puede ser su propio padre.");
        }

        if (request.ParentId.HasValue)
        {
            var exists = await _dbContext.MenuItems.AnyAsync(mi => mi.Id == request.ParentId.Value, cancellationToken);
            if (!exists)
            {
                return BadRequest($"No existe un elemento padre con el Id {request.ParentId}.");
            }
        }

        menuItem.ParentId = request.ParentId;
        menuItem.Label = request.Label;
        menuItem.Route = request.Route;
        menuItem.Icon = request.Icon;
        menuItem.Order = request.Order;
        menuItem.Exact = request.Exact;
        menuItem.IsActive = request.IsActive;

        await UpdateRelationsAsync(menuItem, request.RoleIds, request.PermissionIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await GetMenuItemAsync(menuItem.Id, cancellationToken);
        return Ok(updated);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMenuItemAsync(int id, CancellationToken cancellationToken)
    {
        var hasChildren = await _dbContext.MenuItems.AnyAsync(mi => mi.ParentId == id, cancellationToken);
        if (hasChildren)
        {
            return Conflict("Elimine o reasigne los hijos antes de borrar este elemento.");
        }

        var menuItem = await _dbContext.MenuItems.FirstOrDefaultAsync(mi => mi.Id == id, cancellationToken);
        if (menuItem is null)
        {
            return NotFound();
        }

        _dbContext.MenuItems.Remove(menuItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<MenuItemAdminDto> GetMenuItemAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.MenuItems
            .Include(mi => mi.MenuItemRoles)
            .Include(mi => mi.MenuItemPermissions)
            .AsNoTracking()
            .FirstAsync(mi => mi.Id == id, cancellationToken);

        return Map(entity);
    }

    private async Task UpdateRelationsAsync(MenuItem menuItem, IEnumerable<Guid> roleIds, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
    {
        _dbContext.MenuItemRoles.RemoveRange(menuItem.MenuItemRoles);
        _dbContext.MenuItemPermissions.RemoveRange(menuItem.MenuItemPermissions);

        await _dbContext.SaveChangesAsync(cancellationToken);

        menuItem.MenuItemRoles = roleIds
            .Distinct()
            .Select(roleId => new MenuItemRole { MenuItemId = menuItem.Id, RoleId = roleId })
            .ToList();

        menuItem.MenuItemPermissions = permissionIds
            .Distinct()
            .Select(permissionId => new MenuItemPermission { MenuItemId = menuItem.Id, PermissionId = permissionId })
            .ToList();
    }

    private static MenuItemAdminDto Map(MenuItem item) => new()
    {
        Id = item.Id,
        ParentId = item.ParentId,
        Label = item.Label,
        Route = item.Route,
        Icon = item.Icon,
        Order = item.Order,
        Exact = item.Exact,
        IsActive = item.IsActive,
        RoleIds = item.MenuItemRoles.Select(r => r.RoleId).ToList(),
        PermissionIds = item.MenuItemPermissions.Select(p => p.PermissionId).ToList(),
        Children = new List<MenuItemAdminDto>()
    };

    private static void SortChildren(IList<MenuItemAdminDto> items)
    {
        foreach (var item in items.OrderBy(i => i.Order).ToList())
        {
            if (item.Children.Any())
            {
                var sorted = item.Children.OrderBy(child => child.Order).ToList();
                item.Children.Clear();
                foreach (var child in sorted)
                {
                    item.Children.Add(child);
                }
                SortChildren(item.Children);
            }
        }
    }
}

public record MenuItemAdminDto
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int Order { get; init; }
    public bool Exact { get; init; }
    public bool IsActive { get; init; }
    public IList<Guid> RoleIds { get; init; } = new List<Guid>();
    public IList<Guid> PermissionIds { get; init; } = new List<Guid>();
    public IList<MenuItemAdminDto> Children { get; init; } = new List<MenuItemAdminDto>();
}

public record SaveMenuItemRequest
{
    public string Label { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int Order { get; init; }
    public bool Exact { get; init; }
    public bool IsActive { get; init; }
    public int? ParentId { get; init; }
    public IEnumerable<Guid> RoleIds { get; init; } = Enumerable.Empty<Guid>();
    public IEnumerable<Guid> PermissionIds { get; init; } = Enumerable.Empty<Guid>();
}
