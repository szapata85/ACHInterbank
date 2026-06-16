using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaFileNamingRuleSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesTwoActiveRules_AndReRunningDoesNotDuplicate()
    {
        await using var harness = await CreateHarnessAsync();
        var seeder = new NachaFileNamingRuleSeeder(harness.Context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var rules = await harness.Context.NachaFileNamingRules
            .AsNoTracking()
            .OrderBy(x => x.ClearingHouseId)
            .ToListAsync();

        Assert.Equal(2, rules.Count);
        Assert.All(rules, rule =>
        {
            Assert.True(rule.IsActive);
            Assert.Equal(NachaFileDirection.Outbound, rule.FileDirection);
            Assert.Equal("RRRRTTT.ZZZ.N", rule.NamePattern);
            Assert.Equal(".ach", rule.Extension);
            Assert.Equal(1, rule.DailySequenceMin);
            Assert.Equal(36, rule.DailySequenceMax);
        });

        var defaultSource = await harness.Context.FinancialInstitutions.SingleAsync(x => x.IsDefaultSource);
        Assert.All(rules, rule => Assert.Equal(defaultSource.Id, rule.SourceFinancialInstitutionId));
    }

    [Fact]
    public async Task NachaFileNamingRuleService_ResolvesAchAndCenitOutboundRules()
    {
        await using var harness = await CreateHarnessAsync();
        await new NachaFileNamingRuleSeeder(harness.Context).SeedAsync();

        var service = new NachaFileNamingRuleService(harness.Context);
        var processingDate = new DateTime(2026, 06, 06);

        var achRule = await service.GetActiveOutboundRuleAsync(1, processingDate);
        var cenitRule = await service.GetActiveOutboundRuleAsync(2, processingDate);

        Assert.NotNull(achRule);
        Assert.NotNull(cenitRule);
        Assert.Equal("RRRRTTT.ZZZ.N", achRule!.NamePattern);
        Assert.Equal("RRRRTTT.ZZZ.N", cenitRule!.NamePattern);
        Assert.Equal("8765321", achRule.OriginEntityCode);
        Assert.Equal("8765321", cenitRule.OriginEntityCode);
        Assert.Equal(harness.DefaultSourceId, achRule.SourceFinancialInstitutionId);
        Assert.Equal(harness.DefaultSourceId, cenitRule.SourceFinancialInstitutionId);
    }

    [Fact]
    public async Task ReturnOut_CanUseOfficialOutboundRuleWithoutClearingHouseFallback()
    {
        await using var harness = await CreateHarnessAsync();
        await new NachaFileNamingRuleSeeder(harness.Context).SeedAsync();

        var sequence = CreateSequenceService(harness.Context);
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, new FakeIdentifierMapService(), namingRuleService);

        var result = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACHCOL",
            ClearingHouseOriginCode = "0000000",
            ProcessingDate = new DateTime(2026, 06, 06),
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal("8765321.001.RET", result.FullName);
        Assert.Equal("8765321", result.Prefix);
        Assert.Equal(1, result.ExternalSequence);
    }

    [Fact]
    public async Task SeedAsync_Fails_Controlled_WhenDefaultSourceIsMissing()
    {
        await using var harness = await CreateHarnessAsync(includeDefaultSource: false);
        var seeder = new NachaFileNamingRuleSeeder(harness.Context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());

        Assert.Contains("FinancialInstitution.IsDefaultSource=true", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ExternalFileNameSequenceService CreateSequenceService(AchDbContext context)
    {
        var resolver = new ExternalFileNameSequenceProviderResolver([new EfGenericExternalFileNameSequenceService(context)]);
        return new ExternalFileNameSequenceService(context, resolver);
    }

    private static async Task<SeederHarness> CreateHarnessAsync(bool includeDefaultSource = true)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            ClearingHouseId = 1,
            HolidayStrategy = "Colombian"
        });
        await context.SaveChangesAsync();

        context.ClearingHouses.AddRange(
            new ClearingHouse
            {
                Id = 1,
                Name = "ACH Colombia",
                Code = "ACHCOL",
                OriginCode = "000101006",
                ClearingHouseId = 1
            },
            new ClearingHouse
            {
                Id = 2,
                Name = "CENIT",
                Code = "CENIT",
                OriginCode = "011111111",
                ClearingHouseId = 1
            });
        await context.SaveChangesAsync();

        if (includeDefaultSource)
        {
            var source = new FinancialInstitution
            {
                Id = 34,
                Name = "Cooperativa Financiera de Antioquia",
                RoutingNumber = "98765",
                TransitCode = "321",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            source.CalculateCheckDigit();
            context.FinancialInstitutions.Add(source);
            await context.SaveChangesAsync();
        }

        var defaultSourceId = await context.FinancialInstitutions
            .Where(x => x.IsDefaultSource)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync();

        return new SeederHarness(connection, context, defaultSourceId);
    }

    private sealed class FakeIdentifierMapService : INachaFileIdentifierMapService
    {
        public Task<char> ResolveIdentifierAsync(int sequence, CancellationToken ct = default)
        {
            if (sequence is < 1 or > 36)
            {
                throw new InvalidOperationException("Sequence out of range.");
            }

            return Task.FromResult(sequence <= 26 ? (char)('A' + (sequence - 1)) : (char)('0' + (sequence - 27)));
        }
    }

    private sealed class SeederHarness : IAsyncDisposable
    {
        public SeederHarness(SqliteConnection connection, AchDbContext context, int? defaultSourceId)
        {
            Connection = connection;
            Context = context;
            DefaultSourceId = defaultSourceId;
        }

        public SqliteConnection Connection { get; }
        public AchDbContext Context { get; }
        public int? DefaultSourceId { get; }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
