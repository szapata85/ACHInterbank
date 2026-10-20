using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav35BDeploymentSafeLegacyPermissionMigrationTests
{
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedByLegacyPermission =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["CanReadAch"] =
            [
                FineGrainedPermissions.Returns.Read,
                FineGrainedPermissions.BulkIngestion.Read,
                FineGrainedPermissions.Certificates.Read,
                FineGrainedPermissions.NavigationLogs.Read,
                FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry,
                FineGrainedPermissions.Config.Read,
                FineGrainedPermissions.Integrations.Read
            ],
            ["CanManageAch"] =
            [
                FineGrainedPermissions.Transactions.Create,
                FineGrainedPermissions.Nacha.Upload,
                FineGrainedPermissions.Returns.GenerateFile,
                FineGrainedPermissions.BulkIngestion.Upload,
                FineGrainedPermissions.BulkIngestion.Retry,
                FineGrainedPermissions.BulkIngestion.Cancel,
                FineGrainedPermissions.DigitalEnvelope.Encrypt,
                FineGrainedPermissions.DigitalEnvelope.Decrypt,
                FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry,
                FineGrainedPermissions.Config.Manage,
                FineGrainedPermissions.Integrations.ManageContracts,
                FineGrainedPermissions.Integrations.ManageMappings,
                FineGrainedPermissions.Integrations.Validate,
                FineGrainedPermissions.Integrations.Publish,
                FineGrainedPermissions.Integrations.ManageConnections
            ],
            ["CanManageUsers"] =
            [
                FineGrainedPermissions.AuditLogs.Read,
                FineGrainedPermissions.AuthLogs.Read,
                FineGrainedPermissions.NavigationMenu.Manage
            ],
            ["CanManageCertificates"] =
            [
                FineGrainedPermissions.Certificates.Read,
                FineGrainedPermissions.Certificates.UploadPublic,
                FineGrainedPermissions.Certificates.RegisterPrivate,
                FineGrainedPermissions.Certificates.Revoke
            ]
        };

    private static readonly (string Capability, string Policy)[] TargetPolicies =
    [
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
    public void MigrationMap_ContainsExactlyThePolicyBackedEdges_AndExcludesTestTools()
    {
        var rules = LegacyFineGrainedRoleMigrationSeeder.MigrationRules;

        Assert.Equal(29, rules.Count);
        Assert.Equal(27, rules.Select(rule => rule.FinePermission).Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(rules, rule => rule.FinePermission == FineGrainedPermissions.TestTools.Access);

        foreach (var expected in ExpectedByLegacyPermission)
        {
            Assert.Equal(
                expected.Value.OrderBy(value => value, StringComparer.Ordinal),
                rules.Where(rule => rule.LegacyPermission == expected.Key)
                    .Select(rule => rule.FinePermission)
                    .OrderBy(value => value, StringComparer.Ordinal));
        }
    }

    [Fact]
    public async Task CanonicalPostNav35AState_Nav35BAddsZeroRows_AndIsIdempotent()
    {
        await using var fixture = CreateFixture();
        await SeedPostNav35ABaselineAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var grantsBefore = await AllRolePermissionPairsAsync(fixture.Context);
        var rolesBefore = await fixture.Context.Roles.AsNoTracking().Select(role => role.Id).OrderBy(id => id).ToArrayAsync();
        var usersBefore = await fixture.Context.Users.AsNoTracking().Select(user => user.Id).OrderBy(id => id).ToArrayAsync();
        var userRolesBefore = await fixture.Context.UserRoles.AsNoTracking()
            .OrderBy(link => link.UserId).ThenBy(link => link.RoleId)
            .Select(link => $"{link.UserId:D}|{link.RoleId:D}").ToArrayAsync();

        var seeder = new LegacyFineGrainedRoleMigrationSeeder(fixture.Context);
        Assert.Equal(120, seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(grantsBefore, await AllRolePermissionPairsAsync(fixture.Context));
        Assert.Equal(rolesBefore, await fixture.Context.Roles.AsNoTracking().Select(role => role.Id).OrderBy(id => id).ToArrayAsync());
        Assert.Equal(usersBefore, await fixture.Context.Users.AsNoTracking().Select(user => user.Id).OrderBy(id => id).ToArrayAsync());
        Assert.Equal(userRolesBefore, await fixture.Context.UserRoles.AsNoTracking()
            .OrderBy(link => link.UserId).ThenBy(link => link.RoleId)
            .Select(link => $"{link.UserId:D}|{link.RoleId:D}").ToArrayAsync());
    }

    [Fact]
    public async Task CustomCanReadAchRole_ReceivesExactlyItsSevenFineEquivalents()
    {
        await AssertCustomRoleMigrationAsync("CustomReadRole", ["CanReadAch"], ExpectedByLegacyPermission["CanReadAch"]);
    }

    [Fact]
    public async Task CustomCanManageAchRole_ReceivesExactlyItsFifteenFineEquivalents_WithoutTestTools()
    {
        await AssertCustomRoleMigrationAsync("CustomManageRole", ["CanManageAch"], ExpectedByLegacyPermission["CanManageAch"]);
    }

    [Fact]
    public async Task CustomCanManageUsersRole_ReceivesOnlyItsThreeFineEquivalents()
    {
        await AssertCustomRoleMigrationAsync("CustomUserAdminRole", ["CanManageUsers"], ExpectedByLegacyPermission["CanManageUsers"]);
    }

    [Fact]
    public async Task CustomCanManageCertificatesRole_ReceivesExactlyItsFourCertificateEquivalents()
    {
        await AssertCustomRoleMigrationAsync("CustomCertificateRole", ["CanManageCertificates"], ExpectedByLegacyPermission["CanManageCertificates"]);
    }

    [Fact]
    public async Task CustomRoleWithMultipleLegacyPermissions_ReceivesUnionWithoutDuplicates_AndRerunAddsNothing()
    {
        await using var fixture = CreateFixture();
        await SeedPostNav35ABaselineAsync(fixture.Context);
        var roleId = await CreateCustomRoleAsync(
            fixture.Context,
            "CustomCombinedRole",
            "CanReadAch",
            "CanManageAch",
            "CanManageUsers",
            "CanManageCertificates");
        fixture.Context.ChangeTracker.Clear();

        var seeder = new LegacyFineGrainedRoleMigrationSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var expected = ExpectedByLegacyPermission.Values
            .SelectMany(values => values)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var firstRun = await FineMigrationGrantsAsync(fixture.Context, roleId);
        Assert.Equal(27, expected.Length);
        Assert.Equal(expected, firstRun);
        Assert.Equal(firstRun.Length, firstRun.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, firstRun);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(firstRun, await FineMigrationGrantsAsync(fixture.Context, roleId));
    }

    [Fact]
    public async Task CustomRoleWithoutTargetLegacyPermission_ReceivesZeroFineMigrationGrants()
    {
        await using var fixture = CreateFixture();
        await SeedPostNav35ABaselineAsync(fixture.Context);
        var roleId = await CreateCustomRoleAsync(fixture.Context, "CustomNoLegacyRole");
        fixture.Context.ChangeTracker.Clear();

        await new LegacyFineGrainedRoleMigrationSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Empty(await FineMigrationGrantsAsync(fixture.Context, roleId));
    }

    [Fact]
    public async Task CustomRolePolicyAudience_IsIdenticalBeforeAndAfterMigration()
    {
        await using var fixture = CreateFixture();
        await SeedPostNav35ABaselineAsync(fixture.Context);

        var customRoles = new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["CustomReadRole"] = await CreateCustomRoleAsync(fixture.Context, "CustomReadRole", "CanReadAch"),
            ["CustomManageRole"] = await CreateCustomRoleAsync(fixture.Context, "CustomManageRole", "CanManageAch"),
            ["CustomUserAdminRole"] = await CreateCustomRoleAsync(fixture.Context, "CustomUserAdminRole", "CanManageUsers"),
            ["CustomCertificateRole"] = await CreateCustomRoleAsync(fixture.Context, "CustomCertificateRole", "CanManageCertificates")
        };
        fixture.Context.ChangeTracker.Clear();

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var before = await AuthorizationMatrixAsync(fixture.Context, authorization, customRoles);

        await new LegacyFineGrainedRoleMigrationSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var after = await AuthorizationMatrixAsync(fixture.Context, authorization, customRoles);

        Assert.Equal(before.Count, after.Count);
        foreach (var row in before)
            Assert.Equal(row.Value, after[row.Key]);
    }

    private static async Task AssertCustomRoleMigrationAsync(string roleName, string[] legacyPermissions, string[] expectedFinePermissions)
    {
        await using var fixture = CreateFixture();
        await SeedPostNav35ABaselineAsync(fixture.Context);
        var roleId = await CreateCustomRoleAsync(fixture.Context, roleName, legacyPermissions);
        fixture.Context.ChangeTracker.Clear();

        var before = await FineMigrationGrantsAsync(fixture.Context, roleId);
        Assert.Empty(before);

        var seeder = new LegacyFineGrainedRoleMigrationSeeder(fixture.Context);
        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var after = await FineMigrationGrantsAsync(fixture.Context, roleId);
        Assert.Equal(
            expectedFinePermissions.OrderBy(value => value, StringComparer.Ordinal),
            after);
        Assert.DoesNotContain(FineGrainedPermissions.TestTools.Access, after);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(after, await FineMigrationGrantsAsync(fixture.Context, roleId));
    }

    private static async Task SeedPostNav35ABaselineAsync(AchDbContext context)
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
        await new FineGrainedGrantBackfillSeeder(context).SeedAsync();
    }

    private static async Task<Guid> CreateCustomRoleAsync(AchDbContext context, string roleName, params string[] legacyPermissions)
    {
        var roleId = Guid.NewGuid();
        context.Roles.Add(new Role
        {
            Id = roleId,
            Name = roleName,
            Description = $"NAV3.5B synthetic role {roleName}"
        });

        if (legacyPermissions.Length != 0)
        {
            var permissions = (await context.Permissions
                .Where(permission => permission.Name != null && legacyPermissions.Contains(permission.Name))
                .ToListAsync())
                .ToDictionary(permission => permission.Name!, StringComparer.Ordinal);

            Assert.Equal(legacyPermissions.Distinct(StringComparer.Ordinal).Count(), permissions.Count);
            foreach (var legacyPermission in legacyPermissions.Distinct(StringComparer.Ordinal))
            {
                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissions[legacyPermission].Id
                });
            }
        }

        await context.SaveChangesAsync();
        return roleId;
    }

    private static async Task<string[]> FineMigrationGrantsAsync(AchDbContext context, Guid roleId)
    {
        var fineTargets = LegacyFineGrainedRoleMigrationSeeder.MigrationRules
            .Select(rule => rule.FinePermission)
            .ToHashSet(StringComparer.Ordinal);
        var names = await context.RolePermissions.AsNoTracking()
            .Where(link => link.RoleId == roleId)
            .Join(context.Permissions.AsNoTracking(), link => link.PermissionId, permission => permission.Id, (_, permission) => permission.Name!)
            .ToArrayAsync();
        return names.Where(fineTargets.Contains)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<string[]> AllRolePermissionPairsAsync(AchDbContext context)
    {
        return await context.RolePermissions.AsNoTracking()
            .OrderBy(link => link.RoleId)
            .ThenBy(link => link.PermissionId)
            .Select(link => $"{link.RoleId:D}|{link.PermissionId:D}")
            .ToArrayAsync();
    }

    private static async Task<Dictionary<(string Role, string Capability), bool>> AuthorizationMatrixAsync(
        AchDbContext context,
        IAuthorizationService authorization,
        IReadOnlyDictionary<string, Guid> roles)
    {
        var result = new Dictionary<(string Role, string Capability), bool>();
        foreach (var role in roles)
        {
            var permissions = await context.RolePermissions.AsNoTracking()
                .Where(link => link.RoleId == role.Value)
                .Join(context.Permissions.AsNoTracking(), link => link.PermissionId, permission => permission.Id, (_, permission) => permission.Name!)
                .ToArrayAsync();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                permissions.Select(permission => new Claim("permission", permission)), "test"));

            foreach (var (capability, policy) in TargetPolicies)
                result[(role.Key, capability)] = (await authorization.AuthorizeAsync(principal, null, policy)).Succeeded;
        }

        return result;
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
