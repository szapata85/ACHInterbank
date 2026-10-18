using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// NAV3.5A backfill for canonical seeded roles. Adds fine-grained RolePermission rows only
/// where the seeded role already had the same effective backend capability through a legacy
/// compatibility permission. Legacy policy fallbacks and navigation role gates remain intact.
/// </summary>
[Scoped]
public sealed class FineGrainedGrantBackfillSeeder(AchDbContext context) : IDbSeeder
{
    public const int BackfillOrder = 119;

    public static IReadOnlyList<string> AdminBackfillCapabilities { get; } =
    [
        FineGrainedPermissions.Transactions.Create,
        FineGrainedPermissions.Nacha.Upload,
        FineGrainedPermissions.Returns.Read,
        FineGrainedPermissions.Returns.GenerateFile,
        FineGrainedPermissions.BulkIngestion.Read,
        FineGrainedPermissions.BulkIngestion.Upload,
        FineGrainedPermissions.BulkIngestion.Retry,
        FineGrainedPermissions.BulkIngestion.Cancel,
        FineGrainedPermissions.Certificates.Read,
        FineGrainedPermissions.Certificates.UploadPublic,
        FineGrainedPermissions.Certificates.RegisterPrivate,
        FineGrainedPermissions.Certificates.Revoke,
        FineGrainedPermissions.DigitalEnvelope.Encrypt,
        FineGrainedPermissions.DigitalEnvelope.Decrypt,
        FineGrainedPermissions.AuditLogs.Read,
        FineGrainedPermissions.AuthLogs.Read,
        FineGrainedPermissions.NavigationLogs.Read,
        FineGrainedPermissions.NavigationMenu.Manage,
        FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry,
        FineGrainedPermissions.Config.Read,
        FineGrainedPermissions.Config.Manage,
        FineGrainedPermissions.Integrations.Read,
        FineGrainedPermissions.Integrations.ManageContracts,
        FineGrainedPermissions.Integrations.ManageMappings,
        FineGrainedPermissions.Integrations.Validate,
        FineGrainedPermissions.Integrations.Publish,
        FineGrainedPermissions.Integrations.ManageConnections
    ];

    public static IReadOnlyList<string> OperatorBackfillCapabilities { get; } =
    [
        FineGrainedPermissions.Transactions.Create,
        FineGrainedPermissions.Nacha.Upload,
        FineGrainedPermissions.Returns.Read,
        FineGrainedPermissions.Returns.GenerateFile,
        FineGrainedPermissions.BulkIngestion.Read,
        FineGrainedPermissions.BulkIngestion.Upload,
        FineGrainedPermissions.BulkIngestion.Retry,
        FineGrainedPermissions.BulkIngestion.Cancel,
        FineGrainedPermissions.Certificates.Read,
        FineGrainedPermissions.DigitalEnvelope.Encrypt,
        FineGrainedPermissions.DigitalEnvelope.Decrypt,
        FineGrainedPermissions.NavigationLogs.Read,
        FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry,
        FineGrainedPermissions.Config.Read,
        FineGrainedPermissions.Config.Manage,
        FineGrainedPermissions.Integrations.Read,
        FineGrainedPermissions.Integrations.ManageContracts,
        FineGrainedPermissions.Integrations.ManageMappings,
        FineGrainedPermissions.Integrations.Validate,
        FineGrainedPermissions.Integrations.Publish,
        FineGrainedPermissions.Integrations.ManageConnections
    ];

    public int Order => BackfillOrder;

    public async Task SeedAsync()
    {
        var canonicalRoles = await context.Roles
            .AsNoTracking()
            .Where(role => role.Id == RoleConfiguration.AdminRoleId || role.Id == RoleConfiguration.OperatorRoleId)
            .ToDictionaryAsync(role => role.Id);

        ValidateCanonicalRole(canonicalRoles, RoleConfiguration.AdminRoleId, "Admin");
        ValidateCanonicalRole(canonicalRoles, RoleConfiguration.OperatorRoleId, "ACH.Operator");

        var requestedGrants = AdminBackfillCapabilities
            .Select(name => (RoleId: RoleConfiguration.AdminRoleId, PermissionName: name))
            .Concat(OperatorBackfillCapabilities.Select(name =>
                (RoleId: RoleConfiguration.OperatorRoleId, PermissionName: name)))
            .ToArray();

        var permissionNames = requestedGrants
            .Select(item => item.PermissionName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var permissions = (await context.Permissions
                .AsNoTracking()
                .Where(permission => permissionNames.Contains(permission.Name))
                .ToListAsync())
            .ToDictionary(permission => permission.Name, StringComparer.Ordinal);

        var missingPermissionNames = permissionNames
            .Where(name => !permissions.ContainsKey(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (missingPermissionNames.Length != 0)
        {
            throw new InvalidOperationException(
                $"NAV3.5A target permissions must be persisted before the role-grant backfill: {string.Join(", ", missingPermissionNames)}");
        }

        var roleIds = requestedGrants.Select(item => item.RoleId).Distinct().ToArray();
        var permissionIds = permissions.Values.Select(permission => permission.Id).ToArray();
        var existing = (await context.RolePermissions
                .AsNoTracking()
                .Where(link => roleIds.Contains(link.RoleId) && permissionIds.Contains(link.PermissionId))
                .Select(link => new { link.RoleId, link.PermissionId })
                .ToListAsync())
            .Select(link => (link.RoleId, link.PermissionId))
            .ToHashSet();

        var hasAdditions = false;
        foreach (var grant in requestedGrants)
        {
            var permissionId = permissions[grant.PermissionName].Id;
            if (!existing.Add((grant.RoleId, permissionId)))
                continue;

            context.RolePermissions.Add(new RolePermission
            {
                RoleId = grant.RoleId,
                PermissionId = permissionId
            });
            hasAdditions = true;
        }

        if (hasAdditions)
            await context.SaveChangesAsync();
    }

    private static void ValidateCanonicalRole(
        IReadOnlyDictionary<Guid, Role> roles,
        Guid roleId,
        string expectedName)
    {
        if (!roles.TryGetValue(roleId, out var role)
            || !string.Equals(role.Name, expectedName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The seeded {expectedName} role identity is missing or inconsistent.");
        }
    }
}
