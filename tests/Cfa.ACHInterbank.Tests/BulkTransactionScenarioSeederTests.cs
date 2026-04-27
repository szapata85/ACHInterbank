using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class BulkTransactionScenarioSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesBulkScenarioTransactions_InDevelopment()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedPrerequisites(context);

        var environment = new Mock<IHostEnvironment>();
        environment.SetupProperty(x => x.EnvironmentName, Environments.Development);

        var seeder = new BulkTransactionScenarioSeeder(context, environment.Object);

        await seeder.SeedAsync();

        var references = await context.AchTransactions
            .Where(t => t.Reference.StartsWith("SEED-BULK-"))
            .Select(t => t.Reference)
            .ToListAsync();

        Assert.Contains(references, r => r.StartsWith("SEED-BULK-VALID-"));
        Assert.Contains(references, r => r.StartsWith("SEED-BULK-MIXED-"));
        Assert.Contains(references, r => r.StartsWith("SEED-BULK-VOLUME-"));
        Assert.Contains(references, r => r.StartsWith("SEED-BULK-PARTIAL-EXIST-"));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedPrerequisites(context);

        var environment = new Mock<IHostEnvironment>();
        environment.SetupProperty(x => x.EnvironmentName, Environments.Development);

        var seeder = new BulkTransactionScenarioSeeder(context, environment.Object);

        await seeder.SeedAsync();
        var countAfterFirstRun = await context.AchTransactions.CountAsync(t => t.Reference.StartsWith("SEED-BULK-"));

        await seeder.SeedAsync();
        var countAfterSecondRun = await context.AchTransactions.CountAsync(t => t.Reference.StartsWith("SEED-BULK-"));

        Assert.Equal(countAfterFirstRun, countAfterSecondRun);
    }

    private static void SeedPrerequisites(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            HolidayStrategy = "Colombian"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "00001007",
            ClearingHouseId = 1
        });

        context.AchCycles.Add(new AchCycle
        {
            Id = "CYCLE-SEED",
            CycleName = "Ciclo 1",
            ProcessingDate = DateTime.Today,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 59, 0),
            ClearingHouseId = 1
        });

        context.FinancialInstitutions.AddRange(
            new FinancialInstitution { Id = 1, Name = "Origen", RoutingNumber = "00001", TransitCode = "007" , IsDefaultSource = true, Status = FinancialInstitutionStatus.Active },
            new FinancialInstitution { Id = 2, Name = "Destino 1", RoutingNumber = "00001", TransitCode = "001" , Status = FinancialInstitutionStatus.Active },
            new FinancialInstitution { Id = 3, Name = "Destino 2", RoutingNumber = "00001", TransitCode = "002" , Status = FinancialInstitutionStatus.Active },
            new FinancialInstitution { Id = 4, Name = "Destino 3", RoutingNumber = "00001", TransitCode = "003" , Status = FinancialInstitutionStatus.Active }
        );

        foreach (var institution in context.FinancialInstitutions.Local)
        {
            institution.CalculateCheckDigit();
        }

        context.SaveChanges();
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
