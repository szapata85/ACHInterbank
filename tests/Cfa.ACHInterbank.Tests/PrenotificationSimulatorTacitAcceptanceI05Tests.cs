using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationSimulatorTacitAcceptanceI05Tests
{
    private static readonly DateTimeOffset Deadline = new(2026, 11, 2, 17, 15, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("ACHCOL")]
    [InlineData("CENIT")]
    public void DeadlineBoundary_WaitsBefore_AndAllowsAtExactDeadline(string house)
    {
        Assert.False(PrenotificationTacitAcceptanceDecision.IsDue(Deadline,
            Deadline.AddTicks(-1), true, true, false));
        Assert.True(PrenotificationTacitAcceptanceDecision.IsDue(Deadline,
            Deadline, true, true, false));
        Assert.True(PrenotificationTacitAcceptanceDecision.IsDue(Deadline,
            Deadline.AddMinutes(1), true, true, false));
        Assert.Contains(house, new[] { "ACHCOL", "CENIT" });
    }

    [Fact]
    public void WithoutCanonicalDeadline_OrEffectiveEnabledPolicy_NeverAllowsTacit()
    {
        Assert.False(PrenotificationTacitAcceptanceDecision.IsDue(null, Deadline.AddDays(30), true, true, false));
        Assert.False(PrenotificationTacitAcceptanceDecision.IsDue(Deadline, Deadline, true, false, false));
        Assert.False(PrenotificationTacitAcceptanceDecision.IsDue(Deadline, Deadline, false, true, false));
    }

    [Fact]
    public void GeneratedReturnBlocksTacitEvenAfterDeadline()
        => Assert.False(PrenotificationTacitAcceptanceDecision.IsDue(
            Deadline, Deadline.AddDays(1), true, true, true));

    [Fact]
    public void NoReturnContractDoesNotDeclareReturnFileOrCause()
    {
        var request = new PrenotificationSimulationContractRequest
        {
            Outcome = PrenotificationSimulationOutcome.NoReturnByDeadline,
            ClearingHouseCode = "CENIT", OriginalPrenotificationIds = [42],
            OriginalCycleId = "CENIT-CYCLE-01", BusinessDate = new DateOnly(2026, 11, 2),
            RespondingFinancialInstitutionId = 5
        };
        Assert.True(PrenotificationSimulationContract.Validate(request).ShapeValid);
        request.ResponseCycleId = "CENIT-CYCLE-02";
        Assert.Contains(PrenotificationSimulationContract.Validate(request).Violations,
            x => x.Code == "NO_RETURN_HAS_NO_RESPONSE_CYCLE");
        request.ResponseCycleId = null;
        request.ReasonCode = "R04";
        Assert.Contains(PrenotificationSimulationContract.Validate(request).Violations,
            x => x.Code == "NO_RETURN_HAS_NO_REASON");
    }
}

public partial class NachaInboundSimulatorTests
{
    [Fact]
    public async Task I05_ApplyWithoutExplicitUatHost_IsFailClosed()
    {
        await using var context = CreateContext();
        Seed(context);
        var simulator = CreateService(context, CreateTempOutput());
        var request = new PrenotificationSimulationContractRequest
        {
            Outcome = PrenotificationSimulationOutcome.NoReturnByDeadline,
            ClearingHouseCode = "ACHCOL", OriginalCycleId = "ACH-CYCLE",
            OriginalPrenotificationIds = [1001], BusinessDate = new DateOnly(2026, 5, 20),
            RespondingFinancialInstitutionId = 2
        };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => simulator.ApplyDueTacitAcceptanceAsync(request, "uat"));
        Assert.Contains("TACIT_EXECUTION_DISABLED", error.Message);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Theory]
    [InlineData("ACHCOL", "ACH-CYCLE", 1, 2, 1001)]
    [InlineData("CENIT", "CENIT-CYCLE", 2, 3, 1002)]
    public async Task I05_VirtualClockPreview_IsReadOnlyForBothHouses(
        string house, string cycleId, int houseId, int recipientId, int id)
    {
        await using var context = CreateContext();
        Seed(context);
        var original = TestTransaction(id, cycleId, houseId,
            TransactionTypeEnum.Prenotification, 1, recipientId);
        original.IsPrenotification = true;
        original.Amount = 0;
        original.TransactionCode = "23";
        context.AchTransactions.Add(original);
        var rule = new Cfa.ACHInterbank.Domain.Models.ACH.ClearingHouseTransactionRule
        {
            ClearingHouseId = houseId,
            TransactionNature = TransactionNature.Credit,
            TransactionType = TransactionTypeEnum.Credit,
            TacitAcceptanceEnabled = true,
            EffectiveFrom = new DateTime(2026, 1, 1), IsActive = true
        };
        var account = new Cfa.ACHInterbank.Domain.Models.ACH.ThirdPartyAccount
        {
            ThirdParty = new Cfa.ACHInterbank.Domain.Models.ACH.ThirdParty
            {
                Customer = new Cfa.ACHInterbank.Domain.Models.ACH.Customer
                {
                    FirstName = "Uat", LastName = "Prenote", PersonType = "PJ",
                    DocumentType = "NIT", DocumentNumber = $"9001{id}"
                },
                PersonType = "PJ", DocumentType = "NIT", DocumentNumberNormalized = $"9001{id}",
                DisplayName = "Cuenta UAT"
            },
            FinancialInstitutionId = recipientId,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = $"12345{id}"
        };
        var validation = new Cfa.ACHInterbank.Domain.Models.ACH.ThirdPartyAccountValidation
        {
            ThirdPartyAccount = account, ClearingHouseId = houseId, PolicyRule = rule,
            TransactionNature = TransactionNature.Credit,
            Status = ThirdPartyAccountValidationStatus.PendingResponse,
            EffectiveDate = new DateTime(2026, 5, 20)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();
        var deadline = new DateTimeOffset(2026, 5, 20, 23, 0, 0, TimeSpan.Zero);
        var attempt = new Cfa.ACHInterbank.Domain.Models.ACH.PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id, AchTransactionId = id,
            AttemptNumber = 1, IdempotencyKey = $"i05-{id}",
            Status = PrenotificationValidationAttemptStatus.AwaitingResponse,
            Resolution = PrenotificationValidationResolution.None,
            SourceCycleId = cycleId,
            SentAtUtc = deadline.AddHours(-2), ResponseDeadlineAtUtc = deadline
        };
        context.PrenotificationValidationAttempts.Add(attempt);
        await context.SaveChangesAsync();
        validation.CurrentAttemptId = attempt.Id;
        context.AchFileExports.Add(new Cfa.ACHInterbank.Domain.Models.ACH.AchFileExport
        {
            AchCycleId = cycleId, ClearingHouseId = houseId,
            ExportKind = "NACHA", FileName = "i05-uat.nacha",
            LifecycleStatus = AchFileExportLifecycleStatus.Transmitted,
            Transactions =
            [
                new Cfa.ACHInterbank.Domain.Models.ACH.AchFileExportTransaction
                {
                    AchTransactionId = id, AchCycleId = cycleId, AchBatchId = houseId,
                    TraceNumber = original.TraceNumber, Amount = 0,
                    IncludedAtUtc = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var clock = new Cfa.ACHInterbank.Tests.TestSupport.FixedTimeProvider(
            deadline.AddMinutes(-30), TimeZoneInfo.Utc);
        var simulator = new Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.NachaInboundSimulationService(
            context, Microsoft.Extensions.Options.Options.Create(
                new Cfa.ACHInterbank.Application.ACH.Configuration.NachaInboundSimulatorOptions
                {
                    Enabled = true, Mode = "UAT", DifferentialResponsesEnabled = true,
                    AllowedClearingHouses = ["ACHCOL", "CENIT"]
                }), timeProvider: clock);
        var contract = new PrenotificationSimulationContractRequest
        {
            Outcome = PrenotificationSimulationOutcome.NoReturnByDeadline,
            ClearingHouseCode = house, OriginalPrenotificationIds = [id],
            OriginalCycleId = cycleId, BusinessDate = new DateOnly(2026, 5, 20),
            RespondingFinancialInstitutionId = recipientId
        };
        var before = await simulator.PreviewTacitAcceptanceAsync(new()
        {
            Contract = contract, EvaluationTimeUtc = deadline.AddTicks(-1)
        });
        var after = await simulator.PreviewTacitAcceptanceAsync(new()
        {
            Contract = contract, EvaluationTimeUtc = deadline
        });
        Assert.False(Assert.Single(before.Items).WouldApplyAtEvaluationTime);
        Assert.True(Assert.Single(after.Items).WouldApplyAtEvaluationTime);
        Assert.False(Assert.Single(after.Items).EligibleAtRealTime);
        Assert.True(after.ReadOnly);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
        Assert.Equal(AchTransferStateEnum.Pending,
            (await context.AchTransactions.SingleAsync(x => x.Id == id)).State);
    }
}
