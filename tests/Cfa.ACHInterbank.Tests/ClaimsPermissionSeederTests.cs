using System.IdentityModel.Tokens.Jwt;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.DataBase.Repositories.Security;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.Services.Authentication.Implementations;
using Cfa.ACHInterbank.Application.Services.Authentication.Models;
using Cfa.ACHInterbank.Application.Services.Notifications.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.DataBase.Repositories.Security;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Cfa.ACHInterbank.Tests;

[CollectionDefinition("ClaimsPermissionAuth", DisableParallelization = true)]
public sealed class ClaimsPermissionAuthCollection { }

[Collection("ClaimsPermissionAuth")]
public sealed class ClaimsPermissionSeederTests
{
    private static readonly (Guid Id, string Name, string Description)[] ExpectedPermissions =
    [
        (ClaimsPermissionSeeder.ReadPermissionId, FineGrainedPermissions.Claims.Read, "Consultar reclamaciones ACH"),
        (ClaimsPermissionSeeder.CreateDev14PermissionId, FineGrainedPermissions.Claims.CreateDev14, "Crear reclamaciones DEV14 por débito ACH no consentido"),
        (ClaimsPermissionSeeder.ProgressPermissionId, FineGrainedPermissions.Claims.Progress, "Registrar avances en reclamaciones ACH"),
        (ClaimsPermissionSeeder.ResolvePermissionId, FineGrainedPermissions.Claims.Resolve, "Resolver reclamaciones ACH"),
        (ClaimsPermissionSeeder.ExpirePermissionId, FineGrainedPermissions.Claims.Expire, "Cerrar reclamaciones ACH por vencimiento")
    ];

    [Fact]
    public async Task SeedAsync_CreatesFiveAssignablePermissions_Idempotently()
    {
        await using var fixture = CreateFixture();
        var sut = new ClaimsPermissionSeeder(fixture.Context);

        await sut.SeedAsync();
        await sut.SeedAsync();

        var names = ExpectedPermissions.Select(x => x.Name).ToArray();
        var permissions = await fixture.Context.Permissions
            .Where(x => names.Contains(x.Name!))
            .OrderBy(x => x.Name)
            .ToListAsync();

        Assert.Equal(ExpectedPermissions.Length, permissions.Count);
        foreach (var expected in ExpectedPermissions)
        {
            var permission = Assert.Single(permissions, x => x.Name == expected.Name);
            Assert.Equal(expected.Id, permission.Id);
            Assert.Equal(expected.Description, permission.Description);
        }

        var claimsPermissionIds = ExpectedPermissions.Select(x => x.Id).ToArray();
        Assert.Empty(await fixture.Context.RolePermissions
            .Where(x => claimsPermissionIds.Contains(x.PermissionId))
            .ToListAsync());
    }

    [Fact]
    public async Task PermissionsService_ReturnsAllFiveClaimsPermissions()
    {
        await using var fixture = CreateFixture();
        await new ClaimsPermissionSeeder(fixture.Context).SeedAsync();

        var permissions = (await new PermissionsService(fixture.Context).GetAllAsync()).ToList();

        foreach (var expected in ExpectedPermissions)
        {
            var permission = Assert.Single(permissions, x => x.Name == expected.Name);
            Assert.Equal(expected.Id, permission.Id);
            Assert.Equal(expected.Description, permission.Description);
        }
    }

    [Theory]
    [InlineData(FineGrainedPermissions.Claims.CreateDev14)]
    [InlineData(FineGrainedPermissions.Claims.Progress)]
    public async Task Login_EmitsAssignedClaimsPermission_FromRolePermission(string permissionName)
    {
        await using var fixture = CreateFixture();
        await new ClaimsPermissionSeeder(fixture.Context).SeedAsync();

        var permission = await fixture.Context.Permissions.SingleAsync(x => x.Name == permissionName);
        fixture.Context.RolePermissions.Add(new RolePermission
        {
            RoleId = RoleConfiguration.AdminRoleId,
            PermissionId = permission.Id
        });
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();

        var tokenOptions = Options.Create(new Token
        {
            secretKetJwt = "claims-permission-test-secret-key-0123456789",
            issuerJwt = "claims-tests",
            audienceJwt = "claims-tests",
            expirationType = "m",
            accessExpiration = 30
        });
        var authenticationUiOptions = Options.Create(new AuthenticationUiOptions());

        var lockoutRepository = new Mock<ILoginLockoutSettingsRepository>();
        lockoutRepository
            .Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginLockoutSetting?)null);

        var sut = new AuthService(
            new UserAuthRepository(fixture.Context),
            lockoutRepository.Object,
            Mock.Of<IPasswordResetTokenRepository>(),
            Mock.Of<IEmailSender>(),
            Mock.Of<IUnitOfWork>(),
            tokenOptions,
            authenticationUiOptions);

        var result = await sut.LoginAsync(new LoginRequest
        {
            Username = "admin",
            Password = "Admin123!"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.Contains(permissionName, result.Permissions);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Contains(jwt.Claims, claim => claim.Type == "permission" && claim.Value == permissionName);
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
