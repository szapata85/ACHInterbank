using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class ReportsMenuSeeder : IDbSeeder
{
    public const string Route = "/reports";

    private readonly AchDbContext _context;

    public ReportsMenuSeeder(AchDbContext context)
    {
        _context = context;
    }

    public int Order => 4;

    public async Task SeedAsync()
    {
        var reports = await _context.MenuItems
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(item => item.Route == Route);

        if (reports is null)
        {
            reports = new MenuItem();
            _context.MenuItems.Add(reports);
        }

        reports.MenuId = MenuConfiguration.MainMenuId;
        reports.ParentId = null;
        reports.Label = "Reportes";
        reports.Route = Route;
        reports.Icon = "analytics";
        reports.Order = 7;
        reports.Exact = false;
        reports.IsActive = true;

        await _context.SaveChangesAsync();

        var permissionLinks = await _context.MenuItemPermissions
            .Where(link => link.MenuItemId == reports.Id)
            .ToListAsync();
        _context.MenuItemPermissions.RemoveRange(permissionLinks.Where(
            link => link.PermissionId != PermissionConfiguration.ReadAchPermissionId));
        if (!permissionLinks.Any(link => link.PermissionId == PermissionConfiguration.ReadAchPermissionId))
        {
            _context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = reports.Id,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }

        var roleLinks = await _context.MenuItemRoles
            .Where(link => link.MenuItemId == reports.Id)
            .ToListAsync();
        _context.MenuItemRoles.RemoveRange(roleLinks.Where(link =>
            link.RoleId != RoleConfiguration.AdminRoleId
            && link.RoleId != RoleConfiguration.OperatorRoleId));
        foreach (var roleId in new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId })
        {
            if (!roleLinks.Any(link => link.RoleId == roleId))
            {
                _context.MenuItemRoles.Add(new MenuItemRole
                {
                    MenuItemId = reports.Id,
                    RoleId = roleId
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
