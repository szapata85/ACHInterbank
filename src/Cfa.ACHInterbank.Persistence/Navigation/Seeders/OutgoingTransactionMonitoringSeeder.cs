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

    private readonly AchDbContext _context;

    public OutgoingTransactionMonitoringSeeder(AchDbContext context) => _context = context;

    public int Order => 4;

    public async Task SeedAsync()
    {
        var read = await UpsertPermissionAsync(ReadPermissionId, FineGrainedPermissions.OutgoingTransactions.MonitorRead,
            "Consultar el monitoreo de transacciones de salida");
        var technical = await UpsertPermissionAsync(TechnicalPermissionId, FineGrainedPermissions.OutgoingTransactions.MonitorTechnicalDetailRead,
            "Consultar información técnica autorizada del monitoreo de salidas");
        await _context.SaveChangesAsync();

        await EnsureRolePermissionAsync(RoleConfiguration.AdminRoleId, read.Id);
        await EnsureRolePermissionAsync(RoleConfiguration.OperatorRoleId, read.Id);
        await EnsureRolePermissionAsync(RoleConfiguration.AdminRoleId, technical.Id);

        var parent = await _context.MenuItems.SingleAsync(item => item.Route == "/transactions");
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
        item.Icon = "monitoring";
        item.Order = 6;
        item.Exact = true;
        item.IsActive = true;
        await _context.SaveChangesAsync();

        var candidateIds = candidates.Select(candidate => candidate.Id).Append(item.Id).Distinct().ToArray();
        var permissionLinks = await _context.MenuItemPermissions.Where(link => candidateIds.Contains(link.MenuItemId)).ToListAsync();
        var roleLinks = await _context.MenuItemRoles.Where(link => candidateIds.Contains(link.MenuItemId)).ToListAsync();
        _context.MenuItemPermissions.RemoveRange(permissionLinks);
        _context.MenuItemRoles.RemoveRange(roleLinks);
        foreach (var duplicate in candidates.Where(candidate => candidate.Id != item.Id))
        {
            duplicate.IsActive = false;
            duplicate.Route = $"/navigation/disabled/outgoing-monitoring/{duplicate.Id}";
        }

        _context.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, PermissionId = read.Id });
        _context.MenuItemRoles.AddRange(
            new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.AdminRoleId },
            new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.OperatorRoleId });
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
