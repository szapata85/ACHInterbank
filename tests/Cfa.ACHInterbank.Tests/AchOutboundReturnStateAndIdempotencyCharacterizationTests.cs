using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
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


    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldCreateOneReturnFileGeneratedEvent_PerReturnedTransaction()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 2001, cycleId: "ACH-CHAR-MULTI-1");
        SeedScenario(context, transactionId: 2002, cycleId: "ACH-CHAR-MULTI-1");

        var sut = BuildSut(context, new Dictionary<int, string> { [2001] = "DEV14", [2002] = "DEV14" });
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-MULTI-1", [new ReturnSelectionItemDto(2001, "DEV14"), new ReturnSelectionItemDto(2002, "DEV14")]), CancellationToken.None);

        Assert.Equal(2, await context.Set<AchReturnGenerated>().CountAsync(x => x.ReturnCycleId == "ACH-CHAR-MULTI-1"));
        var events = await context.AchTransactionStateEvents.Where(x => x.AchTransactionId == 2001 || x.AchTransactionId == 2002).ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, x => x.AchTransactionId == 2001);
        Assert.Contains(events, x => x.AchTransactionId == 2002);
        Assert.All(events, x =>
        {
            Assert.Contains("ReturnFileGenerated", x.PayloadJson, StringComparison.Ordinal);
            Assert.Contains("outbound-return", x.PayloadJson, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldNotCreateStateEvent_WhenGenerationFailsBeforePersistence()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 2003, cycleId: "ACH-CHAR-FAIL-1");

        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(false, "DEV14", 7002, "Debit", "Pending", [new AchReturnEligibilityFailure("RETURN_POLICY_REJECTED", "reject") ]));

        var sut = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-FAIL-1", [new ReturnSelectionItemDto(2003, "DEV14")]), CancellationToken.None));

        Assert.Equal(0, await context.Set<AchReturnGenerated>().CountAsync(x => x.OriginalTransactionId == 2003));
        Assert.Equal(0, await context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 2003));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldNotCreateDuplicateStateEvent_WhenSecondGenerationRejected()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 2004, cycleId: "ACH-CHAR-DUP-1");

        var sut = BuildSut(context, 2004, "DEV14");
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-DUP-1", [new ReturnSelectionItemDto(2004, "DEV14")]), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-DUP-1", [new ReturnSelectionItemDto(2004, "DEV14")]), CancellationToken.None));

        Assert.Equal(1, await context.Set<AchReturnGenerated>().CountAsync(x => x.OriginalTransactionId == 2004));
        Assert.Equal(1, await context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 2004));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldCreateReturnFileGeneratedEvent_WithStructuredPayload()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 2005, cycleId: "ACH-CHAR-PAYLOAD-1");

        var sut = BuildSut(context, 2005, "DEV14");
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-PAYLOAD-1", [new ReturnSelectionItemDto(2005, "DEV14")]), CancellationToken.None);

        var evt = await context.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == 2005);
        using var doc = System.Text.Json.JsonDocument.Parse(evt.PayloadJson!);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("ReturnFileGenerated", root.GetProperty("eventType").GetString());
        Assert.Equal("AchReturnsService.GenerateReturnsFileAsync", root.GetProperty("source").GetString());
        Assert.Equal("outbound-return", root.GetProperty("generationMode").GetString());
        Assert.False(root.GetProperty("stateChanged").GetBoolean());

        Assert.Equal(2005, root.GetProperty("originalTransactionId").GetInt32());
        Assert.Equal("EXT-2005", root.GetProperty("transactionExternalId").GetString());
        Assert.Equal("REF-2005", root.GetProperty("reference").GetString());
        Assert.Equal("Debit", root.GetProperty("transactionType").GetString());
        Assert.Equal("Pending", root.GetProperty("previousState").GetString());
        Assert.Equal("Pending", root.GetProperty("newState").GetString());

        Assert.Equal("DEV14", root.GetProperty("returnReasonCode").GetString());
        Assert.Equal("ACH-CHAR-PAYLOAD-1", root.GetProperty("returnCycleId").GetString());
        Assert.Equal(7002, root.GetProperty("clearingHouseId").GetInt32());
        Assert.Equal("ACH", root.GetProperty("clearingHouseCode").GetString());
        Assert.Equal("ACH Colombia", root.GetProperty("clearingHouseName").GetString());

        Assert.Equal(response.FileName, root.GetProperty("fileName").GetString());
        Assert.Equal(response.FileName, root.GetProperty("externalFileName").GetString());
        var expectedHash = Convert.ToHexString(SHA256.HashData(response.Content)).ToLowerInvariant();
        var contentSha256 = root.GetProperty("contentSha256").GetString();
        Assert.Equal(expectedHash, contentSha256);
        Assert.Matches("^[a-f0-9]{64}$", contentSha256);
        Assert.True(root.GetProperty("recordCount").GetInt32() > 0);
        Assert.Equal(1, root.GetProperty("returnCount").GetInt32());

        Assert.Equal("091000020000001", root.GetProperty("originalTraceNumber").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("newTraceNumber").GetString()));
        Assert.Equal("091000020000001", root.GetProperty("originalSequenceNumber").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("newSequenceNumber").GetString()));

        Assert.Equal(125.55m, root.GetProperty("amount").GetDecimal());
        Assert.Equal("COP", root.GetProperty("currency").GetString());
        Assert.Equal("09100001", root.GetProperty("receiverEntityCode").GetString());
        Assert.Equal("09100002", root.GetProperty("originatorEntityCode").GetString());

        Assert.Equal("GeneratedNotTransmitted", root.GetProperty("transmissionStatus").GetString());
        Assert.Equal("TechnicalGeneratedOnly", root.GetProperty("productiveStatus").GetString());
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldCreateAuditEventWithoutChangingTransactionState()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 2006, cycleId: "ACH-CHAR-STATE-1", state: AchTransferStateEnum.Pending);
        var before = await context.AchTransactions.SingleAsync(x => x.Id == 2006);
        var beforeChangedAt = before.StateChangedAtUtc;

        var sut = BuildSut(context, 2006, "DEV14");
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-STATE-1", [new ReturnSelectionItemDto(2006, "DEV14")]), CancellationToken.None);

        var after = await context.AchTransactions.SingleAsync(x => x.Id == 2006);
        var evt = await context.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == 2006);

        Assert.Equal(AchTransferStateEnum.Pending, after.State);
        Assert.Equal(beforeChangedAt, after.StateChangedAtUtc);
        Assert.Equal(evt.FromState, evt.ToState);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldKeepNachaOutputUnchanged_WhenStateEventAuditIsCreated()
    {
        await using var context = BuildContext();
        SeedScenario(context, transactionId: 2007, cycleId: "ACH-CHAR-NACHA-1");

        var sut = BuildSut(context, 2007, "DEV14");
        var response = await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("ACH-CHAR-NACHA-1", [new ReturnSelectionItemDto(2007, "DEV14")]), CancellationToken.None);

        var content = System.Text.Encoding.UTF8.GetString(response.Content);
        Assert.Contains("A094101", content, StringComparison.Ordinal);
        Assert.Contains("DEV14", content, StringComparison.Ordinal);
        Assert.Contains('1', content);
        Assert.Contains('5', content);
        Assert.Contains('6', content);
        Assert.Contains('7', content);
        Assert.Contains('8', content);
        Assert.Contains('9', content);
        Assert.Equal(1, await context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 2007));
    }


    private static AchReturnsService BuildSut(AchDbContext context, IReadOnlyDictionary<int, string> reasonCodes, IAchReturnGenerationLockService? lockService = null)
    {
        var eligibility = new Mock<IAchReturnEligibilityService>(MockBehavior.Strict);
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchReturnEligibilityRequest r, CancellationToken _) =>
            {
                if (!reasonCodes.TryGetValue(r.TransactionId, out var reasonCode))
                {
                    return new AchReturnEligibilityResult(false, null, null, null, null, [new AchReturnEligibilityFailure("TX_NOT_CONFIGURED", "tx")]);
                }
                return new AchReturnEligibilityResult(true, reasonCode, 7002, "Debit", "Pending", []);
            });

        return new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: lockService ?? new TestReturnGenerationLockService());
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
        if (!context.ClearingHouses.Any(x => x.Id == 7002))
        {
            context.ClearingHouses.Add(new ClearingHouse { Id = 7002, Code = "ACH", Name = "ACH Colombia", OriginCode = "901289999" });
        }

        if (!context.AchCycles.Any(x => x.Id == cycleId))
        {
            context.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = cycleId, ProcessingDate = new DateTime(2026, 05, 01), ClearingHouseId = 7002, CutoffTime = new TimeSpan(12, 0, 0) });
        }

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
