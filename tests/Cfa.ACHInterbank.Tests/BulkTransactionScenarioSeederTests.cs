using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
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

        Assert.False(await context.AchTransactions.AnyAsync(
            x => x.Reference.StartsWith("SEED-BULK-") && x.Type == TransactionTypeEnum.Reversal));
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

    [Fact]
    public async Task SeedAsync_EnsuresMaturePrenotificationForEverySeedDebit()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedPrerequisites(context);

        var environment = new Mock<IHostEnvironment>();
        environment.SetupProperty(x => x.EnvironmentName, Environments.Development);
        var seeder = new BulkTransactionScenarioSeeder(context, environment.Object);

        await seeder.SeedAsync();

        var debits = await context.AchTransactions
            .AsNoTracking()
            .Where(x => x.Reference.StartsWith("SEED-BULK-")
                        && x.Type == TransactionTypeEnum.Debit
                        && !x.IsPrenotification)
            .ToListAsync();
        var prenotifications = await context.AchTransactions
            .AsNoTracking()
            .Where(x => x.Reference.StartsWith("SEED-BULK-PRE-") && x.IsPrenotification)
            .ToListAsync();
        var debitLikeAddendas = await context.AchTransactionAddendas
            .AsNoTracking()
            .Include(x => x.Transaction)
            .ThenInclude(x => x.AchBatch)
            .Where(x => x.Transaction.Reference.StartsWith("SEED-BULK-")
                        && x.BusinessType == AchAddendaBusinessType.Debit)
            .ToListAsync();

        Assert.NotEmpty(debits);
        Assert.Equal(debits.Count, prenotifications.Count);
        Assert.NotEmpty(debitLikeAddendas);
        Assert.All(debitLikeAddendas, addenda =>
        {
            Assert.Equal(addenda.Transaction.CompanyIdentification, addenda.CollectorId);
            Assert.Equal(addenda.Transaction.RecipientIdNumber, addenda.ReceiverCustomerCode);
            Assert.False(string.IsNullOrWhiteSpace(addenda.ServiceDescription));
            Assert.StartsWith("MULTICREDIT", addenda.Transaction.AchBatch.CompanyEntryDescription, StringComparison.Ordinal);
            Assert.True(addenda.Purpose is null || addenda.Purpose.Length <= 10);
        });
        Assert.All(debits, debit =>
        {
            Assert.Equal(8, debit.OriginatingDFI.Length);
            Assert.Equal(8, debit.ReceivingDFI.Length);
            var prenote = Assert.Single(prenotifications, x => x.OriginalTraceRef == debit.TraceNumber);
            Assert.Equal(debit.DestinationInstitutionId, prenote.DestinationInstitutionId);
            Assert.Equal(debit.DestinationAccountNumber, prenote.DestinationAccountNumber);
            Assert.True(prenote.EffectiveEntryDate < debit.EffectiveEntryDate);
            Assert.Equal(0m, prenote.Amount);
            Assert.Equal(
                debit.TransactionCode == "27" ? "28" : "38",
                prenote.TransactionCode);
        });
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
