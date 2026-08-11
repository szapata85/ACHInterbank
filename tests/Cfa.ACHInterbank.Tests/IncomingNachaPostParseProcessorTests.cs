using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaPostParseProcessorTests
{
    [Fact]
    public async Task ProcessAsync_DoesNotTransition_WhenLinkIsInsecure()
    {
        using var context = BuildContext();
        SeedMinimalParsed(context, out var ingestionId, out var entryId, out var addendaId);

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "devolución",
                ReturnReasonCode = "R01",
                OriginalTraceRef = "123456789012345",
                ClassifierVersion = "vtest"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.Ambiguous,
                IsAmbiguous = true,
                IsFinal = false,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode>
            {
                new() { Code = "R01", Description = "Fondos insuficientes", AppliesToReturn = true, IsActive = true, RegulatorySource = "EPR" }
            });
        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, state.Object);

        await sut.ProcessAsync(ingestionId, "tester");

        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(e => e.EventType == "LinkingBloqueado"));
        var link = await context.IncomingNachaTransactionLinks.SingleAsync();
        using var linkDoc = JsonDocument.Parse(link.EvidenceJson!);
        Assert.Equal("IncomingReturnUnresolved", linkDoc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal("Unresolved", linkDoc.RootElement.GetProperty("resolutionStatus").GetString());
        Assert.Equal("Ambiguous", linkDoc.RootElement.GetProperty("resolutionReason").GetString());
        Assert.Equal(0, linkDoc.RootElement.GetProperty("candidateCount").GetInt32());
        Assert.Equal("R01", linkDoc.RootElement.GetProperty("returnReasonCode").GetString());
        Assert.Equal("123456789012345", linkDoc.RootElement.GetProperty("originalTraceNumber").GetString());
        var ev = await context.IncomingNachaProcessingEvents.SingleAsync(x => x.EventType == "LinkingBloqueado");
        using var evDoc = JsonDocument.Parse(ev.EvidenceJson!);
        Assert.Equal("IncomingReturnUnresolved", evDoc.RootElement.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task ProcessAsync_PersistsStructuredOrphanPayload_ForNotFound()
    {
        using var context = BuildContext();
        SeedMinimalParsed(context, out var ingestionId, out _, out _);

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "devolución",
                ReturnReasonCode = "R01",
                OriginalTraceRef = "999999999999999",
                ClassifierVersion = "vtest"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.NotFound,
                IsNotFound = true,
                IsFinal = false,
                ConfidenceScore = 0,
                EvidenceJson = "{\"criterion\":\"NotFound\",\"candidates\":[]}"
            });

        var state = new Mock<IAchStateTransitionService>();
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode> { new() { Code = "R01", Description = "Fondos insuficientes", AppliesToReturn = true, IsActive = true, RegulatorySource = "EPR" } });
        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, state.Object);

        await sut.ProcessAsync(ingestionId, "tester");

        var link = await context.IncomingNachaTransactionLinks.SingleAsync();
        using var doc = JsonDocument.Parse(link.EvidenceJson!);
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("IncomingReturnUnresolved", root.GetProperty("eventType").GetString());
        Assert.Equal("Unresolved", root.GetProperty("resolutionStatus").GetString());
        Assert.Equal("NotFound", root.GetProperty("resolutionReason").GetString());
        Assert.True(root.GetProperty("manualReviewRequired").GetBoolean());
        Assert.Equal("IncomingNachaPostParseProcessor.ProcessAsync", root.GetProperty("source").GetString());
        Assert.Equal("IncomingNachaTransactionLinker.LinkAsync", root.GetProperty("linkSource").GetString());
        Assert.Equal("in.ach", root.GetProperty("fileName").GetString());
        Assert.Equal(1, root.GetProperty("fileSize").GetInt64());
        Assert.True(root.TryGetProperty("clearingHouseId", out _));
        Assert.True(root.TryGetProperty("achCycleId", out _));
        Assert.Equal("R01", root.GetProperty("returnReasonCode").GetString());
        Assert.Equal("999999999999999", root.GetProperty("originalTraceNumber").GetString());
        Assert.Equal("NotFound", root.GetProperty("linkType").GetString());
        Assert.Equal(0, root.GetProperty("candidateCount").GetInt32());
        Assert.False(root.GetProperty("stateChanged").GetBoolean());
        Assert.False(root.GetProperty("applied").GetBoolean());
    }

    [Fact]
    public async Task ProcessAsync_TransitionsReturn_WhenDeterministicLink()
    {
        using var context = BuildContext();
        SeedMinimalParsed(context, out var ingestionId, out var entryId, out var addendaId);

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "devolución",
                ReturnReasonCode = "R01",
                OriginalTraceRef = "123456789012345",
                ClassifierVersion = "vtest"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.ExactOriginalTraceRef,
                AchTransactionId = 100,
                IsFinal = true,
                ConfidenceScore = 1,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        state.Setup(x => x.TransitionAsync(It.IsAny<AchStateTransitionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchStateTransitionResult(new AchTransaction { Id = 100 }, true, false));
        var resultResolver = new Mock<IIncomingNachaAchResultResolver>();
        resultResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaAchResultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaAchResultResolution(true, 77, "R01", "Fondos insuficientes", IncomingNachaBusinessOutcome.Returned, "OK"));
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode>
            {
                new() { Code = "R01", Description = "Fondos insuficientes", AppliesToReturn = true, IsActive = true, RegulatorySource = "EPR" }
            });
        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, state.Object, null, resultResolver.Object);
        await sut.ProcessAsync(ingestionId, "tester");

        var processingEvents = await context.IncomingNachaProcessingEvents.AsNoTracking().ToListAsync();
        Assert.True(processingEvents.Any(e => e.EventType == "TransicionDisparada"),
            string.Join(" | ", processingEvents.Select(e => $"{e.EventType}:{e.EventStatus}:{e.Message}")));
        state.Verify(x => x.TransitionAsync(
            It.Is<AchStateTransitionRequest>(request => request.TransactionId == 100
                && request.ToState == AchTransferStateEnum.ReturnedByEpr
                && request.Source == AchStateEventSourceEnum.Epr
                && request.ReasonCode == "R01"
                && request.OriginalTraceRef == "123456789012345"
                && request.IdempotencyKey != null),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(e => e.EventType == "TransicionDisparada"));
    }

    [Fact]
    public async Task ProcessAsync_TransitionsOperator_WhenDevCodeIsMappedAsOperator()
    {
        using var context = BuildContext();
        SeedMinimalParsed(context, out var ingestionId, out _, out _);
        context.ClearingHouses.Single().Code = "ACHCOL";
        context.SaveChanges();

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.RechazadaOperador,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "rechazo operador",
                ReturnReasonCode = "DEV14",
                OriginalTraceRef = "123456789012345",
                ClassifierVersion = "vtest"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.ExactOriginalTraceRef,
                AchTransactionId = 101,
                IsFinal = true,
                ConfidenceScore = 1,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        state.Setup(x => x.TransitionAsync(It.IsAny<AchStateTransitionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchStateTransitionResult(new AchTransaction { Id = 101 }, true, false));
        var resultResolver = new Mock<IIncomingNachaAchResultResolver>();
        resultResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaAchResultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaAchResultResolution(true, 78, "DEV14", "Rechazo del operador", IncomingNachaBusinessOutcome.Rejected, "OK"));

        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode>
            {
                new() { Code = "DEV14", Description = "rechazo operador", AppliesToReturn = true, IsActive = true, RegulatorySource = "OPERATOR" }
            });

        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, state.Object, null, resultResolver.Object);
        await sut.ProcessAsync(ingestionId, "tester");

        state.Verify(x => x.TransitionAsync(
            It.Is<AchStateTransitionRequest>(request => request.TransactionId == 101
                && request.ToState == AchTransferStateEnum.ReturnedByOperator
                && request.Source == AchStateEventSourceEnum.Operator
                && request.ReasonCode == "DEV14"
                && request.OriginalTraceRef == "123456789012345"
                && request.IdempotencyKey != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_BlocksTransition_WhenReasonCodeMissingInCatalog()
    {
        using var context = BuildContext();
        SeedMinimalParsed(context, out var ingestionId, out _, out _);

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "devolución",
                ReturnReasonCode = "R99",
                OriginalTraceRef = "123456789012345",
                ClassifierVersion = "vtest"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.ExactOriginalTraceRef,
                AchTransactionId = 102,
                IsFinal = true,
                ConfidenceScore = 1,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode>());

        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, state.Object);
        await sut.ProcessAsync(ingestionId, "tester");

        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(e => e.EventType == "TransicionBloqueada"));
    }

    [Fact]
    public async Task ProcessAsync_BlocksTransition_WhenFunctionalClassAndCatalogRouteConflict()
    {
        using var context = BuildContext();
        SeedMinimalParsed(context, out var ingestionId, out _, out _);

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.RechazadaOperador,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "rechazo operador",
                ReturnReasonCode = "R01",
                OriginalTraceRef = "123456789012345",
                ClassifierVersion = "vtest"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.ExactOriginalTraceRef,
                AchTransactionId = 103,
                IsFinal = true,
                ConfidenceScore = 1,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode>
            {
                new() { Code = "R01", Description = "Fondos insuficientes", AppliesToReturn = true, IsActive = true, RegulatorySource = "EPR" }
            });

        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, state.Object);
        await sut.ProcessAsync(ingestionId, "tester");

        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(e => e.EventType == "TransicionBloqueada"));
    }

    [Fact]
    public async Task ProcessAsync_BlocksTransition_WhenReasonCodeInactive()
    {
        using var context = BuildContext();
        SeedMinimalParsed(context, out var ingestionId, out _, out _);

        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "devolución",
                ReturnReasonCode = "DEV14",
                OriginalTraceRef = "123456789012345",
                ClassifierVersion = "vtest"
            });

        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.ExactOriginalTraceRef,
                AchTransactionId = 104,
                IsFinal = true,
                ConfidenceScore = 1,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode>
            {
                new() { Code = "DEV14", Description = "rechazo operador", AppliesToReturn = true, IsActive = false, RegulatorySource = "OPERATOR" }
            });

        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), regulatory.Object, state.Object);
        await sut.ProcessAsync(ingestionId, "tester");

        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(e => e.EventType == "TransicionBloqueada"));
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }

    private static void SeedMinimalParsed(AchDbContext context, out Guid ingestionId, out int entryId, out int addendaId)
    {
        ingestionId = Guid.NewGuid();
        var ingestion = new IncomingNachaFileIngestion
        {
            Id = ingestionId,
            FileName = "in.ach",
            FileHashSha256 = "h",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "u",
            CorrelationId = "c",
            Notes = "n",
            ResolvedClearingHouseId = 1,
            ResolvedAchCycleId = "C2",
            DetectedCycleNumber = 2,
            EffectiveDate = new DateTime(2026, 8, 2),
            OperationalDate = new DateTime(2026, 8, 2)
        };

        var header = new NachaHeader { NachaID = "N1", IncomingNachaFileIngestionId = ingestionId };
        var entry = new EntryDetail { EntryDetailID = 10, NachaID = "N1", SequenceNumber = "123456789012345", TransactionCode = "21", Amount = 100, AccountNumber = "001" };
        var addenda = new AddendaRecord { AddendaID = 20, NachaID = "N1", CodeTypeAddendumRecord = "99", ReturnReasonCode = "R01", OriginalTraceNumber = "123456789012345", EntryDetailSequenceNumber = "9012345" };

        context.IncomingNachaFileIngestions.Add(ingestion);
        context.NachaHeaders.Add(header);
        context.EntryDetails.Add(entry);
        context.AddendaRecords.Add(addenda);
        context.ClearingHouses.Add(new ClearingHouse { Id = 1, Name = "CENIT", Code = "CENIT", OriginCode = "00010100" });
        context.AchCycles.AddRange(
            new AchCycle { Id = "C1", CycleName = "Ciclo 1", ProcessingDate = new DateTime(2026, 8, 2), CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = 1 },
            new AchCycle { Id = "C2", CycleName = "Ciclo 2", ProcessingDate = new DateTime(2026, 8, 2), CutoffTime = new TimeSpan(10, 0, 0), ClearingHouseId = 1 },
            new AchCycle { Id = "C3", CycleName = "Ciclo 3", ProcessingDate = new DateTime(2026, 8, 2), CutoffTime = new TimeSpan(12, 0, 0), ClearingHouseId = 1 },
            new AchCycle { Id = "C4", CycleName = "Ciclo 4", ProcessingDate = new DateTime(2026, 8, 2), CutoffTime = new TimeSpan(14, 0, 0), ClearingHouseId = 1 },
            new AchCycle { Id = "C5", CycleName = "Ciclo 5", ProcessingDate = new DateTime(2026, 8, 2), CutoffTime = new TimeSpan(16, 0, 0), ClearingHouseId = 1 });
        for (var transactionId = 100; transactionId <= 104; transactionId++)
        {
            context.AchTransactions.Add(new AchTransaction
            {
                Id = transactionId,
                Amount = 100,
                TransactionExternalId = $"EXT-{transactionId}",
                Reference = $"REF-{transactionId}",
                Type = TransactionTypeEnum.Debit,
                TransactionCode = "27",
                TraceNumber = $"12345678{transactionId:0000000}",
                EffectiveEntryDate = new DateTime(2026, 8, 2),
                State = AchTransferStateEnum.Pending,
                SourceAccountNumber = "1",
                DestinationAccountNumber = "2",
                AchCycleId = "C1",
                AchBatchId = 1
            });
        }
        context.SaveChanges();

        entryId = entry.EntryDetailID;
        addendaId = addenda.AddendaID;
    }
}
