using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnsR7SeparationTests
{
    [Fact]
    public void PublicReturnsContract_ShouldRemainCompatibilityStable()
    {
        var methods = typeof(IAchReturnsService).GetMethods().OrderBy(method => method.Name).ToArray();

        Assert.Equal(2, methods.Length);
        Assert.Equal(
            [nameof(IAchReturnsService.GenerateReturnsFileAsync), nameof(IAchReturnsService.GetTransactionsByCycleAsync)],
            methods.Select(method => method.Name).ToArray());
    }

    [Fact]
    public void BatchAssemblyPrepare_ShouldPreserveOriginalTraceAmountAndTransactionMapping()
    {
        var traceSequence = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        var sut = new AchReturnBatchComposerComponent(traceSequence.Object);
        var cycle = new AchCycle
        {
            Id = "ACH-R7-C1",
            CycleName = "C1",
            ProcessingDate = new DateTime(2026, 10, 10),
            ClearingHouseId = 7002,
            ClearingHouse = new ClearingHouse { Id = 7002, Code = "ACH", Name = "ACH Colombia" }
        };
        var transaction = new AchTransaction
        {
            Id = 77,
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            Type = TransactionTypeEnum.Debit,
            State = AchTransferStateEnum.Pending,
            TransactionCode = "27",
            TraceNumber = "091000020000001",
            OriginatingDFI = "09100001",
            ReceivingDFI = "09100002",
            DestinationAccountNumber = "123456789",
            Amount = 125.55m,
            EffectiveEntryDate = cycle.ProcessingDate
        };
        var scope = new AchReturnGenerationScope(cycle, cycle, "ACH", IsAchColombia: true, IsCenit: false);

        var prepared = sut.Prepare(transaction, "R01", transaction.Amount, scope, null, null);

        Assert.Equal("091000020000001", prepared.OriginalSequence);
        Assert.Equal(125.55m, prepared.Amount);
        Assert.Equal("09100001", prepared.ReceiverEntity);
        Assert.Equal("09100002", prepared.OriginEntity);
        Assert.Equal("26", prepared.ReturnTransactionCode);
        Assert.Equal("PPD", prepared.StandardEntryClassCode);
    }

    [Fact]
    public void AuditPayload_ShouldPreserveLegacyEventIdentityAndContentHash()
    {
        var sut = new AchReturnAuditMonitoringComponent(
            null,
            null,
            null,
            NullLogger<AchReturnsService>.Instance);
        var cycle = new AchCycle
        {
            Id = "ACH-R7-C1",
            CycleName = "C1",
            ProcessingDate = new DateTime(2026, 10, 10),
            ClearingHouseId = 7002,
            ClearingHouse = new ClearingHouse { Id = 7002, Code = "ACH", Name = "ACH Colombia" }
        };
        var transaction = new AchTransaction
        {
            Id = 77,
            TransactionExternalId = "EXT-77",
            Reference = "REF-77",
            Type = TransactionTypeEnum.Debit,
            State = AchTransferStateEnum.Pending,
            TraceNumber = "091000020000001"
        };
        var generated = new AchReturnGenerated
        {
            OriginalTransactionId = 77,
            ReturnCycleId = cycle.Id,
            ReturnReasonCode = "R01",
            Amount = 125.55m,
            NewSequenceNumber = "091000020000002",
            OriginalSequenceNumber = "091000020000001",
            ReceiverEntityCode = "09100001",
            OriginatorEntityCode = "09100002",
            GeneratedAtUtc = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)
        };
        const string content = "RETURN-CONTENT";

        var payload = sut.BuildReturnFileGeneratedPayload(
            transaction,
            generated,
            cycle,
            "0000001.001.20261010.RET",
            10,
            1,
            generated.GeneratedAtUtc,
            content,
            AchTransferStateEnum.ReturnedByEpr,
            null);

        using var document = System.Text.Json.JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal("AchReturnsService.GenerateReturnsFileAsync", root.GetProperty("source").GetString());
        Assert.Equal("091000020000001", root.GetProperty("originalTraceNumber").GetString());
        Assert.Equal(125.55m, root.GetProperty("amount").GetDecimal());
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content))).ToLowerInvariant(),
            root.GetProperty("contentSha256").GetString());
    }
}
