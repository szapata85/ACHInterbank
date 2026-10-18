using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

[Scoped]
public sealed class AchClaimsMenuSeeder(AchDbContext context) : IDbSeeder
{
    public static readonly Guid ClaimsReadPermissionId = ClaimsPermissionSeeder.ReadPermissionId;
    public const string CanonicalRoute = "/ach-claims";
    public const string CanonicalLabel = "Reclamaciones ACH";
    public const string CanonicalIcon = "assignment_late";
    public const int CanonicalOrder = 9;

    public int Order => 4;

    public async Task SeedAsync()
    {
        var claimsRead = await context.Permissions.SingleOrDefaultAsync(permission =>
            permission.Name == FineGrainedPermissions.Claims.Read)
            ?? throw new InvalidOperationException(
                $"No se encontró el permiso requerido {FineGrainedPermissions.Claims.Read}. Ejecute primero {nameof(ClaimsPermissionSeeder)}.");

        var parent = await context.MenuItems.SingleOrDefaultAsync(item =>
            item.Id == MenuItemConfiguration.TransactionsId)
            ?? await context.MenuItems.SingleAsync(item => item.Route == "/transactions");

        var candidates = await context.MenuItems
            .Where(item => item.Route == CanonicalRoute || item.Label == CanonicalLabel)
            .OrderByDescending(item => item.Route == CanonicalRoute)
            .ThenBy(item => item.Id)
            .ToListAsync();

        var item = candidates.FirstOrDefault();
        if (item is null)
        {
            item = new MenuItem();
            context.MenuItems.Add(item);
        }

        item.MenuId = MenuConfiguration.MainMenuId;
        item.ParentId = parent.Id;
        item.Label = CanonicalLabel;
        item.Route = CanonicalRoute;
        item.Icon = CanonicalIcon;
        item.Order = CanonicalOrder;
        item.Exact = true;
        item.IsActive = true;

        foreach (var duplicate in candidates.Skip(1))
        {
            duplicate.IsActive = false;
            duplicate.Route = $"/navigation/disabled/ach-claims/{duplicate.Id}";
        }

        await context.SaveChangesAsync();

        var candidateIds = candidates.Select(candidate => candidate.Id).Append(item.Id).Distinct().ToArray();
        var links = await context.MenuItemPermissions
            .Where(link => candidateIds.Contains(link.MenuItemId))
            .ToListAsync();
        context.MenuItemPermissions.RemoveRange(links.Where(link =>
            link.MenuItemId != item.Id || link.PermissionId != claimsRead.Id));

        if (!links.Any(link => link.MenuItemId == item.Id && link.PermissionId == claimsRead.Id))
            context.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, PermissionId = claimsRead.Id });

        if (!await context.MenuItemPermissions.AnyAsync(link =>
                link.MenuItemId == parent.Id && link.PermissionId == claimsRead.Id))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = parent.Id,
                PermissionId = claimsRead.Id
            });
        }

        await context.SaveChangesAsync();
    }
}
