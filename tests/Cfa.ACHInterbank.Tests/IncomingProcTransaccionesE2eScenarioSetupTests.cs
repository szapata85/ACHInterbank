using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingProcTransaccionesE2eScenarioSetupTests
{
    private static readonly DateTime OperationalDate = new(2026, 5, 24);

    [Fact]
    public async Task EnsureAsync_WithoutExplicitAuthorization_BlocksBeforeMutation()
    {
        await using var fixture = await ScenarioFixture.CreateAsync(includeCfa: true, includeExternal: true, authorized: false);
        var sut = fixture.CreateService();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.EnsureAsync(Request()));

        Assert.Contains("SETUP_NOT_AUTHORIZED", error.Message);
        Assert.Empty(await fixture.Context.AchTransactions.ToListAsync());
    }

    [Fact]
    public async Task EnsureAsync_WithSingleCfa_CreatesFunctionalIncomingAnchorIdempotently()
    {
        await using var fixture = await ScenarioFixture.CreateAsync(includeCfa: true, includeExternal: true);
        var sut = fixture.CreateService();

        var first = await sut.EnsureAsync(Request());
        var second = await sut.EnsureAsync(Request());

        Assert.True(first.IsReady);
        Assert.True(first.CreatedTransaction);
        Assert.False(first.CreatedExternalInstitution);
        Assert.False(second.CreatedTransaction);
        Assert.False(second.CreatedExternalInstitution);
        Assert.Equal(first.TransactionId, second.TransactionId);
        Assert.Equal(1, await fixture.Context.AchTransactions.CountAsync(x => x.TransactionExternalId.StartsWith("E2E-PTX-IN-")));
        Assert.Equal(1, await fixture.Context.AchBatches.CountAsync(x => x.CompanyName == IncomingProcTransaccionesE2eScenarioSetupService.BatchCompanyName));

        var transaction = await fixture.Context.AchTransactions.AsNoTracking().SingleAsync();
        Assert.Equal(TransactionTypeEnum.Credit, transaction.Type);
        Assert.Equal("22", transaction.TransactionCode);
        Assert.Equal(123.45m, transaction.Amount);
        Assert.Equal(fixture.Cfa!.Id, transaction.DestinationInstitutionId);
        Assert.Equal(fixture.External!.Id, transaction.SourceInstitutionId);
        Assert.Equal("E2EACCOUNT0008684", transaction.DestinationAccountNumber);
        Assert.Equal(IncomingProcTransaccionesE2eScenarioSetupService.SyntheticRecipientId, transaction.RecipientIdNumber);
        Assert.Equal(AchTransferStateEnum.Pending, transaction.State);
    }

    [Fact]
    public async Task EnsureAsync_WithoutCfa_BlocksAndDoesNotCreateCfa()
    {
        await using var fixture = await ScenarioFixture.CreateAsync(includeCfa: false, includeExternal: true);
        var sut = fixture.CreateService();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.EnsureAsync(Request()));

        Assert.Contains("CFA_AMBIGUOUS", error.Message);
        Assert.Empty(await fixture.Context.FinancialInstitutions.Where(x => x.IsDefaultSource).ToListAsync());
    }

    [Fact]
    public async Task EnsureAsync_WithMultipleCfa_BlocksWithoutChangingCanonicalInstitutions()
    {
        await using var fixture = await ScenarioFixture.CreateAsync(includeCfa: true, includeExternal: true);
        var second = Institution("CFA DUPLICADA SINTETICA", "00002", "006", isDefault: true);
        fixture.Context.FinancialInstitutions.Add(second);
        await fixture.Context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CreateService().EnsureAsync(Request()));

        Assert.Contains("CFA_AMBIGUOUS", error.Message);
        Assert.Equal(2, await fixture.Context.FinancialInstitutions.CountAsync(x => x.IsDefaultSource));
    }

    [Fact]
    public async Task EnsureAsync_MissingExternal_CreatesOnlyApprovedSyntheticInstitution()
    {
        await using var fixture = await ScenarioFixture.CreateAsync(includeCfa: true, includeExternal: false);

        var result = await fixture.CreateService().EnsureAsync(Request());

        Assert.True(result.CreatedExternalInstitution);
        var external = await fixture.Context.FinancialInstitutions.SingleAsync(x => x.Name == FinancialInstitutionSeeder.SyntheticAchExternalName);
        Assert.False(external.IsDefaultSource);
        Assert.Equal(FinancialInstitutionSeeder.SyntheticAchExternalRouting, external.RoutingNumber);
        Assert.Equal(FinancialInstitutionSeeder.SyntheticAchExternalTransit, external.TransitCode);
        Assert.NotEqual(result.CfaInstitutionId, result.ExternalInstitutionId);
    }

    [Fact]
    public async Task EnsureAsync_NonSyntheticCollision_DoesNotAlterExistingInstitution()
    {
        await using var fixture = await ScenarioFixture.CreateAsync(includeCfa: true, includeExternal: false);
        var collision = Institution(FinancialInstitutionSeeder.SyntheticAchExternalName, "12345", "678", isDefault: false);
        fixture.Context.FinancialInstitutions.Add(collision);
        await fixture.Context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CreateService().EnsureAsync(Request()));

        Assert.Contains("EXTERNAL_NOT_SYNTHETIC", error.Message);
        var persisted = await fixture.Context.FinancialInstitutions.SingleAsync(x => x.Id == collision.Id);
        Assert.Equal("12345", persisted.RoutingNumber);
        Assert.Equal("678", persisted.TransitCode);
        Assert.Empty(await fixture.Context.AchTransactions.ToListAsync());
    }

    [Fact]
    public void RelevantModel_IsEquivalentForSqlServerAndPostgresProviders()
    {
        using var sql = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlServer("Server=localhost;Database=unused;User Id=unused;Password=unused;TrustServerCertificate=True")
            .Options);
        using var postgres = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options);

        foreach (var entityType in new[] { typeof(FinancialInstitution), typeof(AchTransaction), typeof(AchBatch), typeof(AchCycle) })
        {
            var sqlEntity = sql.Model.FindEntityType(entityType);
            var postgresEntity = postgres.Model.FindEntityType(entityType);
            Assert.NotNull(sqlEntity);
            Assert.NotNull(postgresEntity);
            Assert.Equal(
                sqlEntity!.GetProperties().Select(x => x.Name).OrderBy(x => x),
                postgresEntity!.GetProperties().Select(x => x.Name).OrderBy(x => x));
        }
    }

    private static IncomingProcTransaccionesE2eScenarioRequest Request() => new()
    {
        OperationalDate = OperationalDate,
        CycleNumber = 6
    };

    private static FinancialInstitution Institution(string name, string routing, string transit, bool isDefault)
    {
        var institution = new FinancialInstitution
        {
            Name = name,
            RoutingNumber = routing,
            TransitCode = transit,
            IsDefaultSource = isDefault,
            Status = FinancialInstitutionStatus.Active
        };
        institution.CalculateCheckDigit();
        return institution;
    }

    private sealed class ScenarioFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _environment;

        private ScenarioFixture(
            SqliteConnection connection,
            AchDbContext context,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            _connection = connection;
            Context = context;
            _configuration = configuration;
            _environment = environment;
        }

        public AchDbContext Context { get; }
        public FinancialInstitution? Cfa { get; private set; }
        public FinancialInstitution? External { get; private set; }

        public static async Task<ScenarioFixture> CreateAsync(
            bool includeCfa,
            bool includeExternal,
            bool authorized = true)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                [IncomingProcTransaccionesE2eScenarioSetupService.SetupAuthorizationVariable] = authorized ? "true" : "false",
                [IncomingProcTransaccionesE2eScenarioSetupService.ReceiverAccountVariable] = "E2EACCOUNT0008684",
                [IncomingProcTransaccionesE2eScenarioSetupService.ExpectedAmountVariable] = "123.45"
            }).Build();
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns("Testing");
            var fixture = new ScenarioFixture(connection, context, configuration, environment.Object);

            context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" });
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1,
                Name = "ACH Colombia",
                Code = "ACHCOL",
                OriginCode = "12345678",
                ClearingHouseId = 1
            });
            context.AchCycles.Add(new AchCycle
            {
                Id = "ACH-20260524-06",
                CycleName = "Ciclo 6 - ACH Colombia",
                ProcessingDate = OperationalDate,
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(20, 0, 0),
                ClearingHouseId = 1
            });
            if (!await context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.IsActive))
            {
                context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
                {
                    Id = 999,
                    Term = "NOMINAS",
                    Description = "Nóminas sintéticas",
                    StandardEntryClassCode = "PPD",
                    IsActive = true
                });
            }

            if (includeCfa)
            {
                fixture.Cfa = Institution("CFA CANONICA", "00001", "006", isDefault: true);
                context.FinancialInstitutions.Add(fixture.Cfa);
            }

            if (includeExternal)
            {
                fixture.External = Institution(
                    FinancialInstitutionSeeder.SyntheticAchExternalName,
                    FinancialInstitutionSeeder.SyntheticAchExternalRouting,
                    FinancialInstitutionSeeder.SyntheticAchExternalTransit,
                    isDefault: false);
                context.FinancialInstitutions.Add(fixture.External);
            }

            await context.SaveChangesAsync();
            return fixture;
        }

        public IncomingProcTransaccionesE2eScenarioSetupService CreateService()
            => new(Context, _configuration, _environment);

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
