using Cfa.ACHInterbank.Application.Navigation.Dtos;
using Cfa.ACHInterbank.Application.Navigation.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Services;

[Scoped]
public class MenuItemsService : IMenuItemsService
{
    private readonly AchDbContext _dbContext;

    public MenuItemsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<MenuItemAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _dbContext.MenuItems
            .Include(mi => mi.MenuItemRoles)
            .Include(mi => mi.MenuItemPermissions)
            .AsNoTracking()
            .OrderBy(mi => mi.Order)
            .ToListAsync(ct);

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
        return roots;
    }

    public async Task<MenuItemAdminDto> CreateAsync(SaveMenuItemRequest request, CancellationToken ct = default)
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
            var exists = await _dbContext.MenuItems.AnyAsync(mi => mi.Id == request.ParentId.Value, ct);
            if (!exists)
            {
                throw new ArgumentException($"No existe un elemento padre con el Id {request.ParentId}.");
            }
        }

        await _dbContext.MenuItems.AddAsync(menuItem, ct);
        await _dbContext.SaveChangesAsync(ct);

        await UpdateRelationsAsync(menuItem, request.RoleIds, request.PermissionIds, ct);
        await _dbContext.SaveChangesAsync(ct);

        return await GetMenuItemAsync(menuItem.Id, ct);
    }

    public async Task<MenuItemAdminDto?> UpdateAsync(int id, SaveMenuItemRequest request, CancellationToken ct = default)
    {
        var menuItem = await _dbContext.MenuItems
            .Include(mi => mi.MenuItemRoles)
            .Include(mi => mi.MenuItemPermissions)
            .FirstOrDefaultAsync(mi => mi.Id == id, ct);

        if (menuItem is null)
        {
            return null;
        }

        if (request.ParentId == id)
        {
            throw new ArgumentException("Un elemento no puede ser su propio padre.");
        }

        if (request.ParentId.HasValue)
        {
            var exists = await _dbContext.MenuItems.AnyAsync(mi => mi.Id == request.ParentId.Value, ct);
            if (!exists)
            {
                throw new ArgumentException($"No existe un elemento padre con el Id {request.ParentId}.");
            }
        }

        menuItem.ParentId = request.ParentId;
        menuItem.Label = request.Label;
        menuItem.Route = request.Route;
        menuItem.Icon = request.Icon;
        menuItem.Order = request.Order;
        menuItem.Exact = request.Exact;
        menuItem.IsActive = request.IsActive;

        await UpdateRelationsAsync(menuItem, request.RoleIds, request.PermissionIds, ct);
        await _dbContext.SaveChangesAsync(ct);

        return await GetMenuItemAsync(menuItem.Id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var hasChildren = await _dbContext.MenuItems.AnyAsync(mi => mi.ParentId == id, ct);
        if (hasChildren)
        {
            throw new InvalidOperationException("Elimine o reasigne los hijos antes de borrar este elemento.");
        }

        var menuItem = await _dbContext.MenuItems.FirstOrDefaultAsync(mi => mi.Id == id, ct);
        if (menuItem is null)
        {
            return false;
        }

        _dbContext.MenuItems.Remove(menuItem);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private async Task<MenuItemAdminDto> GetMenuItemAsync(int id, CancellationToken ct)
    {
        var entity = await _dbContext.MenuItems
            .Include(mi => mi.MenuItemRoles)
            .Include(mi => mi.MenuItemPermissions)
            .AsNoTracking()
            .FirstAsync(mi => mi.Id == id, ct);

        return Map(entity);
    }

    private async Task UpdateRelationsAsync(MenuItem menuItem, IEnumerable<Guid> roleIds, IEnumerable<Guid> permissionIds, CancellationToken ct)
    {
        _dbContext.MenuItemRoles.RemoveRange(menuItem.MenuItemRoles);
        _dbContext.MenuItemPermissions.RemoveRange(menuItem.MenuItemPermissions);

        await _dbContext.SaveChangesAsync(ct);

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
