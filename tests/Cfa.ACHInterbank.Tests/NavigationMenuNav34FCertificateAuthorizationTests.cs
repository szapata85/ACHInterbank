using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
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

public sealed class NavigationMenuNav34FCertificateAuthorizationTests
{
    private const string CertificatesRoute = "/nacha-security/certificates";

    [Fact]
    public async Task PermissionSeeder_MaterializesExistingCertificateCapabilities_WithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var seeder = new CertificateManagementPermissionSeeder(context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var expected = new Dictionary<Guid, string>
        {
            [CertificateManagementPermissionSeeder.ReadPermissionId] = FineGrainedPermissions.Certificates.Read,
            [CertificateManagementPermissionSeeder.UploadPublicPermissionId] = FineGrainedPermissions.Certificates.UploadPublic,
            [CertificateManagementPermissionSeeder.RegisterPrivatePermissionId] = FineGrainedPermissions.Certificates.RegisterPrivate,
            [CertificateManagementPermissionSeeder.RevokePermissionId] = FineGrainedPermissions.Certificates.Revoke
        };

        foreach (var name in expected.Values)
            Assert.Contains(name, FineGrainedPermissions.AllPermissions);

        var persisted = await context.Permissions.AsNoTracking()
            .Where(item => expected.Keys.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name);
        Assert.Equal(expected.Count, persisted.Count);
        foreach (var pair in expected)
        {
            Assert.True(persisted.TryGetValue(pair.Key, out var persistedName));
            Assert.Equal(pair.Value, persistedName);
        }

        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => expected.Keys.Contains(link.PermissionId))
            .ToArrayAsync());
    }

    [Fact]
    public async Task CertificateMenu_MirrorsBackendReadAlternatives_WithoutChangingIdentity()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var item = new MenuItem
        {
            Id = MenuItemConfiguration.NachaCertificatesId,
            MenuId = MenuConfiguration.MainMenuId,
            ParentId = MenuItemConfiguration.NachaSecurityId,
            Label = "Certificados digitales",
            Route = CertificatesRoute,
            Icon = "verified_user",
            Order = 10,
            Exact = true,
            IsActive = true
        };
        context.MenuItems.Add(item);
        context.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = item.Id,
            PermissionId = PermissionConfiguration.ManageCertificatesPermissionId
        });
        await context.SaveChangesAsync();

        var before = await context.MenuItems.AsNoTracking().SingleAsync(x => x.Id == item.Id);
        var permissionSeeder = new CertificateManagementPermissionSeeder(context);
        var menuSeeder = new CertificateManagementMenuAuthorizationSeeder(context);
        Assert.True(BulkIngestionTrackingMenuAuthorizationSeeder.AuthorizationReconciliationOrder < menuSeeder.Order);

        await permissionSeeder.SeedAsync();
        await menuSeeder.SeedAsync();
        await menuSeeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking().SingleAsync(x => x.Id == item.Id);
        Assert.Equal(before.Label, after.Label);
        Assert.Equal(before.Route, after.Route);
        Assert.Equal(before.ParentId, after.ParentId);
        Assert.Equal(before.Order, after.Order);
        Assert.Equal(before.Icon, after.Icon);
        Assert.Equal(before.Exact, after.Exact);
        Assert.Equal(before.IsActive, after.IsActive);

        var links = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(3, links.Length);
        Assert.Contains(CertificateManagementPermissionSeeder.ReadPermissionId, links);
        Assert.Contains(PermissionConfiguration.ManageCertificatesPermissionId, links);
        Assert.Contains(PermissionConfiguration.ReadAchPermissionId, links);
    }

    [Fact]
    public async Task CertificateMenu_ReadAlternatives_AreOrSemantics()
    {
        var fine = new Permission { Name = FineGrainedPermissions.Certificates.Read };
        var manage = new Permission { Name = FineGrainedPermissions.CanManageCertificates };
        var legacy = new Permission { Name = "CanReadAch" };
        var item = new MenuItem
        {
            Id = 8805,
            Label = "Certificados digitales",
            Route = CertificatesRoute,
            Order = 10,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = fine });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = manage });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = legacy });
        var repository = new StaticMenuRepository([item]);

        Assert.Contains(await MenuFor(repository, FineGrainedPermissions.Certificates.Read), x => x.Route == CertificatesRoute);
        Assert.Contains(await MenuFor(repository, FineGrainedPermissions.CanManageCertificates), x => x.Route == CertificatesRoute);
        Assert.Contains(await MenuFor(repository, "CanReadAch"), x => x.Route == CertificatesRoute);
        Assert.DoesNotContain(await MenuFor(repository), x => x.Route == CertificatesRoute);
    }

    [Fact]
    public async Task CertificateApi_SeparatesReadUploadRegisterAndRevokeCapabilities()
    {
        AssertActionPolicy(nameof(CertificateManagementController.ListAsync), P1Policies.CertificatesRead);
        AssertActionPolicy(nameof(CertificateManagementController.ListVersionsAsync), P1Policies.CertificatesRead);
        AssertActionPolicy(nameof(CertificateManagementController.PreviewManagedAsync), P1Policies.CertificatesUploadPublic);
        AssertActionPolicy(nameof(CertificateManagementController.SaveManagedAsync), P1Policies.CertificatesRegisterPrivate);
        AssertActionPolicy(nameof(CertificateManagementController.RevokeAsync), P1Policies.CertificatesRevoke);
        AssertActionPolicy(nameof(CertificateManagementController.DeleteAsync), P1Policies.CertificatesRevoke);

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Certificates.Read), null, P1Policies.CertificatesRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.CanManageCertificates), null, P1Policies.CertificatesRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P1Policies.CertificatesRead)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Certificates.UploadPublic), null, P1Policies.CertificatesUploadPublic)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Certificates.RegisterPrivate), null, P1Policies.CertificatesRegisterPrivate)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Certificates.Revoke), null, P1Policies.CertificatesRevoke)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.CanManageCertificates), null, P1Policies.CertificatesRevoke)).Succeeded);

        Assert.False((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P1Policies.CertificatesUploadPublic)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Certificates.Read), null, P1Policies.CertificatesRegisterPrivate)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P1Policies.CertificatesRevoke)).Succeeded);
    }

    [Fact]
    public async Task CertificateSpa_IsMixedReadAndAdministration_WithFailClosedActionGates()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts");
        var permissions = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-permissions.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.ts");
        var template = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.html");

        var certificates = Slice(routing, "path: 'certificates'", "path: 'certificates/:id/versions'");
        Assert.Contains("certificatesRead", certificates);
        Assert.Contains("canManageCertificates", certificates);
        Assert.Contains("canReadAch", certificates);
        Assert.DoesNotContain("roles:", certificates);

        var versions = Slice(routing, "path: 'certificates/:id/versions'", "path: 'nacha/generate'");
        Assert.Contains("certificatesRead", versions);
        Assert.Contains("canManageCertificates", versions);
        Assert.Contains("canReadAch", versions);
        Assert.DoesNotContain("canManageAch", versions);

        Assert.Contains("certificatesUploadPublic: 'Certificates.UploadPublic'", permissions);
        Assert.Contains("certificatesRegisterPrivate: 'Certificates.RegisterPrivate'", permissions);
        Assert.Contains("certificatesRevoke: 'Certificates.Revoke'", permissions);
        Assert.Contains("get canUploadCertificates()", component);
        Assert.Contains("get canRevokeCertificates()", component);
        Assert.Contains("if (!this.canUploadCertificates)", component);
        Assert.Contains("if (!this.canRevokeCertificates)", component);
        Assert.Contains("if (!this.canPreviewCertificate)", component);
        Assert.Contains("if (!this.canSaveCertificate)", component);
        Assert.Contains("@if (canUploadCertificates)", template);
        Assert.Contains("canRevokeCertificates && canRevoke(certificate)", template);
        Assert.Contains("canRevokeCertificates && certificate.canDelete", template);
        Assert.DoesNotContain("hasRole(", component);
    }

    [Fact]
    public async Task CertificateFunctionalBehavior_ProtectsPrivateMaterial_AndDoesNotExposeDownloads()
    {
        var root = RepositoryRoot();
        var load = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/CertificateLoadService.cs");
        var deletion = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/CertificateDeletionService.cs");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.ts");
        var api = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/services/certificate-management-api.service.ts");

        Assert.Contains("_privateMaterialProtector.Protect", load);
        Assert.Contains("EncryptedPrivateMaterial", load);
        Assert.Contains("previous.Status = CertificateStatus.Replaced", load);
        Assert.Contains("CertificateRotationHistories", load);
        Assert.Contains("hasOperationalReferences", deletion);
        Assert.Contains("Revoca el certificado antes de eliminarlo", deletion);
        Assert.Contains("CryptographicOperations.ZeroMemory", deletion);

        Assert.DoesNotContain("downloadPublic(", component);
        Assert.DoesNotContain("downloadPublic(", api);
        Assert.DoesNotContain("api.activate(", component);
        Assert.DoesNotContain("api.validate(", component);
        Assert.DoesNotContain("api.audit(", component);
    }

    private static void AssertActionPolicy(string methodName, string expectedPolicy)
    {
        var action = typeof(CertificateManagementController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        var authorize = action!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }

    private static async Task<IList<MenuItemDto>> MenuFor(StaticMenuRepository repository, params string[] permissions)
        => await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext(permissions.Select(permission => new Claim("permission", permission))))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);

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

    private static ClaimsPrincipal Principal(params string[] permissions)
        => new(new ClaimsIdentity(
            permissions.Select(permission => new Claim("permission", permission)),
            "test"));

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return new Fixture(context, connection);
    }

    private static async Task<string> Read(string root, string relativePath)
        => await File.ReadAllTextAsync(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start marker not found: {startMarker}");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"End marker not found after start: {endMarker}");
        return source[start..end];
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ACHInterbank.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class StaticMenuRepository(IReadOnlyCollection<MenuItem> items) : IMenuQueryRepository
    {
        public Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(items.Where(item => item.IsActive).ToList());
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
