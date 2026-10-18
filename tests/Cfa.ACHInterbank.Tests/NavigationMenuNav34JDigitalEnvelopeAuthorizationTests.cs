using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34JDigitalEnvelopeAuthorizationTests
{
    [Fact]
    public async Task CanonicalEncryptAndDecryptPermissions_ArePersistedIdempotently_WithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.DigitalEnvelope.Encrypt, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.DigitalEnvelope.Decrypt, FineGrainedPermissions.AllPermissions);
        Assert.False(await context.Permissions.AsNoTracking().AnyAsync(item => item.Name == FineGrainedPermissions.DigitalEnvelope.Encrypt));
        Assert.False(await context.Permissions.AsNoTracking().AnyAsync(item => item.Name == FineGrainedPermissions.DigitalEnvelope.Decrypt));

        var seeder = new DigitalEnvelopeOperationPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var encrypt = await context.Permissions.AsNoTracking().SingleAsync(item => item.Name == FineGrainedPermissions.DigitalEnvelope.Encrypt);
        var decrypt = await context.Permissions.AsNoTracking().SingleAsync(item => item.Name == FineGrainedPermissions.DigitalEnvelope.Decrypt);
        Assert.Equal(DigitalEnvelopeOperationPermissionSeeder.EncryptPermissionId, encrypt.Id);
        Assert.Equal(DigitalEnvelopeOperationPermissionSeeder.DecryptPermissionId, decrypt.Id);
        Assert.Equal("Cifrar archivos mediante sobre digital", encrypt.Description);
        Assert.Equal("Descifrar archivos mediante sobre digital", decrypt.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == encrypt.Id || link.PermissionId == decrypt.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task MenuEntry_UsesEffectivePreparationReadBoundary_WithoutRoleGate_AndKeepsCanonicalStructure()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        await SeedFunctionalNavigationAsync(context);
        await new NavigationInformationArchitectureSeeder(context).SeedAsync();
        await new DigitalEnvelopeOperationPermissionSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();

        var grantsBefore = await RoleGrantsAsync(context);
        var seeder = new DigitalEnvelopeMenuAuthorizationSeeder(context);
        Assert.True(NavigationInformationArchitectureSeeder.FinalReconciliationOrder < seeder.Order);
        Assert.True(PaymentRailCapabilityRegistryMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);

        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var parent = await context.MenuItems.AsNoTracking().SingleAsync(item => item.Id == MenuItemConfiguration.NachaSecurityId);
        var item = await context.MenuItems.AsNoTracking().SingleAsync(value => value.Id == MenuItemConfiguration.DigitalEnvelopeId);
        Assert.Equal("Seguridad de archivos", parent.Label);
        Assert.Equal("Cifrar y descifrar archivos", item.Label);
        Assert.Equal("/nacha-security/sobre-digital", item.Route);
        Assert.Equal(parent.Id, item.ParentId);
        Assert.Equal(20, item.Order);
        Assert.True(item.IsActive);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(new[] { PermissionConfiguration.ReadAchPermissionId }, permissions);
        Assert.Empty(await context.MenuItemRoles.AsNoTracking().Where(link => link.MenuItemId == item.Id).ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task BackendPolicies_KeepFineCryptographicCapabilitiesSeparate_WithManageAchFallback_AndRejectReadOnly()
    {
        using var provider = Provider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(User(FineGrainedPermissions.DigitalEnvelope.Encrypt), null, P1Policies.DigitalEnvelopeEncrypt)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(User("CanManageAch"), null, P1Policies.DigitalEnvelopeEncrypt)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User("CanReadAch"), null, P1Policies.DigitalEnvelopeEncrypt)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User(FineGrainedPermissions.DigitalEnvelope.Decrypt), null, P1Policies.DigitalEnvelopeEncrypt)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(User(FineGrainedPermissions.DigitalEnvelope.Decrypt), null, P1Policies.DigitalEnvelopeDecrypt)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(User("CanManageAch"), null, P1Policies.DigitalEnvelopeDecrypt)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User("CanReadAch"), null, P1Policies.DigitalEnvelopeDecrypt)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User(FineGrainedPermissions.DigitalEnvelope.Encrypt), null, P1Policies.DigitalEnvelopeDecrypt)).Succeeded);
    }

    [Fact]
    public void ConsumedApiEndpoints_HaveReadPreparationAndSeparateCryptographicPolicies()
    {
        AssertAction<SobreDigitalController>(nameof(SobreDigitalController.Certificates), typeof(HttpGetAttribute), P1Policies.CertificatesRead);
        AssertAction<SobreDigitalController>(nameof(SobreDigitalController.Encrypt), typeof(HttpPostAttribute), P1Policies.DigitalEnvelopeEncrypt);
        AssertAction<SobreDigitalController>(nameof(SobreDigitalController.Decrypt), typeof(HttpPostAttribute), P1Policies.DigitalEnvelopeDecrypt);
        AssertAction<ClearingHousesController>(nameof(ClearingHousesController.GetOperational), typeof(HttpGetAttribute), "CanReadAch");

        var testMethod = typeof(SobreDigitalController).GetMethod(nameof(SobreDigitalController.testRSA));
        Assert.NotNull(testMethod);
        Assert.Contains(testMethod!.GetCustomAttributes<ApiExplorerSettingsAttribute>(true), attribute => attribute.IgnoreApi);
        Assert.Contains(testMethod.GetCustomAttributes<AuthorizeAttribute>(true), attribute => attribute.Policy == P1Policies.DigitalEnvelopeTest);
    }

    [Fact]
    public async Task SpaModel_IsMixedReadPreparation_WithFailClosedEncryptAndDecryptActions()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts");
        var permissions = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-permissions.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.ts");
        var html = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.html");
        var service = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/services/sobre-digital.service.ts");
        var clearingHouses = await Read(root, "web/ach-interbank-ui/src/app/core/services/clearing-houses-api.service.ts");

        Assert.Contains("canActivateChild: [permissionGuard]", routing);
        var route = Slice(routing, "path: 'sobre-digital'", "path: '', pathMatch: 'full'");
        Assert.Contains("permissions: [NACHA_SECURITY_PERMISSIONS.canReadAch]", route);
        Assert.DoesNotContain("roleGuard", routing);
        Assert.DoesNotContain("canManageAch", route);
        Assert.DoesNotContain("canManualEncryptEnvelope", route);
        Assert.DoesNotContain("canManualDecryptEnvelope", route);
        Assert.Contains("digitalEnvelopeEncrypt: 'DigitalEnvelope.Encrypt'", permissions);
        Assert.Contains("digitalEnvelopeDecrypt: 'DigitalEnvelope.Decrypt'", permissions);

        Assert.Contains("get canEncrypt(): boolean", component);
        Assert.Contains("NACHA_SECURITY_PERMISSIONS.digitalEnvelopeEncrypt", component);
        Assert.Contains("get canDecrypt(): boolean", component);
        Assert.Contains("NACHA_SECURITY_PERMISSIONS.digitalEnvelopeDecrypt", component);
        Assert.Contains("NACHA_SECURITY_PERMISSIONS.canManageAch", component);
        Assert.Contains("if (!this.canExecute(mode))", component);
        Assert.Contains("!canEncrypt", html);
        Assert.Contains("!canDecrypt", html);
        Assert.Contains("permite consultar y preparar la operación", html);

        Assert.Contains("this.http.get<SobreDigitalCertificate[]>", service);
        Assert.Equal(2, Count(service, "this.http.post("));
        Assert.Contains("`${this.basePath}/encrypt`", service);
        Assert.Contains("`${this.basePath}/decrypt`", service);
        Assert.DoesNotContain("manual-encrypt", service);
        Assert.DoesNotContain("manual-decrypt", service);
        Assert.Contains("`${this.basePath}/operational`", clearingHouses);
    }

    [Fact]
    public async Task CryptoImplementation_KeepsPrivateMaterialServerSide_PersistsOnlyAuditMetadata_AndHasNoDirectExternalTransmission()
    {
        var root = RepositoryRoot();
        var managed = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/ManagedDigitalEnvelopeService.cs");
        var resolver = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/CertificateSecretResolverServices.cs");
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs");

        Assert.Contains("ResolvePrivateCertificateAsync", managed);
        Assert.Contains("_secretResolver.ResolveAsync", managed);
        Assert.Contains("GetRSAPrivateKey()", managed);
        Assert.Contains("DigitalEnvelopeOperationLogs.Add", managed);
        Assert.Contains("HashPlainSha256", managed);
        Assert.Contains("HashEncryptedSha256", managed);
        Assert.DoesNotContain("File.Write", managed);
        Assert.DoesNotContain("HttpClient", managed);
        Assert.DoesNotContain("SOAP", managed, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MFT", managed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("return File(result.Content, result.ContentType, result.FileName);", controller);

        Assert.Contains("DatabaseEncryptedCertificateSecretProvider", resolver);
        Assert.Contains("_protector.Unprotect(version.EncryptedPrivateMaterial)", resolver);
        Assert.Contains("X509KeyStorageFlags.EphemeralKeySet", resolver);
        Assert.Contains("CertificateSecretMasker.Mask", resolver);
    }

    [Fact]
    public async Task Regression_CertificatesAndTechnicalManualRoutesRemainUnchangedByNav34JModel()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/nacha-security/nacha-security-routing.module.ts");
        var certMenu = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/CertificateManagementMenuAuthorizationSeeder.cs");
        var handler = await Read(root, "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs");

        Assert.Contains("path: 'certificates'", routing);
        Assert.Contains("NACHA_SECURITY_PERMISSIONS.certificatesRead", routing);
        Assert.Contains("path: 'nacha/generate'", routing);
        Assert.Contains("path: 'nacha/generate-encrypted'", routing);
        Assert.Contains("path: 'digital-envelope/manual-encrypt'", routing);
        Assert.Contains("path: 'digital-envelope/manual-decrypt'", routing);
        Assert.Contains("CertificateManagementPermissionSeeder.ReadPermissionId", certMenu);
        Assert.Contains("return hasRole && hasPermission;", handler);
    }

    private static void AssertAction<TController>(string actionName, Type httpAttributeType, string expectedPolicy)
    {
        var method = typeof(TController).GetMethod(actionName);
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(true), attribute => httpAttributeType.IsInstanceOfType(attribute));
        Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(true), attribute =>
            attribute.Policy == expectedPolicy && string.IsNullOrWhiteSpace(attribute.Roles));
    }

    private static int Count(string source, string marker)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }
        return count;
    }

    private static async Task<string[]> RoleGrantsAsync(AchDbContext context)
        => (await context.RolePermissions.AsNoTracking()
            .OrderBy(link => link.RoleId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.RoleId, link.PermissionId })
            .ToArrayAsync())
            .Select(link => $"{link.RoleId}|{link.PermissionId}")
            .ToArray();

    private static async Task SeedFunctionalNavigationAsync(AchDbContext context)
    {
        IDbSeeder[] seeders =
        [
            new ClaimsPermissionSeeder(context),
            new CfaOperationalTransactionRestrictionsPermissionSeeder(context),
            new NachaSecurityMenuSeeder(context),
            new NachaConfigMenuSeeder(context),
            new OperationalRestrictionsMenuSeeder(context),
            new UsersMenuSeeder(context),
            new CyclesMenuSeeder(context),
            new IncomingNachaCommandCenterMenuSeeder(context),
            new AchClaimsMenuSeeder(context),
            new AchColombiaFileExchangeMenuSeeder(context),
            new AchColombiaMftAdministrationMenuSeeder(context),
            new OutgoingTransactionMonitoringSeeder(context),
            new ReportsMenuSeeder(context)
        ];

        foreach (var seeder in DbSeederExecutionOrder.Apply(seeders))
        {
            await seeder.SeedAsync();
        }
    }

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

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:tokenManager:issuerJwt"] = "issuer",
                ["appSettings:tokenManager:audienceJwt"] = "audience",
                ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
            })
            .Build();
        services.AddExternal(configuration);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal User(string permission)
        => new(new ClaimsIdentity([new Claim("permission", permission)], "test"));

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
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
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
