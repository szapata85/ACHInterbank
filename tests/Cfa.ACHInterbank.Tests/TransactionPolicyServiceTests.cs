using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class TransactionPolicyServiceTests
{
    [Fact]
    public async Task PreviewAsync_RejectsWhenOutsideCycleWindow()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-outside", DateTime.Today, new TimeSpan(0, 0, 0), new TimeSpan(0, 30, 0));

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object);
        var preview = await service.PreviewAsync(new TransactionPolicyPreviewRequest(
            1000m,
            "REF-001",
            TransactionTypeEnum.Credit,
            AccountTypeEnum.Checking,
            false,
            2,
            "1234567890",
            "9876543210",
            "900123456",
            null));

        Assert.False(preview.CanSubmit);
        Assert.Contains("fuera de la ventana", preview.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_RejectsDuplicateTransactionsWithinCycle()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-open", DateTime.Today, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.AchBatches.Add(new AchBatch
        {
            Id = 1,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = 1,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = DateTime.Today
        });

        context.AchTransactions.Add(new AchTransaction
        {
            Amount = 1500m,
            Reference = "REF-002",
            Type = TransactionTypeEnum.Credit,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            AchCycleId = cycle.Id,
            CompanyIdentification = "900123456",
            CompanyName = "EMPRESA DEMO",
            TransactionCode = "22",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            AchBatchId = 1,
            CompanyEntryDescriptionId = 1
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object);
        var preview = await service.PreviewAsync(new TransactionPolicyPreviewRequest(
            1500m,
            "REF-002",
            TransactionTypeEnum.Credit,
            AccountTypeEnum.Checking,
            false,
            2,
            "1234567890",
            "9876543210",
            "900123456",
            null));

        Assert.False(preview.CanSubmit);
        Assert.True(preview.WouldDuplicate);
    }

    [Fact]
    public void ValidateRequest_RejectsInvalidNaturalPersonIdentity()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var validator = new TransactionValidator(context);

        var request = new AchTransactionRequestData
        {
            Amount = 1000m,
            Reference = "REF-003",
            Type = TransactionTypeEnum.Debit,
            AccountType = AccountTypeEnum.Checking,
            IsPrenotification = false,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1,
            SourcePersonType = "PJ",
            RecipientPersonType = "PN",
            RecipientIdNumber = "ABCD1234",
            RecipientName = "Persona Demo"
        };

        var ex = Assert.Throws<ArgumentException>(() => validator.ValidateRequest(request));
        Assert.Contains("personas naturales", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static TransactionPolicyService CreateService(AchDbContext context, IRoutingStrategyService routing)
    {
        var options = Options.Create(new TransactionPolicyOptions
        {
            Defaults = new TransactionLimitRule
            {
                MaxAmountPerTransaction = 10000000m,
                MaxAmountPerCycle = 50000000m,
                MaxTransactionsPerCycle = 100,
                AllowedAccountTypes = [AccountTypeEnum.Checking, AccountTypeEnum.Savings, AccountTypeEnum.ElectronicDeposits]
            }
        });

        return new TransactionPolicyService(context, routing, options);
    }

    private static void SeedCatalog(AchDbContext context)
    {
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 1,
            Term = "NOMINAS",
            Description = "Pago de nómina",
            StandardEntryClassCode = "PPD",
            IsActive = true
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        context.SaveChanges();
    }

    private static AchCycle SeedCycle(AchDbContext context, string id, DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        var cycle = new AchCycle
        {
            Id = id,
            CycleName = "Ciclo 1",
            ProcessingDate = processingDate,
            StartTime = startTime,
            EndTime = endTime,
            CutoffTime = endTime,
            ClearingHouseId = 1
        };

        context.AchCycles.Add(cycle);
        context.SaveChanges();
        return cycle;
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
