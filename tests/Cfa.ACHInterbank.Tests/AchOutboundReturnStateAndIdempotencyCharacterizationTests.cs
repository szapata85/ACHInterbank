using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchOutboundReturnStateAndIdempotencyCharacterizationTests
{
    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldPersistAchReturnGenerated_CurrentBehavior()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 1001, cycleId: "ACH-CHAR-1");

        var sut = BuildSut(context, 1001, "DEV14");
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-1", [new ReturnSelectionItemDto(1001, "DEV14")]), CancellationToken.None);

        var generated = await context.Set<AchReturnGenerated>().SingleAsync(x => x.OriginalTransactionId == 1001);
        Assert.Equal(1001, generated.OriginalTransactionId);
        Assert.Equal("DEV14", generated.ReturnReasonCode);
        Assert.Equal("ACH-CHAR-1", generated.ReturnCycleId);
        Assert.Equal(125.55m, generated.Amount);
        Assert.False(string.IsNullOrWhiteSpace(generated.NewSequenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(generated.OriginalSequenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(generated.ReceiverEntityCode));
        Assert.False(string.IsNullOrWhiteSpace(generated.OriginatorEntityCode));
        Assert.False(string.IsNullOrWhiteSpace(generated.FileName));
        Assert.True(generated.GeneratedAtUtc > DateTime.MinValue);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldNotChangeOriginalTransactionState_CurrentBehavior()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 1002, cycleId: "ACH-CHAR-2", state: AchTransferStateEnum.Pending);
        var initialState = await context.AchTransactions.Where(x => x.Id == 1002).Select(x => x.State).SingleAsync();

        var sut = BuildSut(context, 1002, "DEV14");
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-2", [new ReturnSelectionItemDto(1002, "DEV14")]), CancellationToken.None);

        var reloaded = await context.AchTransactions.SingleAsync(x => x.Id == 1002);
        Assert.Equal(initialState, reloaded.State);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldCreateTransactionStateEvent_ForReturnFileGenerated_CurrentBehavior()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 1003, cycleId: "ACH-CHAR-3");

        Assert.Equal(0, await context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 1003));

        var sut = BuildSut(context, 1003, "DEV14");
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-3", [new ReturnSelectionItemDto(1003, "DEV14")]), CancellationToken.None);

        var evt = await context.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == 1003);
        Assert.Equal(AchTransferStateEnum.Pending, evt.FromState);
        Assert.Equal(AchTransferStateEnum.Pending, evt.ToState);
        Assert.Equal(AchStateEventSourceEnum.System, evt.Source);
        Assert.Equal("DEV14", evt.ReasonCode);
        Assert.Contains("ReturnFileGenerated", evt.PayloadJson, StringComparison.Ordinal);
        Assert.Contains("outbound-return", evt.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldRejectSecondGeneration_WhenReturnAlreadyGenerated_CurrentBehavior()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 1004, cycleId: "ACH-CHAR-4");

        var sut = BuildSut(context, 1004, "DEV14");
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-4", [new ReturnSelectionItemDto(1004, "DEV14")]), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-4", [new ReturnSelectionItemDto(1004, "DEV14")]), CancellationToken.None));
        Assert.Contains("ya cuenta con una devolución registrada", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await context.Set<AchReturnGenerated>().CountAsync(x => x.OriginalTransactionId == 1004));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldSerializeConcurrentGeneration_InSameServiceInstance_CurrentBehavior()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 1005, cycleId: "ACH-CHAR-5");

        var sut = BuildSut(context, 1005, "DEV14", new AchReturnGenerationLockService());
        var request = new GenerateReturnsFileRequest("ACH-CHAR-5", [new ReturnSelectionItemDto(1005, "DEV14")]);

        var t1 = ExecuteIgnoringFailureAsync(() => sut.GenerateReturnsFileAsync(request, CancellationToken.None));
        var t2 = ExecuteIgnoringFailureAsync(() => sut.GenerateReturnsFileAsync(request, CancellationToken.None));
        await Task.WhenAll(t1, t2);

        Assert.Equal(1, await context.Set<AchReturnGenerated>().CountAsync(x => x.OriginalTransactionId == 1005));
        Assert.True(t1.Result.Succeeded ^ t2.Result.Succeeded);
    }

    [Fact]
    public void AchReturnGeneratedConfiguration_ShouldNotDeclareUniqueIndex_CurrentBehavior()
    {
        using var context = BuildContext();
        var entityType = context.Model.FindEntityType(typeof(AchReturnGenerated));
        Assert.NotNull(entityType);

        var target = entityType!.GetIndexes().FirstOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(AchReturnGenerated.OriginalTransactionId),
                nameof(AchReturnGenerated.ReturnReasonCode),
                nameof(AchReturnGenerated.ReturnCycleId)
            }));

        Assert.NotNull(target);
        Assert.False(target!.IsUnique);
    }

    [Fact]
    public void AchReturnGenerated_ShouldNotCurrentlyExposeTransmissionLifecycleFields_CurrentBehavior()
    {
        var propertyNames = typeof(AchReturnGenerated).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("RequestedBy", propertyNames);
        Assert.DoesNotContain("ContentSha256", propertyNames);
        Assert.DoesNotContain("ExternalFileName", propertyNames);
        Assert.DoesNotContain("TransmissionStatus", propertyNames);
        Assert.DoesNotContain("AcceptedAtUtc", propertyNames);
        Assert.DoesNotContain("RejectedAtUtc", propertyNames);
    }

    private static AchReturnsService BuildSut(AchDbContext context, int txId, string reasonCode, IAchReturnGenerationLockService? lockService = null)
    {
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.Is<AchReturnEligibilityRequest>(r => r.TransactionId == txId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, reasonCode, 7002, "Debit", "Pending", []));

        return new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: lockService ?? new TestReturnGenerationLockService());
    }

    private static async Task<(bool Succeeded, Exception? Error)> ExecuteIgnoringFailureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static void SeedScenario(AchDbContext context, int transactionId, string cycleId, AchTransferStateEnum state = AchTransferStateEnum.Pending)
    {
        context.ClearingHouses.Add(new ClearingHouse { Id = 7002, Code = "ACH", Name = "ACH Colombia", OriginCode = "901289999" });
        context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = cycleId, ProcessingDate = new DateTime(2026, 05, 01), ClearingHouseId = 7002, CutoffTime = new TimeSpan(12, 0, 0) });
        context.AchTransactions.Add(new AchTransaction
        {
            Id = transactionId,
            Amount = 125.55m,
            TransactionExternalId = $"EXT-{transactionId}",
            Reference = $"REF-{transactionId}",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "27",
            ServiceClassCode = "200",
            CompanyEntryDescriptionId = 1,
            CompanyName = "ACME",
            CompanyIdentification = "1234567890",
            OriginatingDFI = "09100001",
            ReceivingDFI = "09100002",
            TraceNumber = "091000020000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = new DateTime(2026, 05, 01),
            AddendaRecordIndicator = true,
            IsPrenotification = false,
            State = state,
            StateChangedAtUtc = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycleId,
            AchBatchId = 1
        });

        context.SaveChanges();
    }
}
