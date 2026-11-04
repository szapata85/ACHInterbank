using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationSimulatorEligibilityI02PolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 13, 0, 0, TimeSpan.Zero);
    private static PrenotificationSimulationEligibilityPolicy.Evidence Valid(DateTimeOffset deadline) => new(
        true, true, true, AchTransactionDirection.Outgoing, AchTransactionOrigin.Cfa,
        AchTransferStateEnum.Pending, "000012830000111", "PRE-111", "ACH-CYCLE", "ACHCOL", 2,
        true, 1, true, PrenotificationValidationAttemptStatus.AwaitingResponse,
        ThirdPartyAccountValidationStatus.PendingResponse, PrenotificationValidationResolution.None,
        deadline, false);

    [Fact]
    public void ReturnedOnlyBeforeDeadline_AndNoReturnOnlyAfterDeadline()
    {
        Assert.Empty(PrenotificationSimulationEligibilityPolicy.CheckOutcome(Valid(Now.AddHours(1)), PrenotificationSimulationOutcome.Returned, Now));
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckOutcome(Valid(Now.AddHours(1)), PrenotificationSimulationOutcome.NoReturnByDeadline, Now), x => x.Code == "PRENOTIFICATION_DEADLINE_NOT_REACHED");
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckOutcome(Valid(Now), PrenotificationSimulationOutcome.Returned, Now), x => x.Code == "RETURN_DEADLINE_EXPIRED");
        Assert.Empty(PrenotificationSimulationEligibilityPolicy.CheckOutcome(Valid(Now), PrenotificationSimulationOutcome.NoReturnByDeadline, Now));
    }

    [Fact]
    public void DoesNotInferSendingFromCreation_OrAcceptUnknownDeadline()
    {
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckCommon(Valid(Now) with { TransmittedOrAcknowledged = false }), x => x.Code == "PRENOTIFICATION_NOT_TRANSMITTED");
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckCommon(Valid(Now) with { ResponseDeadlineAtUtc = null }), x => x.Code == "PRENOTIFICATION_DEADLINE_UNKNOWN");
    }

    [Fact]
    public void BlocksRepliedClosedOrIncomingAndNonCurrentAttempt()
    {
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckCommon(Valid(Now) with { HasPriorResponse = true }), x => x.Code == "PRENOTIFICATION_ALREADY_RESPONDED");
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckCommon(Valid(Now) with { TransactionState = AchTransferStateEnum.ReturnedByEpr }), x => x.Code == "PRENOTIFICATION_NOT_PENDING");
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckCommon(Valid(Now) with { Direction = AchTransactionDirection.Incoming }), x => x.Code == "NOT_ORIGINATED_HERE");
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckCommon(Valid(Now) with { CurrentAttemptMatches = false }), x => x.Code == "PRENOTIFICATION_ATTEMPT_NOT_ACTIVE");
        Assert.Contains(PrenotificationSimulationEligibilityPolicy.CheckCommon(Valid(Now) with { ActiveAttemptCount = 2 }), x => x.Code == "PRENOTIFICATION_ATTEMPT_NOT_ACTIVE");
    }
}

public partial class NachaInboundSimulatorTests
{
    [Theory]
    [InlineData("ACHCOL", "ACH-CYCLE", 1, 2, 101)]
    [InlineData("CENIT", "CENIT-CYCLE", 2, 3, 102)]
    public async Task I02_ListAndPreviewAreReadOnly_AndRequireExportedCurrentAttempt(
        string houseCode, string cycleId, int houseId, int destinationId, int transactionId)
    {
        await using var context = CreateContext();
        Seed(context);
        var tx = TestTransaction(transactionId, cycleId, houseId,
            TransactionTypeEnum.Prenotification, 1, destinationId);
        tx.IsPrenotification = true;
        tx.Amount = 0;
        tx.TransactionCode = "23";
        context.AchTransactions.Add(tx);
        await context.SaveChangesAsync();

        var customer = new Customer
        {
            FirstName = "Prueba", LastName = "UAT", PersonType = "PJ",
            DocumentType = "NIT", DocumentNumber = $"800000{transactionId}"
        };
        var party = new ThirdParty
        {
            Customer = customer, PersonType = "PJ", DocumentType = "NIT",
            DocumentNumberNormalized = $"800000{transactionId}", DisplayName = "Cuenta pruebas"
        };
        var account = new ThirdPartyAccount
        {
            ThirdParty = party, FinancialInstitutionId = destinationId,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = $"0000{transactionId}"
        };
        var validation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccount = account, ClearingHouseId = houseId,
            TransactionNature = TransactionNature.Credit,
            Status = ThirdPartyAccountValidationStatus.PendingResponse,
            EffectiveDate = new DateTime(2026, 5, 20)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();
        var attempt = new PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id,
            AchTransactionId = transactionId, AttemptNumber = 1,
            IdempotencyKey = $"prenote-{transactionId}",
            Status = PrenotificationValidationAttemptStatus.AwaitingResponse,
            Resolution = PrenotificationValidationResolution.None,
            SourceCycleId = cycleId,
            SentAtUtc = new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero),
            ResponseDeadlineAtUtc = new DateTimeOffset(2026, 5, 20, 23, 0, 0, TimeSpan.Zero)
        };
        context.PrenotificationValidationAttempts.Add(attempt);
        await context.SaveChangesAsync();
        validation.CurrentAttemptId = attempt.Id;
        context.AchFileExports.Add(new AchFileExport
        {
            AchCycleId = cycleId, ClearingHouseId = houseId, ExportKind = "NACHA",
            FileName = "test-uat.nacha", LifecycleStatus = AchFileExportLifecycleStatus.Transmitted,
            Transactions =
            [
                new AchFileExportTransaction
                {
                    AchTransactionId = transactionId, AchCycleId = cycleId,
                    AchBatchId = houseId, TraceNumber = tx.TraceNumber,
                    Amount = 0, IncludedAtUtc = new DateTime(2026, 5, 20, 13, 0, 0, DateTimeKind.Utc)
                }
            ]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var simulator = CreateService(context, CreateTempOutput());
        var before = await context.PrenotificationValidationAttempts.CountAsync();
        var page = await simulator.ListOriginatedPrenotificationsAsync(new PrenotificationCandidateQuery
        {
            ClearingHouseCode = houseCode, RespondingFinancialInstitutionId = destinationId
        });
        var item = Assert.Single(page.Items);
        Assert.Equal(transactionId, item.OriginalPrenotificationId);
        Assert.True(item.TransmittedOrAcknowledged);
        Assert.Equal(attempt.Id, item.ValidationAttemptId);
        Assert.False(page.ExecutionEnabled);

        var preview = await simulator.PreviewPrenotificationAsync(new PrenotificationSimulationContractRequest
        {
            Outcome = PrenotificationSimulationOutcome.NoReturnByDeadline,
            ClearingHouseCode = houseCode,
            OriginalPrenotificationIds = [transactionId],
            OriginalCycleId = cycleId,
            BusinessDate = new DateOnly(2026, 5, 20),
            RespondingFinancialInstitutionId = destinationId
        });
        Assert.False(preview.ExecutionEnabled);
        Assert.True(preview.BusinessEligible); // preflight favorable por plazo vencido, pero nunca ejecutable
        Assert.Equal(before, await context.PrenotificationValidationAttempts.CountAsync());
        Assert.Empty(await context.NachaInboundSimulations.ToListAsync());
    }

    [Fact]
    public async Task I02_RejectsOtherClearingHouseAndTooManyIdsWithoutWriting()
    {
        await using var context = CreateContext();
        Seed(context);
        var simulator = CreateService(context, CreateTempOutput());
        var invalid = await simulator.PreviewPrenotificationAsync(new PrenotificationSimulationContractRequest
        {
            Outcome = PrenotificationSimulationOutcome.Returned,
            ClearingHouseCode = "ACHCOL",
            OriginalCycleId = "CENIT-CYCLE",
            ResponseCycleId = "CENIT-CYCLE",
            OriginalPrenotificationIds = [10],
            BusinessDate = new DateOnly(2026, 5, 20),
            RespondingFinancialInstitutionId = 3,
            ReasonCode = "R04", IdempotencyKey = "return-i02-test"
        });
        Assert.False(invalid.BusinessEligible);
        Assert.Contains(invalid.Violations, x => x.Code == "ORIGINAL_CYCLE_NOT_FOUND");
        var many = await simulator.PreviewPrenotificationAsync(new PrenotificationSimulationContractRequest
        {
            Outcome = PrenotificationSimulationOutcome.NoReturnByDeadline,
            ClearingHouseCode = "ACHCOL",
            OriginalCycleId = "ACH-CYCLE",
            OriginalPrenotificationIds = Enumerable.Range(1, 101).ToArray(),
            BusinessDate = new DateOnly(2026, 5, 20),
            RespondingFinancialInstitutionId = 2
        });
        Assert.False(many.ShapeValid);
        Assert.Contains(many.Violations, x => x.Code == "PRENOTIFICATION_IDS_LIMIT_EXCEEDED");
        Assert.Empty(await context.NachaInboundSimulations.ToListAsync());
    }
}
