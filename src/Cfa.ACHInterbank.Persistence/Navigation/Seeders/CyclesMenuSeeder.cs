using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class CyclesMenuSeeder : IDbSeeder
{
    private static readonly string[] DuplicateRoutes =
    [
        "/transactions/cycle-configs",
        "/cenit/operacion/ciclos"
    ];

    private readonly AchDbContext _context;

    public CyclesMenuSeeder(AchDbContext context)
    {
        _context = context;
    }

    public int Order => 2;

    public async Task SeedAsync()
    {
        var canonical = await _context.MenuItems
            .FirstOrDefaultAsync(x => x.Id == MenuItemConfiguration.AchCyclesId);

        if (canonical is null)
        {
            canonical = new MenuItem
            {
                Id = MenuItemConfiguration.AchCyclesId,
                MenuId = MenuConfiguration.MainMenuId,
                Order = 4
            };
            _context.MenuItems.Add(canonical);
        }

        canonical.Label = "Configuración de ciclos";
        canonical.Route = "/ach-cycles";
        canonical.Icon = "schedule";
        canonical.Exact = true;
        canonical.IsActive = true;

        if (!await _context.MenuItemPermissions.AnyAsync(x =>
                x.MenuItemId == MenuItemConfiguration.AchCyclesId
                && x.PermissionId == PermissionConfiguration.ReadAchPermissionId))
        {
            _context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.AchCyclesId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }

        var duplicates = await _context.MenuItems
            .Where(x => x.Id != MenuItemConfiguration.AchCyclesId && DuplicateRoutes.Contains(x.Route))
            .ToListAsync();
        foreach (var duplicate in duplicates)
        {
            duplicate.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }
}
