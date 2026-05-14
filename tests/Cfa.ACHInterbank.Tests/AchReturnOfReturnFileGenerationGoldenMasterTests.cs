using System.Text;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnOfReturnFileGenerationGoldenMasterTests
{
    private static readonly DateTime GeneratedAtUtc = new(2026, 05, 14, 12, 34, 56, DateTimeKind.Utc);

    [Fact]
    public async Task GenerateAsync_ShouldMatchPreliminaryGoldenMaster_ForCenit()
    {
        await using var context = BuildContext();
        SeedFlow(context, flowId: 700101, clearingHouseId: 7001, sourceTrace: "SRC-CENIT-001", rorTrace: "ROR-CENIT-001", reasonCode: "R02");
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest([700101], GeneratedAtUtc), CancellationToken.None);

        const string expected = "ROR|CH:7001|TS:2026-05-14T12:34:56.0000000Z|COUNT:1\n"
            + "FLOW|700101|SRC:7001011|ROR:7001012|REASON:R02|SRC_TRACE:SRC-CENIT-001|ROR_TRACE:ROR-CENIT-001";

        Assert.True(result.IsGenerated);
        Assert.Equal("ROR_7001_20260514123456.ach", result.FileName);
        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(result.ContentText!));
        Assert.Equal(result.ContentText, Encoding.ASCII.GetString(result.Content!));
        Assert.Equal(1, result.GeneratedFlowCount);
        Assert.Contains(700101, result.FlowIds);
    }

    [Fact]
    public async Task GenerateAsync_ShouldMatchPreliminaryGoldenMaster_ForAchColombia()
    {
        await using var context = BuildContext();
        SeedFlow(context, flowId: 700201, clearingHouseId: 7002, sourceTrace: "SRC-ACH-001", rorTrace: "ROR-ACH-001", reasonCode: "R02");
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest([700201], GeneratedAtUtc), CancellationToken.None);

        const string expected = "ROR|CH:7002|TS:2026-05-14T12:34:56.0000000Z|COUNT:1\n"
            + "FLOW|700201|SRC:7002011|ROR:7002012|REASON:R02|SRC_TRACE:SRC-ACH-001|ROR_TRACE:ROR-ACH-001";

        Assert.True(result.IsGenerated);
        Assert.Equal("ROR_7002_20260514123456.ach", result.FileName);
        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(result.ContentText!));
        Assert.Equal(result.ContentText, Encoding.ASCII.GetString(result.Content!));
        Assert.Equal(1, result.GeneratedFlowCount);
        Assert.Contains(700201, result.FlowIds);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReject_WhenPreliminaryGoldenMasterMixesCenitAndAch()
    {
        await using var context = BuildContext();
        SeedFlow(context, flowId: 700101, clearingHouseId: 7001, sourceTrace: "SRC-CENIT-001", rorTrace: "ROR-CENIT-001", reasonCode: "R02");
        SeedFlow(context, flowId: 700201, clearingHouseId: 7002, sourceTrace: "SRC-ACH-001", rorTrace: "ROR-ACH-001", reasonCode: "R02");
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest([700101, 700201], GeneratedAtUtc), CancellationToken.None);

        Assert.False(result.IsGenerated);
        Assert.Contains(result.Failures, x => x.Code == "CLEARING_HOUSE_MISSING");
        Assert.Null(result.ContentText);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task GenerateAsync_ShouldMatchPreliminaryGoldenMaster_ForMultipleCenitFlows()
    {
        await using var context = BuildContext();
        SeedFlow(context, flowId: 700101, clearingHouseId: 7001, sourceTrace: "SRC-CENIT-001", rorTrace: "ROR-CENIT-001", reasonCode: "R02");
        SeedFlow(context, flowId: 700102, clearingHouseId: 7001, sourceTrace: "SRC-CENIT-002", rorTrace: "ROR-CENIT-002", reasonCode: "R02");
        var sut = new AchReturnOfReturnFileGenerationService(context);

        var result = await sut.GenerateAsync(new AchReturnOfReturnFileGenerationRequest([700102, 700101], GeneratedAtUtc), CancellationToken.None);

        const string expected = "ROR|CH:7001|TS:2026-05-14T12:34:56.0000000Z|COUNT:2\n"
            + "FLOW|700101|SRC:7001011|ROR:7001012|REASON:R02|SRC_TRACE:SRC-CENIT-001|ROR_TRACE:ROR-CENIT-001\n"
            + "FLOW|700102|SRC:7001021|ROR:7001022|REASON:R02|SRC_TRACE:SRC-CENIT-002|ROR_TRACE:ROR-CENIT-002";

        Assert.True(result.IsGenerated);
        Assert.Equal("ROR_7001_20260514123456.ach", result.FileName);
        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(result.ContentText!));
        Assert.Equal(2, result.GeneratedFlowCount);
        Assert.Contains(700101, result.FlowIds);
        Assert.Contains(700102, result.FlowIds);
    }

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static void SeedFlow(AchDbContext context, int flowId, int clearingHouseId, string sourceTrace, string rorTrace, string reasonCode)
    {
        var cycleId = $"CH-{clearingHouseId}";
        if (!context.AchCycles.Any(x => x.Id == cycleId))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = cycleId,
                CycleName = $"CH-{clearingHouseId}",
                ProcessingDate = new DateTime(2026, 05, 14),
                CutoffTime = TimeSpan.FromHours(8),
                ClearingHouseId = clearingHouseId
            });
        }

        var sourceId = flowId * 10 + 1;
        var rorId = flowId * 10 + 2;

        context.AchTransactions.AddRange(
            BuildTx(sourceId, cycleId, sourceTrace),
            BuildTx(rorId, cycleId, rorTrace));

        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow
        {
            Id = flowId,
            SourceReturnTransactionId = sourceId,
            ReturnOfReturnTransactionId = rorId,
            ReasonCode = reasonCode
        });

        context.SaveChanges();
    }

    private static AchTransaction BuildTx(int id, string cycleId, string trace)
        => new()
        {
            Id = id,
            AchCycleId = cycleId,
            Type = TransactionTypeEnum.Return,
            State = AchTransferStateEnum.ReturnedByOperator,
            EffectiveEntryDate = new DateTime(2026, 05, 14),
            TransactionCode = "22",
            TraceNumber = trace,
            ReceivingDFI = "12345678",
            OriginatingDFI = "12345678",
            Amount = 100m,
            Reference = "R",
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2",
            ReturnReasonCode = "R01"
        };

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n");
}
