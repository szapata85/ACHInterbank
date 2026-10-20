using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class OutgoingTransactionMonitoringSeeder : IDbSeeder
{
    public static readonly Guid ReadPermissionId = Guid.Parse("a0420002-2f85-4ca5-a100-000000000001");
    public static readonly Guid TechnicalPermissionId = Guid.Parse("a0420002-2f85-4ca5-a100-000000000002");
    public const string CanonicalRoute = "/transactions/outgoing-monitoring";
    public const string CanonicalLabel = "Transacciones de salida";
    public const string CanonicalIcon = "monitoring";
    public const int CanonicalOrder = 6;
    public const string CanonicalDashboardLabel = "Panel principal";

    private const string ParentRoute = "/transactions";
    private const string DisabledDuplicateRoutePrefix = "/navigation/disabled/outgoing-monitoring";

    private readonly AchDbContext _context;

    public OutgoingTransactionMonitoringSeeder(AchDbContext context) => _context = context;

    public int Order => 4;

    public async Task SeedAsync()
    {
        var dashboard = await _context.MenuItems.SingleOrDefaultAsync(item => item.Route == "/dashboard");
        if (dashboard is not null)
            dashboard.Label = CanonicalDashboardLabel;

        var read = await UpsertPermissionAsync(ReadPermissionId, FineGrainedPermissions.OutgoingTransactions.MonitorRead,
            "Consultar el monitoreo de transacciones de salida");
        var technical = await UpsertPermissionAsync(TechnicalPermissionId, FineGrainedPermissions.OutgoingTransactions.MonitorTechnicalDetailRead,
            "Consultar información técnica autorizada del monitoreo de salidas");
        await _context.SaveChangesAsync();

        await EnsureRolePermissionAsync(RoleConfiguration.AdminRoleId, read.Id);
        await EnsureRolePermissionAsync(RoleConfiguration.OperatorRoleId, read.Id);
        await EnsureRolePermissionAsync(RoleConfiguration.AdminRoleId, technical.Id);

        var parent = await _context.MenuItems.SingleOrDefaultAsync(item => item.Route == ParentRoute)
            ?? throw new InvalidOperationException($"No se encontró el grupo de navegación requerido en la ruta {ParentRoute}.");
        var candidates = await _context.MenuItems
            .Where(item => item.Route == CanonicalRoute || item.Label == CanonicalLabel)
            .OrderByDescending(item => item.Route == CanonicalRoute)
            .ThenBy(item => item.Id)
            .ToListAsync();
        var item = candidates.FirstOrDefault();
        if (item is null)
        {
            item = new MenuItem();
            _context.MenuItems.Add(item);
        }

        item.MenuId = MenuConfiguration.MainMenuId;
        item.ParentId = parent.Id;
        item.Label = CanonicalLabel;
        item.Route = CanonicalRoute;
        item.Icon = CanonicalIcon;
        item.Order = CanonicalOrder;
        item.Exact = true;
        item.IsActive = true;
        await _context.SaveChangesAsync();

        var candidateIds = candidates.Select(candidate => candidate.Id).Append(item.Id).Distinct().ToArray();
        var permissionLinks = await _context.MenuItemPermissions
            .Where(link => candidateIds.Contains(link.MenuItemId))
            .ToListAsync();
        var roleLinks = await _context.MenuItemRoles
            .Where(link => candidateIds.Contains(link.MenuItemId))
            .ToListAsync();
        foreach (var duplicate in candidates.Where(candidate => candidate.Id != item.Id))
        {
            duplicate.IsActive = false;
            duplicate.Route = $"{DisabledDuplicateRoutePrefix}/{duplicate.Id}";
        }

        _context.MenuItemPermissions.RemoveRange(permissionLinks.Where(link =>
            link.MenuItemId != item.Id || link.PermissionId != read.Id));
        _context.MenuItemRoles.RemoveRange(roleLinks.Where(link =>
            link.MenuItemId != item.Id
            || (link.RoleId != RoleConfiguration.AdminRoleId && link.RoleId != RoleConfiguration.OperatorRoleId)));

        if (!permissionLinks.Any(link => link.MenuItemId == item.Id && link.PermissionId == read.Id))
            _context.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, PermissionId = read.Id });
        if (!roleLinks.Any(link => link.MenuItemId == item.Id && link.RoleId == RoleConfiguration.AdminRoleId))
            _context.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.AdminRoleId });
        if (!roleLinks.Any(link => link.MenuItemId == item.Id && link.RoleId == RoleConfiguration.OperatorRoleId))
            _context.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.OperatorRoleId });
        await _context.SaveChangesAsync();
    }

    private async Task<Permission> UpsertPermissionAsync(Guid id, string name, string description)
    {
        var permission = await _context.Permissions.FirstOrDefaultAsync(item => item.Name == name)
            ?? await _context.Permissions.FindAsync(id);
        if (permission is null)
        {
            permission = new Permission { Id = id };
            _context.Permissions.Add(permission);
        }
        permission.Name = name;
        permission.Description = description;
        return permission;
    }

    private async Task EnsureRolePermissionAsync(Guid roleId, Guid permissionId)
    {
        if (!await _context.RolePermissions.AnyAsync(item => item.RoleId == roleId && item.PermissionId == permissionId))
            _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
    }
}
