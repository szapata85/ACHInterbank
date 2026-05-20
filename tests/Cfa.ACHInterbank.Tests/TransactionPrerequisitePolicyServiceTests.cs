using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class TransactionPrerequisitePolicyServiceTests
{
    [Fact]
    public async Task ValidateForNachaExportAsync_AchColombiaDebitWithoutPrenote_ReturnsPrerequisiteFailure()
    {
        using var context = CreateContext();
        var cycle = await SeedCycleAsync(context, clearingHouseId: 1, "ACH Colombia");
        SeedRule(context, 1, TransactionNature.Debit, TransactionTypeEnum.Debit, PrenotificationRequirementMode.Mandatory, requiresPrenotification: true);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var transaction = BuildTransaction(cycle, TransactionTypeEnum.Debit);

        var result = await service.ValidateForNachaExportAsync(transaction, prenotificationDate: null, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("NACHA_EXPORT_PREREQUISITE_FAILED", result.Code);
    }

    [Fact]
    public async Task ValidateForNachaExportAsync_AchColombiaCreditWithoutPrenote_ReturnsValid()
    {
        using var context = CreateContext();
        var cycle = await SeedCycleAsync(context, clearingHouseId: 1, "ACH Colombia");
        SeedRule(context, 1, TransactionNature.Credit, TransactionTypeEnum.Credit, PrenotificationRequirementMode.Optional, requiresPrenotification: false);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var transaction = BuildTransaction(cycle, TransactionTypeEnum.Credit);

        var result = await service.ValidateForNachaExportAsync(transaction, prenotificationDate: null, CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Equal("OK", result.Code);
    }

    [Fact]
    public async Task ValidateForNachaExportAsync_CenitWithoutConfiguredRule_ReturnsRuleNotConfigured()
    {
        using var context = CreateContext();
        var cycle = await SeedCycleAsync(context, clearingHouseId: 2, "CENIT");

        var service = CreateService(context);
        var transaction = BuildTransaction(cycle, TransactionTypeEnum.Debit);

        var result = await service.ValidateForNachaExportAsync(transaction, prenotificationDate: null, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal("NACHA_EXPORT_RULE_NOT_CONFIGURED", result.Code);
    }

    [Fact]
    public async Task PreviewAsync_ConfiguredCreditRule_ReportsOptionalPrenotification()
    {
        using var context = CreateContext();
        await SeedCycleAsync(context, clearingHouseId: 1, "ACH Colombia");
        SeedRule(context, 1, TransactionNature.Credit, TransactionTypeEnum.Credit, PrenotificationRequirementMode.Optional, requiresPrenotification: false);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.PreviewAsync(new TransactionPrerequisitePreviewRequest(
            ClearingHouseId: 1,
            TransactionType: TransactionTypeEnum.Credit,
            EffectiveEntryDate: new DateTime(2026, 1, 15),
            AppliesToNachaExport: true), CancellationToken.None);

        Assert.True(result.RuleConfigured);
        Assert.Equal(PrenotificationRequirementMode.Optional, result.PrenotificationMode);
        Assert.Equal("PRENOTIFICATION_OPTIONAL", result.Decision);
    }

    private static AchDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<AchCycle> SeedCycleAsync(AchDbContext context, int clearingHouseId, string clearingHouseName)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = clearingHouseId,
            HolidayStrategy = "Colombian"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = clearingHouseId,
            Code = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? "CENIT" : "ACH",
            Name = clearingHouseName,
            OriginCode = "000101006",
            ClearingHouseId = clearingHouseId
        });

        var cycle = new AchCycle
        {
            Id = $"cycle-{clearingHouseId}",
            CycleName = "CICLO-UAT",
            ProcessingDate = new DateTime(2026, 1, 15),
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 59, 0),
            ClearingHouseId = clearingHouseId,
            ClearingHouseCycleConfigId = null
        };

        context.AchCycles.Add(cycle);
        await context.SaveChangesAsync();
        return cycle;
    }

    private static void SeedRule(
        AchDbContext context,
        int clearingHouseId,
        TransactionNature nature,
        TransactionTypeEnum transactionType,
        PrenotificationRequirementMode mode,
        bool requiresPrenotification)
    {
        context.ClearingHouseTransactionRules.Add(new ClearingHouseTransactionRule
        {
            ClearingHouseId = clearingHouseId,
            TransactionNature = nature,
            TransactionType = transactionType,
            RequiresPrenotification = requiresPrenotification,
            PrenotificationMode = mode,
            RequiresReceiverIdentificationValidation = requiresPrenotification,
            ReceiverIdentificationValidationMode = requiresPrenotification
                ? ValidationRequirementMode.Mandatory
                : ValidationRequirementMode.Optional,
            AppliesToNachaExport = true,
            AppliesToMonetaryTransactions = true,
            EffectiveFrom = new DateTime(2025, 1, 1),
            IsActive = true,
            NormativeSource = "UAT norma sintetica",
            NormativeReference = "UAT-REF",
            Notes = "Prueba automatizada"
        });
    }

    private static AchTransaction BuildTransaction(AchCycle cycle, TransactionTypeEnum type)
        => new()
        {
            Id = 99,
            Type = type,
            Amount = 1000m,
            Reference = "UAT-RULE-001",
            TransactionExternalId = "UAT-RULE-001",
            TransactionCode = type == TransactionTypeEnum.Credit ? "22" : "27",
            SourceAccountNumber = "0000001001",
            DestinationAccountNumber = "0000001002",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            CompanyEntryDescriptionId = 1,
            CompanyName = "Cliente UAT Sintetico",
            CompanyIdentification = "900000001",
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            EffectiveEntryDate = new DateTime(2026, 1, 15)
        };

    private static TransactionPrerequisitePolicyService CreateService(AchDbContext context)
    {
        var holidays = new Mock<IBankHoliday>();
        holidays.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns([]);
        return new TransactionPrerequisitePolicyService(context, holidays.Object);
    }
}
