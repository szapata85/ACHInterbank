using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class TransactionPolicyServiceTests
{
    private const int TestClearingHouseConfigId = 9301;
    private const int TestClearingHouseId = 9301;
    private const int TestSourceInstitutionId = 9401;
    private const int TestDestinationInstitutionId = 9402;
    private static readonly DateTime BogotaProcessingDate = new(2026, 8, 3);
    private static readonly DateTimeOffset UtcInstantAfterDateBoundary = new(2026, 8, 4, 1, 15, 0, TimeSpan.Zero);

    [Fact]
    public async Task PreviewAsync_RejectsWhenOutsideCycleWindow()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-outside", BogotaProcessingDate.AddDays(1), new TimeSpan(0, 0, 0), new TimeSpan(0, 30, 0));

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(TestDestinationInstitutionId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object, CreateFixedClock());
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
    public async Task PreviewAsync_RejectsDuplicateTransactionsWithinCycle_UtcDateAlreadyAdvancedWhileBogotaStillPreviousDate_UsesBogotaOperationalTime()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-open", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.AchBatches.Add(new AchBatch
        {
            Id = 1,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = BogotaProcessingDate
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
            EffectiveEntryDate = BogotaProcessingDate
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(TestDestinationInstitutionId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object, CreateFixedClock());
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
        Assert.Equal(new DateTime(2026, 8, 4), UtcInstantAfterDateBoundary.UtcDateTime.Date);
        Assert.Equal(BogotaProcessingDate, cycle.ProcessingDate.Date);
    }

    [Fact]
    public async Task PreviewAsync_CurrentContractReturnsDuplicateMessageAndSyntheticKey()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-contract", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.AchBatches.Add(new AchBatch
        {
            Id = 3,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = BogotaProcessingDate
        });

        context.AchTransactions.Add(new AchTransaction
        {
            Amount = 1500m,
            TransactionExternalId = string.Empty,
            Reference = "REF-CONTRACT-001",
            Type = TransactionTypeEnum.Credit,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            AchCycleId = cycle.Id,
            CompanyIdentification = "900123456",
            CompanyName = "EMPRESA DEMO",
            TransactionCode = "22",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            AchBatchId = 3,
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourceInstitutionId = TestSourceInstitutionId,
            DestinationInstitutionId = TestDestinationInstitutionId,
            EffectiveEntryDate = BogotaProcessingDate
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(TestDestinationInstitutionId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object, CreateFixedClock());
        var preview = await service.PreviewAsync(new TransactionPolicyPreviewRequest(
            1500m,
            null,
            "REF-CONTRACT-001",
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
        Assert.Equal("Ya existe una transacción equivalente para el mismo ciclo.", preview.Message);
        Assert.Equal($"{cycle.Id}:Credit:1234567890:9876543210:1500:REF-CONTRACT-001", preview.IdempotencyKey);
    }

    [Fact]
    public async Task PreviewAsync_RegulatoryDeny_HasAbsolutePrecedence_AndDoesNotEvaluateCfaRestriction()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-reg-deny", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        var routing = RouteTo(cycle.Id);

        var regulation = new Mock<ICycleTransactionPolicy>();
        regulation.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CycleTransactionPolicyResult(
                false,
                "REG-DENY",
                "La regulación rechaza la clase funcional.",
                "ACH",
                1,
                "MonetaryDebit"));

        var cfa = new Mock<ICfaOperationalTransactionRestrictionResolver>(MockBehavior.Strict);
        var service = CreateService(context, routing.Object, CreateFixedClock(), cfa.Object, regulation.Object);

        var preview = await service.PreviewAsync(Request(
            amount: 1_000m,
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.Checking,
            reference: "REG-DENY-001"));

        Assert.False(preview.CanSubmit);
        Assert.Contains("REG-DENY", preview.Message);
        cfa.Verify(x => x.ResolveAsync(It.IsAny<CfaOperationalTransactionRestrictionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreviewAsync_RegulatoryAllow_ThenCfaOperationalDeny_Rejects()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-cfa-deny", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseCycleConfigId = cycle.ClearingHouseCycleConfigId,
            TransactionType = TransactionTypeEnum.Debit,
            AllowedAccountTypes = [AccountTypeEnum.Checking],
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 1_000m,
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.ElectronicDeposits,
            reference: "CFA-DENY-001"));

        Assert.False(preview.CanSubmit);
        Assert.Contains("bloqueado", preview.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_InheritsGlobalLimits()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-global-limit", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());

        var preview = await service.PreviewAsync(Request(
            amount: 10_000_001m,
            type: TransactionTypeEnum.Credit,
            accountType: AccountTypeEnum.Checking,
            reference: "GLOBAL-LIMIT-001"));

        Assert.False(preview.CanSubmit);
        Assert.Equal(10_000_000m, preview.MaxAmountPerTransaction);
        Assert.Contains("límite por transacción", preview.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_ClearingHouseOverride_OverridesOnlyDefinedFields()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-house-override", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseId = TestClearingHouseId,
            TransactionType = TransactionTypeEnum.Credit,
            MaxAmountPerTransaction = 2_000_000m,
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 1_500_000m,
            type: TransactionTypeEnum.Credit,
            accountType: AccountTypeEnum.Checking,
            reference: "HOUSE-OVERRIDE-001"));

        Assert.True(preview.CanSubmit);
        Assert.Equal(2_000_000m, preview.MaxAmountPerTransaction);
        Assert.Equal(50_000_000m, preview.RemainingAmountForCycle);
        Assert.Equal(100, preview.RemainingTransactionsForCycle);
    }

    [Fact]
    public async Task PreviewAsync_CycleOverride_OverridesOnlyDefinedFields()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-cycle-override", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseCycleConfigId = cycle.ClearingHouseCycleConfigId,
            MaxAmountPerTransaction = 1_000_000m,
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 500_000m,
            type: TransactionTypeEnum.Credit,
            accountType: AccountTypeEnum.Checking,
            reference: "CYCLE-OVERRIDE-001"));

        Assert.True(preview.CanSubmit);
        Assert.Equal(1_000_000m, preview.MaxAmountPerTransaction);
        Assert.Equal(50_000_000m, preview.RemainingAmountForCycle);
        Assert.Equal(100, preview.RemainingTransactionsForCycle);
    }

    [Fact]
    public async Task PreviewAsync_AllowedAccountTypes_IsExplicitVeto_WithoutFallback()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-account-veto", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseCycleConfigId = cycle.ClearingHouseCycleConfigId,
            TransactionType = TransactionTypeEnum.Debit,
            AllowedAccountTypes = [AccountTypeEnum.Checking, AccountTypeEnum.Savings],
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 500m,
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.ElectronicDeposits,
            reference: "ACCOUNT-VETO-001"));

        Assert.False(preview.CanSubmit);
        Assert.Contains("bloqueado", preview.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_HistoricalCycle5_UsesRestrictionBoundToHistoricalCycleConfig()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var historical = SeedCycle(
            context,
            "cycle-historical-c5",
            BogotaProcessingDate,
            TimeSpan.Zero,
            new TimeSpan(23, 59, 0),
            "Ciclo 5",
            RegulatoryCycleScheduleCatalog.HistoricalBootstrapPolicyVersion);
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseCycleConfigId = historical.ClearingHouseCycleConfigId,
            TransactionType = TransactionTypeEnum.Debit,
            AllowedAccountTypes = [AccountTypeEnum.Checking, AccountTypeEnum.Savings],
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(historical.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 500m,
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.ElectronicDeposits,
            reference: "HIST-C5-001"));

        Assert.False(preview.CanSubmit);
        Assert.Contains("bloqueado", preview.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_FuturePolicyVersion_DoesNotInheritHistoricalCycle5Restriction()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var historical = SeedCycle(
            context,
            "cycle-historical-c5-isolation",
            BogotaProcessingDate,
            TimeSpan.Zero,
            new TimeSpan(23, 59, 0),
            "Ciclo 5",
            RegulatoryCycleScheduleCatalog.HistoricalBootstrapPolicyVersion);
        var future = SeedCycle(
            context,
            "cycle-future-c5-isolation",
            BogotaProcessingDate,
            TimeSpan.Zero,
            new TimeSpan(23, 59, 0),
            "Ciclo 5",
            "ACHCOL-FUTURE");
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseCycleConfigId = historical.ClearingHouseCycleConfigId,
            TransactionType = TransactionTypeEnum.Debit,
            AllowedAccountTypes = [AccountTypeEnum.Checking, AccountTypeEnum.Savings],
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(future.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 500m,
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.ElectronicDeposits,
            reference: "FUTURE-C5-001"));

        Assert.True(preview.CanSubmit);
        Assert.Equal(10_000_000m, preview.MaxAmountPerTransaction);
    }

    [Fact]
    public async Task PreviewAsync_InheritedGlobalCycleAmountAndCountLimits_RemainEnforced()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-aggregate-limits", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseCycleConfigId = cycle.ClearingHouseCycleConfigId,
            TransactionType = TransactionTypeEnum.Credit,
            MaxAmountPerTransaction = 20_000_000m,
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        context.AchBatches.Add(new AchBatch
        {
            Id = 404,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = BogotaProcessingDate
        });
        for (var i = 1; i <= 100; i++)
        {
            context.AchTransactions.Add(new AchTransaction
            {
                Amount = 100m,
                TransactionExternalId = $"LIMIT-{i:000}",
                Reference = $"LIMIT-{i:000}",
                Type = TransactionTypeEnum.Credit,
                SourceAccountNumber = $"12345{i:00000}",
                DestinationAccountNumber = $"98765{i:00000}",
                AchCycleId = cycle.Id,
                CompanyIdentification = "900123456",
                CompanyName = "EMPRESA DEMO",
                TransactionCode = "22",
                OriginatingDFI = "123456780",
                ReceivingDFI = "765432100",
                AchBatchId = 404,
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                SourceInstitutionId = TestSourceInstitutionId,
                DestinationInstitutionId = TestDestinationInstitutionId,
                EffectiveEntryDate = BogotaProcessingDate
            });
        }
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 1_000m,
            type: TransactionTypeEnum.Credit,
            accountType: AccountTypeEnum.Checking,
            reference: "COUNT-LIMIT-001"));

        Assert.False(preview.CanSubmit);
        Assert.Contains("máximo de 100", preview.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewAsync_InheritedGlobalMaxAmountPerCycle_RemainsEnforced()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-amount-limit", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
        {
            ClearingHouseCycleConfigId = cycle.ClearingHouseCycleConfigId,
            TransactionType = TransactionTypeEnum.Credit,
            MaxAmountPerTransaction = 20_000_000m,
            EffectiveFrom = DateTime.UnixEpoch,
            IsActive = true
        });
        context.AchBatches.Add(new AchBatch
        {
            Id = 405,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = BogotaProcessingDate
        });
        context.AchTransactions.Add(new AchTransaction
        {
            Amount = 49_999_500m,
            TransactionExternalId = "AMOUNT-EXISTING",
            Reference = "AMOUNT-EXISTING",
            Type = TransactionTypeEnum.Credit,
            SourceAccountNumber = "12345000001",
            DestinationAccountNumber = "98765000001",
            AchCycleId = cycle.Id,
            CompanyIdentification = "900123456",
            CompanyName = "EMPRESA DEMO",
            TransactionCode = "22",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            AchBatchId = 405,
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourceInstitutionId = TestSourceInstitutionId,
            DestinationInstitutionId = TestDestinationInstitutionId,
            EffectiveEntryDate = BogotaProcessingDate
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cycleTransactionPolicy: AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 1_000m,
            type: TransactionTypeEnum.Credit,
            accountType: AccountTypeEnum.Checking,
            reference: "AMOUNT-LIMIT-001"));

        Assert.False(preview.CanSubmit);
        Assert.Contains("cupo suficiente", preview.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(500m, preview.RemainingAmountForCycle);
    }

    [Fact]
    public async Task PreviewAsync_PrenotificationFlag_IsPassedToOperationalRestrictionResolver()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        var cycle = SeedCycle(context, "cycle-prenote", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        var cfa = new Mock<ICfaOperationalTransactionRestrictionResolver>();
        cfa.Setup(x => x.ResolveAsync(
                It.Is<CfaOperationalTransactionRestrictionRequest>(r => r.IsPrenotification && r.ClearingHouseCycleConfigId == cycle.ClearingHouseCycleConfigId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowRestriction());

        var service = CreateService(context, RouteTo(cycle.Id).Object, CreateFixedClock(), cfa.Object, AlwaysAllowRegulation());
        var preview = await service.PreviewAsync(Request(
            amount: 0m,
            type: TransactionTypeEnum.Debit,
            accountType: AccountTypeEnum.Checking,
            reference: "PRENOTE-001",
            isPrenotification: true));

        Assert.True(preview.CanSubmit);
        cfa.VerifyAll();
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
        var cycle = SeedCycle(context, "cycle-opid", BogotaProcessingDate, TimeSpan.Zero, new TimeSpan(23, 59, 0));
        context.AchBatches.Add(new AchBatch
        {
            Id = 2,
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = BogotaProcessingDate
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
            EffectiveEntryDate = BogotaProcessingDate
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(TestDestinationInstitutionId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycle.Id);

        var service = CreateService(context, routing.Object, CreateFixedClock());
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

    private static TransactionPolicyService CreateService(
        AchDbContext context,
        IRoutingStrategyService routing,
        TimeProvider timeProvider,
        ICfaOperationalTransactionRestrictionResolver? restrictionResolver = null,
        ICycleTransactionPolicy? cycleTransactionPolicy = null)
    {
        restrictionResolver ??= new CfaOperationalTransactionRestrictionResolver(context);
        return new TransactionPolicyService(
            context,
            routing,
            restrictionResolver,
            timeProvider,
            cycleTransactionPolicy: cycleTransactionPolicy);
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
                HolidayStrategy = "Colombian",
                TimeZoneId = "America/Bogota"
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

        if (!context.CfaOperationalTransactionRestrictions.Any())
        {
            context.CfaOperationalTransactionRestrictions.Add(new CfaOperationalTransactionRestriction
            {
                MaxAmountPerTransaction = 10_000_000m,
                MaxAmountPerCycle = 50_000_000m,
                MaxTransactionsPerCycle = 100,
                AllowedAccountTypes =
                [
                    AccountTypeEnum.Checking,
                    AccountTypeEnum.Savings,
                    AccountTypeEnum.ElectronicDeposits
                ],
                EffectiveFrom = DateTime.UnixEpoch,
                IsActive = true
            });
        }

        context.SaveChanges();
        return companyEntryDescriptionId;
    }

    private static AchCycle SeedCycle(
        AchDbContext context,
        string id,
        DateTime processingDate,
        TimeSpan startTime,
        TimeSpan endTime,
        string cycleName = "Ciclo 1",
        string policyVersion = "TEST-V1")
    {
        var config = new ClearingHouseCycleConfig
        {
            ClearingHouseId = TestClearingHouseId,
            PolicyVersion = policyVersion,
            CycleName = cycleName,
            StartTime = startTime,
            CutoffTime = endTime,
            EndTime = endTime,
            OutputReleaseTime = endTime,
            EffectiveFrom = processingDate.Date,
            EffectiveTo = processingDate.Date,
            IsActive = true
        };
        context.ClearingHouseCycleConfigs.Add(config);
        context.SaveChanges();

        var cycle = new AchCycle
        {
            Id = id,
            CycleName = cycleName,
            ProcessingDate = processingDate,
            StartTime = startTime,
            EndTime = endTime,
            CutoffTime = endTime,
            OutputReleaseTime = endTime,
            ClearingHouseId = TestClearingHouseId,
            ClearingHouseCycleConfigId = config.Id
        };

        context.AchCycles.Add(cycle);
        context.SaveChanges();
        return cycle;
    }

    private static Mock<IRoutingStrategyService> RouteTo(string cycleId)
    {
        var routing = new Mock<IRoutingStrategyService>();
        routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(
                TestDestinationInstitutionId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cycleId);
        return routing;
    }

    private static ICycleTransactionPolicy AlwaysAllowRegulation()
    {
        var regulation = new Mock<ICycleTransactionPolicy>();
        regulation.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CycleTransactionPolicyResult(
                true,
                "ALLOWED",
                "Permitido por regulación para la prueba de integración.",
                "ACH",
                1,
                "MonetaryCredit"));
        return regulation.Object;
    }

    private static CfaOperationalTransactionRestrictionResolution AllowRestriction(
        decimal? maxAmountPerTransaction = null,
        decimal? maxAmountPerCycle = null,
        int? maxTransactionsPerCycle = null)
        => new(
            false,
            null,
            maxAmountPerTransaction,
            maxAmountPerCycle,
            maxTransactionsPerCycle,
            null,
            Array.Empty<int>());

    private static TransactionPolicyPreviewRequest Request(
        decimal amount,
        TransactionTypeEnum type,
        AccountTypeEnum accountType,
        string reference,
        bool isPrenotification = false)
        => new(
            amount,
            null,
            reference,
            type,
            accountType,
            isPrenotification,
            TestDestinationInstitutionId,
            "1234567890",
            "9876543210",
            "900123456",
            null);

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

    private static FixedTimeProvider CreateFixedClock()
        => new(UtcInstantAfterDateBoundary, TimeZoneInfo.Utc);
}
