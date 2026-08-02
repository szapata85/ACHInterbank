using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class IncomingNachaCommandCenterMenuSeeder : IDbSeeder
{
    public const string CanonicalLabel = "Seguimiento de archivos NACHA-M";
    public const string CanonicalRoute = "/incoming-nacha-command-center";
    public const string CanonicalIcon = "manage_search";
    public const int CanonicalOrder = 7;

    private const string TransactionsRoute = "/transactions";
    private const string DisabledDuplicateRoutePrefix = "/navigation/disabled/incoming-nacha-command-center";
    private static readonly string[] LegacyLabels =
    [
        "Command Center inbound NACHA",
        "Command Center Inbound NACHA-M",
        "Inbound NACHA",
        "Monitor inbound",
        "Operational Dashboard",
        "Dashboard inbound",
        "Seguimiento inbound",
        "Centro de ingestas"
    ];

    private readonly AchDbContext _context;

    public IncomingNachaCommandCenterMenuSeeder(AchDbContext context)
    {
        _context = context;
    }

    public int Order => 3;

    public async Task SeedAsync()
    {
        var parent = await _context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == TransactionsRoute)
            ?? throw new InvalidOperationException(
                $"No se encontró el grupo operativo requerido en la ruta {TransactionsRoute}.");

        var candidates = await _context.MenuItems
            .Where(item => item.Route == CanonicalRoute || LegacyLabels.Contains(item.Label))
            .OrderByDescending(item => item.Route == CanonicalRoute)
            .ThenByDescending(item => item.IsActive)
            .ThenBy(item => item.Id)
            .ToListAsync();

        var canonical = candidates.FirstOrDefault();
        if (canonical is null)
        {
            canonical = new MenuItem();
            _context.MenuItems.Add(canonical);
        }

        canonical.MenuId = MenuConfiguration.MainMenuId;
        canonical.ParentId = parent.Id;
        canonical.Label = CanonicalLabel;
        canonical.Route = CanonicalRoute;
        canonical.Icon = CanonicalIcon;
        canonical.Order = CanonicalOrder;
        canonical.Exact = true;
        canonical.IsActive = true;

        await _context.SaveChangesAsync();

        var duplicates = candidates.Where(item => item.Id != canonical.Id).ToList();
        foreach (var duplicate in duplicates)
        {
            duplicate.IsActive = false;
            if (string.Equals(duplicate.Route, CanonicalRoute, StringComparison.OrdinalIgnoreCase))
            {
                duplicate.Route = $"{DisabledDuplicateRoutePrefix}/{duplicate.Id}";
            }
        }

        var candidateIds = candidates.Select(item => item.Id).Append(canonical.Id).Distinct().ToArray();
        var existingPermissions = await _context.MenuItemPermissions
            .Where(link => candidateIds.Contains(link.MenuItemId))
            .ToListAsync();
        var existingRoles = await _context.MenuItemRoles
            .Where(link => candidateIds.Contains(link.MenuItemId))
            .ToListAsync();
        _context.MenuItemPermissions.RemoveRange(existingPermissions);
        _context.MenuItemRoles.RemoveRange(existingRoles);

        _context.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = canonical.Id,
            PermissionId = PermissionConfiguration.ReadAchPermissionId
        });
        _context.MenuItemRoles.AddRange(
            new MenuItemRole
            {
                MenuItemId = canonical.Id,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = canonical.Id,
                RoleId = RoleConfiguration.OperatorRoleId
            });

        await _context.SaveChangesAsync();
    }
}
