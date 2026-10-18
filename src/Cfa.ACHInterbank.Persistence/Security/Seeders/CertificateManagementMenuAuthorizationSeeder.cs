using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// Reconciles only Certificados digitales so its menu-entry permission alternatives mirror
/// P1.CertificatesRead: Certificates.Read OR CanManageCertificates OR CanReadAch.
/// Administrative actions remain separately protected by their backend policies and SPA gates.
/// </summary>
[Scoped]
public sealed class CertificateManagementMenuAuthorizationSeeder(AchDbContext context) : IDbSeeder
{
    public const int AuthorizationReconciliationOrder = 106;

    public int Order => AuthorizationReconciliationOrder;

    public async Task SeedAsync()
    {
        var menuItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Id == MenuItemConfiguration.NachaCertificatesId);

        if (menuItem is null
            || !string.Equals(menuItem.Route, "/nacha-security/certificates", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Canonical Certificados digitales MenuItem must exist before its authorization is reconciled.");
        }

        var fineRead = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == CertificateManagementPermissionSeeder.ReadPermissionId);
        if (fineRead is null
            || !string.Equals(fineRead.Name, FineGrainedPermissions.Certificates.Read, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Certificates.Read must be seeded before Certificados digitales menu authorization is reconciled.");
        }

        var manage = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ManageCertificatesPermissionId);
        if (manage is null
            || !string.Equals(manage.Name, FineGrainedPermissions.CanManageCertificates, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanManageCertificates must exist before Certificados digitales menu authorization is reconciled.");
        }

        var legacyRead = await context.Permissions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == PermissionConfiguration.ReadAchPermissionId);
        if (legacyRead is null
            || !string.Equals(legacyRead.Name, "CanReadAch", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CanReadAch must exist before Certificados digitales menu authorization is reconciled.");
        }

        var allowedPermissionIds = new HashSet<Guid>
        {
            CertificateManagementPermissionSeeder.ReadPermissionId,
            PermissionConfiguration.ManageCertificatesPermissionId,
            PermissionConfiguration.ReadAchPermissionId
        };

        var permissionLinks = await context.MenuItemPermissions
            .Where(link => link.MenuItemId == menuItem.Id)
            .ToListAsync();

        context.MenuItemPermissions.RemoveRange(
            permissionLinks.Where(link => !allowedPermissionIds.Contains(link.PermissionId)));

        foreach (var permissionId in allowedPermissionIds)
        {
            if (!permissionLinks.Any(link => link.PermissionId == permissionId))
            {
                context.MenuItemPermissions.Add(new MenuItemPermission
                {
                    MenuItemId = menuItem.Id,
                    PermissionId = permissionId
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
