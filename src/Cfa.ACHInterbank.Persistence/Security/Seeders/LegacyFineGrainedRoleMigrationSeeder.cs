using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Seeders;

/// <summary>
/// NAV3.5B deployment-safe compatibility backfill. For every existing role, mirrors only
/// demonstrated legacy policy fallbacks into their equivalent NAV3 fine-grained permissions.
/// It does not remove fallbacks, create roles, change users, or grant unrelated capabilities.
/// </summary>
[Scoped]
public sealed class LegacyFineGrainedRoleMigrationSeeder(AchDbContext context) : IDbSeeder
{
    public const int MigrationOrder = 120;

    public sealed record MigrationRule(string LegacyPermission, string FinePermission);

    public static IReadOnlyList<MigrationRule> MigrationRules { get; } =
    [
        // CanReadAch transition edges.
        new("CanReadAch", FineGrainedPermissions.Returns.Read),
        new("CanReadAch", FineGrainedPermissions.BulkIngestion.Read),
        new("CanReadAch", FineGrainedPermissions.Certificates.Read),
        new("CanReadAch", FineGrainedPermissions.NavigationLogs.Read),
        new("CanReadAch", FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry),
        new("CanReadAch", FineGrainedPermissions.Config.Read),
        new("CanReadAch", FineGrainedPermissions.Integrations.Read),

        // CanManageAch transition edges.
        new("CanManageAch", FineGrainedPermissions.Transactions.Create),
        new("CanManageAch", FineGrainedPermissions.Nacha.Upload),
        new("CanManageAch", FineGrainedPermissions.Returns.GenerateFile),
        new("CanManageAch", FineGrainedPermissions.BulkIngestion.Upload),
        new("CanManageAch", FineGrainedPermissions.BulkIngestion.Retry),
        new("CanManageAch", FineGrainedPermissions.BulkIngestion.Cancel),
        new("CanManageAch", FineGrainedPermissions.DigitalEnvelope.Encrypt),
        new("CanManageAch", FineGrainedPermissions.DigitalEnvelope.Decrypt),
        new("CanManageAch", FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry),
        new("CanManageAch", FineGrainedPermissions.Config.Manage),
        new("CanManageAch", FineGrainedPermissions.Integrations.ManageContracts),
        new("CanManageAch", FineGrainedPermissions.Integrations.ManageMappings),
        new("CanManageAch", FineGrainedPermissions.Integrations.Validate),
        new("CanManageAch", FineGrainedPermissions.Integrations.Publish),
        new("CanManageAch", FineGrainedPermissions.Integrations.ManageConnections),

        // CanManageUsers transition edges.
        new("CanManageUsers", FineGrainedPermissions.AuditLogs.Read),
        new("CanManageUsers", FineGrainedPermissions.AuthLogs.Read),
        new("CanManageUsers", FineGrainedPermissions.NavigationMenu.Manage),

        // CanManageCertificates transition edges.
        new("CanManageCertificates", FineGrainedPermissions.Certificates.Read),
        new("CanManageCertificates", FineGrainedPermissions.Certificates.UploadPublic),
        new("CanManageCertificates", FineGrainedPermissions.Certificates.RegisterPrivate),
        new("CanManageCertificates", FineGrainedPermissions.Certificates.Revoke)
    ];

    public int Order => MigrationOrder;

    public async Task SeedAsync()
    {
        var permissionNames = MigrationRules
            .SelectMany(rule => new[] { rule.LegacyPermission, rule.FinePermission })
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var permissions = (await context.Permissions
                .AsNoTracking()
                .Where(permission => permission.Name != null && permissionNames.Contains(permission.Name))
                .ToListAsync())
            .ToDictionary(permission => permission.Name!, StringComparer.Ordinal);

        var missingPermissionNames = permissionNames
            .Where(name => !permissions.ContainsKey(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (missingPermissionNames.Length != 0)
        {
            throw new InvalidOperationException(
                $"NAV3.5B migration permissions must exist before role compatibility backfill: {string.Join(", ", missingPermissionNames)}");
        }

        var legacyIds = MigrationRules
            .Select(rule => permissions[rule.LegacyPermission].Id)
            .Distinct()
            .ToArray();

        var legacyRoleGrants = await context.RolePermissions
            .AsNoTracking()
            .Where(link => legacyIds.Contains(link.PermissionId))
            .Select(link => new { link.RoleId, link.PermissionId })
            .ToListAsync();

        if (legacyRoleGrants.Count == 0)
            return;

        var rulesByLegacyPermissionId = MigrationRules
            .GroupBy(rule => permissions[rule.LegacyPermission].Id)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(rule => permissions[rule.FinePermission].Id)
                    .Distinct()
                    .ToArray());

        var requestedGrants = new HashSet<(Guid RoleId, Guid PermissionId)>();
        foreach (var legacyGrant in legacyRoleGrants)
        {
            foreach (var finePermissionId in rulesByLegacyPermissionId[legacyGrant.PermissionId])
                requestedGrants.Add((legacyGrant.RoleId, finePermissionId));
        }

        if (requestedGrants.Count == 0)
            return;

        var roleIds = requestedGrants.Select(grant => grant.RoleId).Distinct().ToArray();
        var finePermissionIds = requestedGrants.Select(grant => grant.PermissionId).Distinct().ToArray();
        var existing = (await context.RolePermissions
                .AsNoTracking()
                .Where(link => roleIds.Contains(link.RoleId) && finePermissionIds.Contains(link.PermissionId))
                .Select(link => new { link.RoleId, link.PermissionId })
                .ToListAsync())
            .Select(link => (link.RoleId, link.PermissionId))
            .ToHashSet();

        var hasAdditions = false;
        foreach (var grant in requestedGrants.OrderBy(item => item.RoleId).ThenBy(item => item.PermissionId))
        {
            if (!existing.Add(grant))
                continue;

            context.RolePermissions.Add(new RolePermission
            {
                RoleId = grant.RoleId,
                PermissionId = grant.PermissionId
            });
            hasAdditions = true;
        }

        if (hasAdditions)
            await context.SaveChangesAsync();
    }
}
