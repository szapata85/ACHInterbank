using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchIncomingReturnApplicationAndOrphanCharacterizationTests
{
    [Fact]
    public async Task IngestAsync_ShouldApplyIncomingReturnAndSetStateToReturnedByEpr_CurrentBehavior()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, txId: 10);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());

        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.Accepted, r.Decision);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, (await c.AchTransactions.SingleAsync(x => x.Id == 10)).State);
    }

    [Fact]
    public async Task IngestAsync_ShouldCreateStateEvent_WhenApplyingIncomingReturn_CurrentBehavior()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, txId: 10);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());

        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        var ev = await c.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == 10);
        Assert.Equal(AchTransferStateEnum.Pending, ev.FromState);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, ev.ToState);
        Assert.Equal(AchStateEventSourceEnum.Epr, ev.Source);
        Assert.Equal("R01", ev.ReasonCode);
        Assert.Contains("\"eventType\": \"IncomingReturnApplied\"", ev.PayloadJson);
    }

    [Fact]
    public async Task IngestAsync_ShouldCreateIncomingReturnAppliedEvent_WithStructuredPayload()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, txId: 10);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());

        await sut.IngestAsync(new("f.ach", BuildType7("R01", "123456780000001"), new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);
        var ev = await c.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == 10);

        using var doc = JsonDocument.Parse(ev.PayloadJson!);
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("IncomingReturnApplied", root.GetProperty("eventType").GetString());
        Assert.Equal("AchIncomingReturnIngestionService.IngestAsync", root.GetProperty("source").GetString());
        Assert.Equal("incoming-return", root.GetProperty("generationMode").GetString());
        Assert.Equal(10, root.GetProperty("achTransactionId").GetInt32());
        Assert.Equal("Pending", root.GetProperty("previousState").GetString());
        Assert.Equal("ReturnedByEpr", root.GetProperty("newState").GetString());
        Assert.Equal("R01", root.GetProperty("returnReasonCode").GetString());
        Assert.Equal("123456780000001", root.GetProperty("originalTraceNumber").GetString());
        Assert.Equal("f.ach", root.GetProperty("fileName").GetString());
        Assert.True(root.GetProperty("stateChanged").GetBoolean());
        Assert.True(DateTime.TryParse(root.GetProperty("appliedAtUtc").GetString(), out _));
    }

    [Fact]
    public async Task IngestAsync_ShouldNotCreateStateEvent_WhenRejectedTotal()
    {
        await using var c = Ctx();
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());

        var r = await sut.IngestAsync(new("f.ach", BuildType7("R01", "000000000000001"), new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedTotal, r.Decision);
        Assert.Empty(await c.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task IngestAsync_ShouldCreateStateEventOnlyForAppliedRecords_WhenRejectedPartial()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, txId: 10);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());

        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "000000000000002");
        var r = await sut.IngestAsync(new("f.ach", content, new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedPartial, r.Decision);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, (await c.AchTransactions.SingleAsync(x => x.Id == 10)).State);
        var events = await c.AchTransactionStateEvents.ToListAsync();
        Assert.Single(events);
        Assert.Equal(10, events[0].AchTransactionId);
    }

    [Fact]
    public async Task IngestAsync_ShouldNotCreateStateEvent_ForDuplicateRecordInSameFile_CurrentBehavior()
    {
        await using var c = Ctx();
        SeedTx(c, "123456780000001", 7001, txId: 10);
        var sut = new AchIncomingReturnIngestionService(c, CatalogAllowAll());

        var content = BuildType7("R01", "123456780000001") + BuildType7("R01", "123456780000001");
        var r = await sut.IngestAsync(new("f.ach", content, new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        Assert.Equal(AchIncomingReturnIngestionDecision.RejectedPartial, r.Decision);
        Assert.Equal(0, r.UpdatedTransactionCount);
        Assert.Empty(await c.AchTransactionStateEvents.Where(x => x.AchTransactionId == 10).ToListAsync());
    }

    [Fact]
    public async Task PostParseProcessor_ShouldCreateStateEvent_WhenIncomingReturnIsAppliedThroughStateTransitionService_CurrentBehavior()
    {
        await using var c = Ctx();
        var tx = SeedTx(c, "123456789012345", 7001, txId: 77);
        SeedMinimalParsed(c, out var ingestionId);

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "devolución",
                ReturnReasonCode = "R01",
                OriginalTraceRef = tx.TraceNumber,
                ClassifierVersion = "vtest",
                ClassificationEvidenceJson = "{\"test\":\"characterization\"}"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.ExactOriginalTraceRef,
                AchTransactionId = tx.Id,
                IsFinal = true,
                ConfidenceScore = 1,
                EvidenceJson = "{\"criterion\":\"ExactOriginalTraceRef\"}"
            });

        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<AchReturnCode>
        {
            new() { Code = "R01", AppliesToReturn = true, IsActive = true, RegulatorySource = "EPR" }
        });

        var stateTransition = new AchStateTransitionService(c);
        var sut = new IncomingNachaPostParseProcessor(c, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, stateTransition);

        await sut.ProcessAsync(ingestionId, "tester", CancellationToken.None);

        var updated = await c.AchTransactions.SingleAsync(x => x.Id == tx.Id);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, updated.State);
        var ev = await c.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == tx.Id);
        Assert.Equal(AchTransferStateEnum.Pending, ev.FromState);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, ev.ToState);
        Assert.Equal(AchStateEventSourceEnum.Epr, ev.Source);
        Assert.Equal("R01", ev.ReasonCode);
        Assert.False(string.IsNullOrWhiteSpace(ev.PayloadJson));
    }

    [Fact]
    public async Task Linker_ShouldMarkNotFoundAsUnresolvedOrManualReview_CurrentBehavior()
    {
        await using var c = Ctx();
        var sut = new IncomingNachaTransactionLinker(c);
        var result = await sut.LinkAsync(
            new EntryDetail { SequenceNumber = "999999999999999", RecipIdNumber = "EXT-NA", TransactionCode = "22", AccountNumber = "0001", Amount = 100m },
            new AddendaRecord { OriginalTraceNumber = "888888888888888", ReturnReasonCode = "R01" },
            new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.Devolucion },
            CancellationToken.None);

        Assert.True(result.IsNotFound);
        Assert.Equal(IncomingNachaLinkType.NotFound, result.LinkType);
        Assert.Contains("NotFound", result.EvidenceJson);
    }

    [Fact]
    public async Task Linker_ShouldMarkAmbiguousAsManualReview_CurrentBehavior()
    {
        await using var c = Ctx();
        SeedTx(c, "123456789012345", 7001, txId: 10);
        SeedTx(c, "123456789012345", 7001, txId: 11, cycleId: "C2");
        var sut = new IncomingNachaTransactionLinker(c);

        var result = await sut.LinkAsync(
            new EntryDetail { SequenceNumber = "000000000000111", RecipIdNumber = "", TransactionCode = "22", AccountNumber = "0001", Amount = 100m },
            new AddendaRecord { OriginalTraceNumber = "123456789012345", ReturnReasonCode = "R01" },
            new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.Devolucion },
            CancellationToken.None);

        Assert.True(result.IsAmbiguous);
        Assert.Equal(IncomingNachaLinkType.Ambiguous, result.LinkType);
        Assert.Contains("AmbiguousOriginalTraceRef", result.EvidenceJson);
    }

    [Fact]
    public async Task IncomingNachaIngestion_ShouldDetectDuplicateFile_ByHashAndSize_CurrentBehavior()
    {
        await using var c = Ctx();
        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                Status = Domain.Models.ACH.IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 7001,
                AchCycleId = "C1",
                IsResolved = true,
                OperationalDate = new DateTime(2026, 5, 16)
            });
        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<NachaParseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaParseResult());
        var policy = new Mock<IExternalFileNamePolicy>();
        policy.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext context, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = context.ProvidedExternalFileName ?? context.InternalFileName ?? "incoming.txt",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed },
                CorrelationEvidence = new ExternalFileNameCorrelationEvidence(),
                Components = new ExternalFileNameComponents { FullName = context.ProvidedExternalFileName ?? "incoming.txt" }
            });
        var sut = new IncomingNachaIngestionAppService(c, resolver.Object, parser.Object, Mock.Of<IIncomingNachaPostParseProcessor>(), policy.Object, Mock.Of<Microsoft.Extensions.Logging.ILogger<IncomingNachaIngestionAppService>>());

        var bytes = Encoding.UTF8.GetBytes(new string('1', 106));
        var req = new IncomingNachaIngestionRequest { FileName = "incoming.ach", ContentType = "text/plain", FileStream = new MemoryStream(bytes), RequestedBy = "qa" };
        var first = await sut.IngestAsync(req, CancellationToken.None);
        var second = await sut.IngestAsync(new IncomingNachaIngestionRequest { FileName = "incoming-copy.ach", ContentType = "text/plain", FileStream = new MemoryStream(bytes), RequestedBy = "qa" }, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, first.IngestionId);
        Assert.Equal(Domain.Models.ACH.IncomingNachaIngestionStatus.Duplicado, second.IngestionStatus);
    }

    private static string BuildType7(string reason, string originalTrace)
    {
        var chars = Enumerable.Repeat(' ', 106).ToArray();
        chars[0] = '7'; chars[1] = '9'; chars[2] = '9';
        var rr = reason.PadRight(5).Take(5).ToArray(); Array.Copy(rr, 0, chars, 3, 5);
        var tr = originalTrace.PadLeft(15, '0').TakeLast(15).ToArray(); Array.Copy(tr, 0, chars, 8, 15);
        return new string(chars);
    }

    private static Mock<IAchRegulatoryCatalogService> CatalogAllowAllMock()
    {
        var m = new Mock<IAchRegulatoryCatalogService>();
        m.Setup(x => x.ValidateReturnCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        m.Setup(x => x.ValidateReturnPolicyAsync(It.IsAny<int>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((true, null));
        return m;
    }

    private static IAchRegulatoryCatalogService CatalogAllowAll() => CatalogAllowAllMock().Object;

    private static AchDbContext Ctx() => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AchTransaction SeedTx(AchDbContext c, string trace, int clearingHouseId, int txId, string cycleId = "C1")
    {
        if (!c.ClearingHouses.Any(x => x.Id == clearingHouseId))
        {
            c.ClearingHouses.Add(new ClearingHouse { Id = clearingHouseId, Code = "CENIT", Name = "CH", OriginCode = "000101006" });
        }

        c.AchCycles.Add(new AchCycle { Id = cycleId, CycleName = cycleId, ProcessingDate = new DateTime(2026, 5, 16), CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = clearingHouseId });
        var tx = new AchTransaction
        {
            Id = txId,
            TraceNumber = trace,
            AchCycleId = cycleId,
            Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = new DateTime(2026, 5, 16),
            TransactionCode = "22",
            ReceivingDFI = "12345678",
            OriginatingDFI = "12345678",
            Amount = 100,
            Reference = "R",
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2",
            OriginalTraceRef = $"ALT{txId:000000000000}",
            RecipientIdNumber = "RID-001"
        };
        c.AchTransactions.Add(tx);
        c.SaveChanges();
        return tx;
    }

    private static void SeedMinimalParsed(AchDbContext context, out Guid ingestionId)
    {
        ingestionId = Guid.NewGuid();
        context.IncomingNachaFileIngestions.Add(new IncomingNachaFileIngestion
        {
            Id = ingestionId,
            FileName = "in.ach",
            FileHashSha256 = "h",
            FileSize = 106,
            ContentType = "text/plain",
            UploadedBy = "u",
            CorrelationId = "c",
            Notes = "n",
            ResolvedClearingHouseId = 7001,
            ResolvedAchCycleId = "C1",
            OperationalDate = new DateTime(2026, 5, 16)
        });
        context.NachaHeaders.Add(new NachaHeader { NachaID = "N1", IncomingNachaFileIngestionId = ingestionId });
        context.EntryDetails.Add(new EntryDetail { EntryDetailID = 10, NachaID = "N1", SequenceNumber = "123456789012345", TransactionCode = "22", Amount = 100m, AccountNumber = "0001", RecipIdNumber = "RID-001" });
        context.AddendaRecords.Add(new AddendaRecord { AddendaID = 20, NachaID = "N1", CodeTypeAddendumRecord = "99", ReturnReasonCode = "R01", OriginalTraceNumber = "123456789012345", EntryDetailSequenceNumber = "9012345" });
        context.SaveChanges();
    }
}
