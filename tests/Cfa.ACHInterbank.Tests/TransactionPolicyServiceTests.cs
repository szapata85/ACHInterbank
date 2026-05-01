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
    private const int TestClearingHouseConfigId = 9301;
    private const int TestClearingHouseId = 9301;
    private const int TestSourceInstitutionId = 9401;
    private const int TestDestinationInstitutionId = 9402;

    [Fact]
    public async Task PreviewAsync_RejectsWhenOutsideCycleWindow()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-outside", DateTime.Today.AddDays(1), new TimeSpan(0, 0, 0), new TimeSpan(0, 30, 0));

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(TestDestinationInstitutionId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object);
        var preview = await service.PreviewAsync(new TransactionPolicyPreviewRequest(
            1000m,
            null,
            "REF-001",
            TransactionTypeEnum.Credit,
            AccountTypeEnum.Checking,
            false,
            TestDestinationInstitutionId,
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
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-open", DateTime.Today, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.AchBatches.Add(new AchBatch
        {
            Id = 1,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
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
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourceInstitutionId = TestSourceInstitutionId,
            DestinationInstitutionId = TestDestinationInstitutionId,
            EffectiveEntryDate = DateTime.Today
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(TestDestinationInstitutionId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object);
        var preview = await service.PreviewAsync(new TransactionPolicyPreviewRequest(
            1500m,
            null,
            "REF-002",
            TransactionTypeEnum.Credit,
            AccountTypeEnum.Checking,
            false,
            TestDestinationInstitutionId,
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
        var companyEntryDescriptionId = SeedCatalog(context);
        var validator = new TransactionValidator(context);

        var request = new AchTransactionRequestData
        {
            Amount = 1000m,
            Reference = "REF-003",
            Type = TransactionTypeEnum.Debit,
            AccountType = AccountTypeEnum.Checking,
            IsPrenotification = false,
            DestinationInstitutionId = TestDestinationInstitutionId,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourcePersonType = "PJ",
            RecipientPersonType = "PN",
            RecipientIdNumber = "ABCD1234",
            RecipientName = "Persona Demo"
        };

        var ex = Assert.Throws<ArgumentException>(() => validator.ValidateRequest(request));
        Assert.Contains("personas naturales", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_DetectsDuplicateByTransactionExternalId_WithoutDependingOnLegacyReference()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-opid", DateTime.Today, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.AchBatches.Add(new AchBatch
        {
            Id = 2,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = DateTime.Today
        });

        context.AchTransactions.Add(new AchTransaction
        {
            Amount = 2000m,
            TransactionExternalId = "TX-EXT-777",
            Reference = "LEGACY-ABC",
            Type = TransactionTypeEnum.Credit,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            AchCycleId = cycle.Id,
            CompanyIdentification = "900123456",
            CompanyName = "EMPRESA DEMO",
            TransactionCode = "22",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            AchBatchId = 2,
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourceInstitutionId = TestSourceInstitutionId,
            DestinationInstitutionId = TestDestinationInstitutionId,
            EffectiveEntryDate = DateTime.Today
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(TestDestinationInstitutionId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object);
        var preview = await service.PreviewAsync(new TransactionPolicyPreviewRequest(
            2000m,
            "TX-EXT-777",
            "LEGACY-DISTINTA",
            TransactionTypeEnum.Credit,
            AccountTypeEnum.Checking,
            false,
            TestDestinationInstitutionId,
            "1234567890",
            "9876543210",
            "900123456",
            null));

        Assert.False(preview.CanSubmit);
        Assert.True(preview.WouldDuplicate);
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

    private static int SeedCatalog(AchDbContext context)
    {
        var companyEntryDescriptionId = context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "NOMINAS" && x.IsActive)
            .Select(x => x.Id)
            .First();

        if (!context.ClearingHouseConfigs.Any(x => x.Id == TestClearingHouseConfigId))
        {
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = TestClearingHouseConfigId,
                HolidayStrategy = "Colombian"
            });
        }

        if (!context.ClearingHouses.Any(x => x.Id == TestClearingHouseId))
        {
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = TestClearingHouseId,
                Name = "ACH Colombia",
                Code = "ACH",
                OriginCode = "12345678",
                ClearingHouseId = TestClearingHouseConfigId
            });
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == TestSourceInstitutionId))
        {
            context.FinancialInstitutions.Add(new FinancialInstitution
            {
                Id = TestSourceInstitutionId,
                Name = "Banco Origen",
                RoutingNumber = "12345",
                TransitCode = "678",
                Status = FinancialInstitutionStatus.Active,
                IsDefaultSource = true
            });
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == TestDestinationInstitutionId))
        {
            context.FinancialInstitutions.Add(new FinancialInstitution
            {
                Id = TestDestinationInstitutionId,
                Name = "Banco Destino",
                RoutingNumber = "76543",
                TransitCode = "210",
                Status = FinancialInstitutionStatus.Active,
                IsDefaultSource = false
            });
        }

        foreach (var institution in context.FinancialInstitutions.Local)
        {
            institution.CalculateCheckDigit();
        }

        context.SaveChanges();
        return companyEntryDescriptionId;
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
            ClearingHouseId = TestClearingHouseId
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
