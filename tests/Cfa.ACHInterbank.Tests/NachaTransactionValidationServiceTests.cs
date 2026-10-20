using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaTransactionValidationServiceTests
{
    [Fact]
    public void Constructor_RejectsUnavailablePrerequisitePolicy_InsteadOfEnablingFallback()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        var holidays = Mock.Of<IBankHoliday>();

        Assert.Throws<ArgumentNullException>(
            () => new NachaTransactionValidationService(context, holidays, null!));
    }

    [Fact]
    public async Task ValidateTransactionsForSendAsync_DelegatesMonetaryDecisionToCanonicalPolicy()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        var policy = new Mock<ITransactionPrerequisitePolicyService>();
        policy
            .Setup(x => x.ValidateForNachaExportAsync(
                It.IsAny<AchTransaction>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPrerequisiteValidationResult(true, "OK", "Válida", null));
        var sut = new NachaTransactionValidationService(context, Mock.Of<IBankHoliday>(), policy.Object);
        var transaction = BuildTransaction(TransactionTypeEnum.Credit, "22");

        await sut.ValidateTransactionsForSendAsync([transaction], CancellationToken.None);

        policy.Verify(
            x => x.ValidateForNachaExportAsync(transaction, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateTransactionsForSendAsync_UsesTableDrivenPolicy_ForReversalCodeThatLooksLikeDebit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        var holidays = new Mock<IBankHoliday>();
        holidays.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns([]);
        var policy = new TransactionPrerequisitePolicyService(context, holidays.Object);
        var sut = new NachaTransactionValidationService(context, holidays.Object, policy);
        var transaction = BuildTransaction(TransactionTypeEnum.Reversal, "27");

        await sut.ValidateTransactionsForSendAsync([transaction], CancellationToken.None);
    }

    [Fact]
    public async Task ValidateTransactionsForSendAsync_DirectlyInsertedAchColombiaCycleFiveOrdinaryDebit_BlocksBeforeExportAudit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        await SeedCycleAsync(context, "ACHCOL", "Ciclo 5");
        var policy = new Mock<ITransactionPrerequisitePolicyService>();
        policy.Setup(x => x.ValidateForNachaExportAsync(It.IsAny<AchTransaction>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPrerequisiteValidationResult(true, "OK", "Válida", null));
        var sut = new NachaTransactionValidationService(context, Mock.Of<IBankHoliday>(), policy.Object);
        var transaction = BuildTransaction(TransactionTypeEnum.Debit, "27");
        transaction.AchCycleId = "CYCLE-001";

        var exception = await Assert.ThrowsAsync<NachaGenerationException>(
            () => sut.ValidateTransactionsForSendAsync([transaction], CancellationToken.None));

        Assert.Equal(CycleTransactionPolicy.NotAllowedReasonCode, exception.Code);
        Assert.Contains("Transacción 1", exception.UserMessage);
        Assert.Empty(context.AchFileExports);
        policy.Verify(x => x.ValidateForNachaExportAsync(It.IsAny<AchTransaction>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(TransactionTypeEnum.Credit, false, null, null, 100)]
    [InlineData(TransactionTypeEnum.Debit, true, null, null, 0)]
    [InlineData(TransactionTypeEnum.Debit, false, "R01", "TRACE", 100)]
    [InlineData(TransactionTypeEnum.Debit, true, "R01", "TRACE", 0)]
    public async Task ValidateTransactionsForSendAsync_AchColombiaCycleFiveAllowedOperations_RemainExportable(
        TransactionTypeEnum type,
        bool isPrenotification,
        string? returnReason,
        string? originalTrace,
        decimal amount)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        await SeedCycleAsync(context, "ACHCOL", "Ciclo 5");
        var policy = new Mock<ITransactionPrerequisitePolicyService>();
        policy.Setup(x => x.ValidateForNachaExportAsync(It.IsAny<AchTransaction>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPrerequisiteValidationResult(true, "OK", "Válida", null));
        var sut = new NachaTransactionValidationService(context, Mock.Of<IBankHoliday>(), policy.Object);
        var transaction = BuildTransaction(type, type == TransactionTypeEnum.Credit ? "22" : "27");
        transaction.AchCycleId = "CYCLE-001";
        transaction.IsPrenotification = isPrenotification;
        transaction.ReturnReasonCode = returnReason ?? string.Empty;
        transaction.OriginalTraceRef = originalTrace ?? string.Empty;
        transaction.Amount = amount;

        await sut.ValidateTransactionsForSendAsync([transaction], CancellationToken.None);
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

    private static AchTransaction BuildTransaction(TransactionTypeEnum type, string code)
        => new()
        {
            Id = 1,
            Type = type,
            TransactionCode = code,
            Amount = 100m,
            Reference = "VALIDATION-001",
            TransactionExternalId = "VALIDATION-001",
            SourceAccountNumber = "SOURCE",
            DestinationAccountNumber = "DESTINATION",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = "CYCLE-001",
            AchBatchId = 1,
            EffectiveEntryDate = new DateTime(2026, 7, 14),
            CompanyEntryDescriptionId = 1
        };

    private static async Task SeedCycleAsync(AchDbContext context, string clearingHouseCode, string cycleName)
    {
        var config = new ClearingHouseConfig
        {
            Id = 10,
            ClearingHouseId = 31,
            TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            PaymentRailCode = clearingHouseCode == "ACHCOL" ? "ACH_COLOMBIA" : clearingHouseCode
        };
        context.ClearingHouseConfigs.Add(config);
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 31,
            Code = clearingHouseCode,
            Name = clearingHouseCode,
            OriginCode = "12345678",
            ClearingHouseId = 10
        });
        var cycleConfig = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 31,
            PolicyVersion = "TEST-V1",
            CycleName = cycleName,
            StartTime = new TimeSpan(16, 1, 0),
            EndTime = new TimeSpan(18, 0, 0),
            CutoffTime = new TimeSpan(18, 0, 0),
            OutputReleaseTime = new TimeSpan(19, 0, 0),
            AllowsMonetaryDebit = clearingHouseCode != "ACHCOL",
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true
        };
        context.ClearingHouseCycleConfigs.Add(cycleConfig);
        await context.SaveChangesAsync();
        context.AchCycles.Add(new AchCycle
        {
            Id = "CYCLE-001",
            CycleName = cycleName,
            ProcessingDate = new DateTime(2026, 8, 4),
            StartTime = new TimeSpan(16, 1, 0),
            EndTime = new TimeSpan(18, 0, 0),
            CutoffTime = new TimeSpan(18, 0, 0),
            OutputReleaseTime = new TimeSpan(19, 0, 0),
            ClearingHouseId = 31,
            ClearingHouseCycleConfigId = cycleConfig.Id
        });
        await context.SaveChangesAsync();
    }
}
