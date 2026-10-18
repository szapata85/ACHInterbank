using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav35AFineGrainedGrantBackfillTests
{
    private static readonly (string Capability, string Policy)[] TargetPolicies =
    [
        (FineGrainedPermissions.TestTools.Access, FineGrainedPermissions.TestTools.Access),
        (FineGrainedPermissions.Transactions.Create, P0Policies.TransactionsCreate),
        (FineGrainedPermissions.Nacha.Upload, P1Policies.NachaUpload),
        (FineGrainedPermissions.Returns.Read, P0Policies.ReturnsRead),
        (FineGrainedPermissions.Returns.GenerateFile, P0Policies.ReturnsGenerateFile),
        (FineGrainedPermissions.BulkIngestion.Read, P1Policies.BulkIngestionRead),
        (FineGrainedPermissions.BulkIngestion.Upload, P1Policies.BulkIngestionUpload),
        (FineGrainedPermissions.BulkIngestion.Retry, P1Policies.BulkIngestionRetry),
        (FineGrainedPermissions.BulkIngestion.Cancel, P1Policies.BulkIngestionCancel),
        (FineGrainedPermissions.Certificates.Read, P1Policies.CertificatesRead),
        (FineGrainedPermissions.Certificates.UploadPublic, P1Policies.CertificatesUploadPublic),
        (FineGrainedPermissions.Certificates.RegisterPrivate, P1Policies.CertificatesRegisterPrivate),
        (FineGrainedPermissions.Certificates.Revoke, P1Policies.CertificatesRevoke),
        (FineGrainedPermissions.DigitalEnvelope.Encrypt, P1Policies.DigitalEnvelopeEncrypt),
        (FineGrainedPermissions.DigitalEnvelope.Decrypt, P1Policies.DigitalEnvelopeDecrypt),
        (FineGrainedPermissions.AuditLogs.Read, P1Policies.AuditLogsRead),
        (FineGrainedPermissions.AuthLogs.Read, P1Policies.AuthLogsRead),
        (FineGrainedPermissions.NavigationLogs.Read, P1Policies.NavigationLogsRead),
        (FineGrainedPermissions.NavigationMenu.Manage, P1Policies.NavigationMenuManage),
        (FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry, FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry),
        (FineGrainedPermissions.Config.Read, P1Policies.ConfigRead),
        (FineGrainedPermissions.Config.Manage, P1Policies.ConfigManage),
        (FineGrainedPermissions.Integrations.Read, P1Policies.IntegrationsRead),
        (FineGrainedPermissions.Integrations.ManageContracts, P1Policies.IntegrationContractsManage),
        (FineGrainedPermissions.Integrations.ManageMappings, P1Policies.IntegrationMappingsManage),
        (FineGrainedPermissions.Integrations.Validate, P1Policies.IntegrationMappingsValidate),
        (FineGrainedPermissions.Integrations.Publish, P1Policies.IntegrationMappingsPublish),
        (FineGrainedPermissions.Integrations.ManageConnections, P1Policies.IntegrationConnectionsManage)
    ];

    [Fact]
    public async Task CanonicalSeededRoles_AndLegacyFallbackPopulation_AreExplicitAndStable()
    {
        await using var fixture = CreateFixture();
        var roles = await fixture.Context.Roles.AsNoTracking().OrderBy(role => role.Name).ToArrayAsync();

        Assert.Equal(2, roles.Length);
        Assert.Contains(roles, role => role.Id == RoleConfiguration.AdminRoleId && role.Name == "Admin");
        Assert.Contains(roles, role => role.Id == RoleConfiguration.OperatorRoleId && role.Name == "ACH.Operator");

        Assert.Equal(
            ["CanManageAch", "CanManageCertificates", "CanManageUsers", "CanReadAch"],
            (await LegacyFallbacksAsync(fixture.Context, RoleConfiguration.AdminRoleId)).OrderBy(value => value).ToArray());
        Assert.Equal(
            ["CanManageAch", "CanReadAch"],
            (await LegacyFallbacksAsync(fixture.Context, RoleConfiguration.OperatorRoleId)).OrderBy(value => value).ToArray());
    }

    [Fact]
    public async Task Backfill_AddsExactlyFortyEightEquivalentRolePermissions_AndIsIdempotent()
    {
        await using var fixture = CreateFixture();
        await SeedTargetPermissionsAndExistingTestToolsGrantAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var before = await TargetGrantPairsAsync(fixture.Context);
        Assert.Single(before);
        Assert.Equal($"{RoleConfiguration.AdminRoleId:D}|{FineGrainedPermissions.TestTools.Access}", before[0]);

        var rolesBefore = await fixture.Context.Roles.AsNoTracking().Select(role => role.Id).OrderBy(id => id).ToArrayAsync();
        var userRolesBefore = await fixture.Context.UserRoles.AsNoTracking()
            .OrderBy(link => link.UserId).ThenBy(link => link.RoleId)
            .Select(link => $"{link.UserId:D}|{link.RoleId:D}").ToArrayAsync();

        var seeder = new FineGrainedGrantBackfillSeeder(fixture.Context);
        Assert.Equal(119, seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var after = await TargetGrantPairsAsync(fixture.Context);
        Assert.Equal(49, after.Length); // 48 new grants plus the pre-existing Admin TestTools.Access grant.
        Assert.Equal(49, after.Distinct(StringComparer.Ordinal).Count());

        var admin = await FineTargetGrantsAsync(fixture.Context, RoleConfiguration.AdminRoleId);
        var operatorGrants = await FineTargetGrantsAsync(fixture.Context, RoleConfiguration.OperatorRoleId);
        Assert.Equal(28, admin.Length);
        Assert.Equal(21, operatorGrants.Length);
        Assert.Contains(FineGrainedPermissions.TestTools.Access, admin);
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, operatorGrants);

        Assert.Equal(
            FineGrainedGrantBackfillSeeder.AdminBackfillCapabilities.OrderBy(value => value, StringComparer.Ordinal),
            admin.Where(value => value != FineGrainedPermissions.TestTools.Access).OrderBy(value => value, StringComparer.Ordinal));
        Assert.Equal(
            FineGrainedGrantBackfillSeeder.OperatorBackfillCapabilities.OrderBy(value => value, StringComparer.Ordinal),
            operatorGrants.OrderBy(value => value, StringComparer.Ordinal));

        Assert.DoesNotContain(FineGrainedPermissions.Certificates.UploadPublic, operatorGrants);
        Assert.DoesNotContain(FineGrainedPermissions.Certificates.RegisterPrivate, operatorGrants);
        Assert.DoesNotContain(FineGrainedPermissions.Certificates.Revoke, operatorGrants);
        Assert.DoesNotContain(FineGrainedPermissions.AuditLogs.Read, operatorGrants);
        Assert.DoesNotContain(FineGrainedPermissions.AuthLogs.Read, operatorGrants);
        Assert.DoesNotContain(FineGrainedPermissions.NavigationMenu.Manage, operatorGrants);

        Assert.Equal(rolesBefore, await fixture.Context.Roles.AsNoTracking().Select(role => role.Id).OrderBy(id => id).ToArrayAsync());
        Assert.Equal(userRolesBefore, await fixture.Context.UserRoles.AsNoTracking()
            .OrderBy(link => link.UserId).ThenBy(link => link.RoleId)
            .Select(link => $"{link.UserId:D}|{link.RoleId:D}").ToArrayAsync());
    }

    [Fact]
    public async Task SeededRolePolicyAudience_IsIdenticalBeforeAndAfterBackfill()
    {
        await using var fixture = CreateFixture();
        await SeedTargetPermissionsAndExistingTestToolsGrantAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var before = await AuthorizationMatrixAsync(fixture.Context, authorization);
        await new FineGrainedGrantBackfillSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var after = await AuthorizationMatrixAsync(fixture.Context, authorization);

        Assert.Equal(before.Count, after.Count);
        foreach (var row in before)
            Assert.Equal(row.Value, after[row.Key]);

        Assert.True(after[("Admin", FineGrainedPermissions.TestTools.Access)]);
        Assert.False(after[("ACH.Operator", FineGrainedPermissions.TestTools.Access)]);
        Assert.False(after[("ACH.Operator", FineGrainedPermissions.Certificates.UploadPublic)]);
        Assert.False(after[("ACH.Operator", FineGrainedPermissions.Certificates.RegisterPrivate)]);
        Assert.False(after[("ACH.Operator", FineGrainedPermissions.Certificates.Revoke)]);
        Assert.False(after[("ACH.Operator", FineGrainedPermissions.AuditLogs.Read)]);
        Assert.False(after[("ACH.Operator", FineGrainedPermissions.AuthLogs.Read)]);
        Assert.False(after[("ACH.Operator", FineGrainedPermissions.NavigationMenu.Manage)]);
    }

    [Fact]
    public void TargetInventory_IsCompleteStableAndLimitedToNav3MigrationCapabilities()
    {
        var target = TargetPolicies.Select(item => item.Capability).ToArray();
        Assert.Equal(28, target.Length);
        Assert.Equal(28, target.Distinct(StringComparer.Ordinal).Count());
        Assert.All(target, capability => Assert.Contains(capability, FineGrainedPermissions.AllPermissions));

        Assert.DoesNotContain(FineGrainedPermissions.Nacha.Generate, target);
        Assert.DoesNotContain(FineGrainedPermissions.Certificates.Activate, target);
        Assert.DoesNotContain(FineGrainedPermissions.Certificates.Validate, target);
        Assert.DoesNotContain(FineGrainedPermissions.Certificates.Audit, target);
    }

    private static async Task SeedTargetPermissionsAndExistingTestToolsGrantAsync(AchDbContext context)
    {
        IDbSeeder[] permissionSeeders =
        [
            new TestToolsPermissionSeeder(context),
            new TransactionsCreatePermissionSeeder(context),
            new NachaUploadPermissionSeeder(context),
            new ReturnsReadPermissionSeeder(context),
            new ReturnsPermissionSeeder(context),
            new BulkIngestionTrackingPermissionSeeder(context),
            new BulkIngestionUploadPermissionSeeder(context),
            new CertificateManagementPermissionSeeder(context),
            new DigitalEnvelopeOperationPermissionSeeder(context),
            new SystemRecordPermissionsSeeder(context),
            new NavigationMenuPermissionSeeder(context),
            new PaymentRailCapabilityRegistryPermissionSeeder(context),
            new ConfigurationPermissionSeeder(context),
            new IntegrationReadPermissionSeeder(context),
            new IntegrationContractPermissionSeeder(context),
            new IntegrationMappingPermissionSeeder(context),
            new IntegrationConnectionPermissionSeeder(context)
        ];

        foreach (var seeder in permissionSeeders.OrderBy(item => item.Order).ThenBy(item => item.GetType().FullName, StringComparer.Ordinal))
            await seeder.SeedAsync();

        await new TestToolsAdminRolePermissionSeeder(context).SeedAsync();
    }

    private static async Task<Dictionary<(string Role, string Capability), bool>> AuthorizationMatrixAsync(
        AchDbContext context,
        IAuthorizationService authorization)
    {
        var result = new Dictionary<(string Role, string Capability), bool>();
        foreach (var (roleId, roleName) in new[]
        {
            (RoleConfiguration.AdminRoleId, "Admin"),
            (RoleConfiguration.OperatorRoleId, "ACH.Operator")
        })
        {
            var permissions = await context.RolePermissions.AsNoTracking()
                .Where(link => link.RoleId == roleId)
                .Join(context.Permissions.AsNoTracking(), link => link.PermissionId, permission => permission.Id, (_, permission) => permission.Name)
                .ToArrayAsync();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                permissions.Select(permission => new Claim("permission", permission)), "test"));

            foreach (var (capability, policy) in TargetPolicies)
                result[(roleName, capability)] = (await authorization.AuthorizeAsync(principal, null, policy)).Succeeded;
        }
        return result;
    }

    private static async Task<string[]> LegacyFallbacksAsync(AchDbContext context, Guid roleId)
    {
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "CanManageAch", "CanReadAch", "CanManageUsers", "CanManageCertificates"
        };
        return await context.RolePermissions.AsNoTracking()
            .Where(link => link.RoleId == roleId)
            .Join(context.Permissions.AsNoTracking(), link => link.PermissionId, permission => permission.Id, (_, permission) => permission.Name)
            .Where(name => names.Contains(name))
            .ToArrayAsync();
    }

    private static async Task<string[]> FineTargetGrantsAsync(AchDbContext context, Guid roleId)
    {
        var targets = TargetPolicies.Select(item => item.Capability).ToHashSet(StringComparer.Ordinal);
        return (await context.RolePermissions.AsNoTracking()
                .Where(link => link.RoleId == roleId)
                .Join(context.Permissions.AsNoTracking(), link => link.PermissionId, permission => permission.Id, (_, permission) => permission.Name)
                .ToArrayAsync())
            .Where(targets.Contains)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<string[]> TargetGrantPairsAsync(AchDbContext context)
    {
        var targets = TargetPolicies.Select(item => item.Capability).ToHashSet(StringComparer.Ordinal);
        var rows = await context.RolePermissions.AsNoTracking()
            .Join(context.Permissions.AsNoTracking(), link => link.PermissionId, permission => permission.Id,
                (link, permission) => new { link.RoleId, permission.Name })
            .ToArrayAsync();
        return rows.Where(row => targets.Contains(row.Name))
            .Select(row => $"{row.RoleId:D}|{row.Name}")
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static ServiceProvider BuildExternalProvider()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["appSettings:tokenManager:issuerJwt"] = "issuer-test",
            ["appSettings:tokenManager:audienceJwt"] = "audience-test",
            ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
        }).Build();
        services.AddExternal(config);
        return services.BuildServiceProvider();
    }

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        return new Fixture(context, connection);
    }

    private sealed class Fixture(AchDbContext context, SqliteConnection connection) : IAsyncDisposable
    {
        public AchDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
