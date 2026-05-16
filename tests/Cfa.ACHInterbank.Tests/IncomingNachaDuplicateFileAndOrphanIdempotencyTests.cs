using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaDuplicateFileAndOrphanIdempotencyTests
{
    [Fact]
    public async Task IncomingNachaFileIngestion_ShouldEnforceCanonicalUniqueIndex_ForHashAndSize()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var context = CreateSqliteContext(conn);
        await context.Database.EnsureCreatedAsync();

        var first = BuildIngestion("a.ach", "abc", 106, isReprocess: false);
        context.IncomingNachaFileIngestions.Add(first);
        await context.SaveChangesAsync();

        context.IncomingNachaFileIngestions.Add(BuildIngestion("b.ach", "abc", 106, isReprocess: false));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var canonicalCount = await context.IncomingNachaFileIngestions.CountAsync(x => !x.IsReprocess && x.FileHashSha256 == "abc" && x.FileSize == 106);
        Assert.Equal(1, canonicalCount);

        var indexes = await context.Database.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='IncomingNachaFileIngestions';").ToListAsync();
        Assert.Contains("UX_IncomingNachaFileIngestions_FileHash_FileSize_Canonical", indexes);
    }

    [Fact]
    public async Task IncomingNachaFileIngestion_ShouldAllowReprocessRows_WithSameHashAndSize()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var context = CreateSqliteContext(conn);
        await context.Database.EnsureCreatedAsync();

        var canonical = BuildIngestion("a.ach", "abc", 106, isReprocess: false);
        context.IncomingNachaFileIngestions.Add(canonical);
        await context.SaveChangesAsync();

        context.IncomingNachaFileIngestions.Add(BuildIngestion("a-r1.ach", "abc", 106, isReprocess: true, canonical.Id));
        context.IncomingNachaFileIngestions.Add(BuildIngestion("a-r2.ach", "abc", 106, isReprocess: true, canonical.Id));

        await context.SaveChangesAsync();

        Assert.Equal(1, await context.IncomingNachaFileIngestions.CountAsync(x => !x.IsReprocess));
        Assert.Equal(2, await context.IncomingNachaFileIngestions.CountAsync(x => x.IsReprocess));
    }

    [Fact]
    public async Task IncomingNachaIngestion_ShouldReturnDuplicate_ForSecondCanonicalUpload_DifferentFileName()
    {
        using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<NachaParseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaParseResult());

        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 1,
                DetectedClearingHouseId = 1,
                AchCycleId = "ACH-20260417-01",
                OperationalDate = new DateTime(2026, 4, 17),
                Confidence = 0.95m,
                EvidenceJson = "{}"
            });

        var sut = new IncomingNachaIngestionAppService(context, resolver.Object, parser.Object, Mock.Of<IIncomingNachaPostParseProcessor>(), BuildExternalPolicyMock().Object, Mock.Of<ILogger<IncomingNachaIngestionAppService>>());
        var bytes = Encoding.UTF8.GetBytes(new string('1', 106));

        var first = await sut.IngestAsync(new IncomingNachaIngestionRequest { FileStream = new MemoryStream(bytes), FileName = "incoming-A.ach", RequestedBy = "qa" });
        var second = await sut.IngestAsync(new IncomingNachaIngestionRequest { FileStream = new MemoryStream(bytes), FileName = "incoming-B.ach", RequestedBy = "qa" });

        Assert.Equal(IncomingNachaIngestionStatus.Duplicado, second.IngestionStatus);
        Assert.Equal(first.IngestionId, second.IngestionId);
        Assert.Equal(1, await context.IncomingNachaFileIngestions.CountAsync());
        parser.Verify(x => x.ParseAndSaveDetailedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<NachaParseRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostParseProcessor_ShouldNotDuplicateLinkingBloqueadoEvent_ForSameUnresolvedRecord()
    {
        using var context = CreateInMemoryContext();
        SeedMinimalParsed(context, out var ingestionId);

        var classifier = BuildReturnClassifier("R01", "123456789012345");
        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult { LinkType = IncomingNachaLinkType.NotFound, IsNotFound = true, IsFinal = false, EvidenceJson = "{\"candidates\":[]}" });

        var state = new Mock<IAchStateTransitionService>();
        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), BuildRegulatoryCatalog().Object, state.Object);

        await sut.ProcessAsync(ingestionId, "qa");
        await sut.ProcessAsync(ingestionId, "qa");

        var events = await context.IncomingNachaProcessingEvents
            .Where(e => e.IncomingNachaFileIngestionId == ingestionId && e.EventType == "LinkingBloqueado" && e.EventStatus == "NoEncontrado")
            .ToListAsync();
        Assert.Single(events);

        using var evDoc = JsonDocument.Parse(events[0].EvidenceJson!);
        Assert.Equal("IncomingReturnUnresolved", evDoc.RootElement.GetProperty("eventType").GetString());

        var link = await context.IncomingNachaTransactionLinks.SingleAsync();
        using var linkDoc = JsonDocument.Parse(link.EvidenceJson!);
        Assert.Equal("IncomingReturnUnresolved", linkDoc.RootElement.GetProperty("eventType").GetString());

        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task PostParseProcessor_ShouldPreserveCandidateIds_ForAmbiguousOrphan()
    {
        using var context = CreateInMemoryContext();
        SeedMinimalParsed(context, out var ingestionId);

        var classifier = BuildReturnClassifier("R01", "123456789012345");
        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult { LinkType = IncomingNachaLinkType.Ambiguous, IsAmbiguous = true, IsFinal = false, EvidenceJson = "{\"candidates\":[10,11]}" });

        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), BuildRegulatoryCatalog().Object, Mock.Of<IAchStateTransitionService>());
        await sut.ProcessAsync(ingestionId, "qa");

        var link = await context.IncomingNachaTransactionLinks.SingleAsync();
        using var doc = JsonDocument.Parse(link.EvidenceJson!);
        var root = doc.RootElement;

        Assert.Equal("Ambiguous", root.GetProperty("resolutionReason").GetString());
        Assert.Equal(2, root.GetProperty("candidateCount").GetInt32());
        Assert.Equal(10, root.GetProperty("candidateTransactionIds")[0].GetInt32());
        Assert.Equal(11, root.GetProperty("candidateTransactionIds")[1].GetInt32());
        Assert.True(root.GetProperty("manualReviewRequired").GetBoolean());
        Assert.False(root.GetProperty("applied").GetBoolean());
        Assert.False(root.GetProperty("stateChanged").GetBoolean());
    }

    [Fact]
    public async Task PostParseProcessor_ShouldNotDuplicateOrphanLink_WhenSameUnresolvedRecordIsProcessedTwice()
    {
        using var context = CreateInMemoryContext();
        SeedMinimalParsed(context, out var ingestionId);

        var classifier = BuildReturnClassifier("R01", "123456789012345");
        var linker = new Mock<IIncomingNachaTransactionLinker>();
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<IncomingNachaLinkingContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult { LinkType = IncomingNachaLinkType.NotFound, IsNotFound = true, IsFinal = false, EvidenceJson = "{}" });

        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, Mock.Of<IIncomingNachaPrenotificationResolver>(), Mock.Of<IIncomingNachaDispatchPlanner>(), BuildRegulatoryCatalog().Object, Mock.Of<IAchStateTransitionService>());
        await sut.ProcessAsync(ingestionId, "qa");
        await sut.ProcessAsync(ingestionId, "qa");

        var links = await context.IncomingNachaTransactionLinks.ToListAsync();
        Assert.Single(links);
        var events = await context.IncomingNachaProcessingEvents.Where(x => x.EventType == "LinkingBloqueado").ToListAsync();
        Assert.Single(events);
    }

    static AchDbContext CreateSqliteContext(SqliteConnection conn)
        => new(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(conn).Options);

    static AchDbContext CreateInMemoryContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    static IncomingNachaFileIngestion BuildIngestion(string fileName, string hash, long size, bool isReprocess, Guid? parentIngestionId = null)
        => new()
        {
            FileName = fileName,
            FileHashSha256 = hash,
            FileSize = size,
            ContentType = "text/plain",
            UploadedBy = "qa",
            CorrelationId = Guid.NewGuid().ToString("N"),
            Notes = "qa",
            IsReprocess = isReprocess,
            ParentIngestionId = isReprocess ? parentIngestionId : null
        };

    static Mock<IIncomingNachaFunctionalClassifier> BuildReturnClassifier(string reasonCode, string trace)
    {
        var classifier = new Mock<IIncomingNachaFunctionalClassifier>();
        classifier.Setup(x => x.Classify(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>()))
            .Returns(new IncomingNachaClassificationResult
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                EligibilityStatus = IncomingNachaEligibilityStatus.PendienteResolucion,
                RequiresLink = true,
                BusinessMeaning = "devolución",
                ReturnReasonCode = reasonCode,
                OriginalTraceRef = trace,
                ClassifierVersion = "vtest"
            });
        return classifier;
    }

    static Mock<IAchRegulatoryCatalogService> BuildRegulatoryCatalog()
    {
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.GetReturnCodesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchReturnCode> { new() { Code = "R01", Description = "Fondos insuficientes", AppliesToReturn = true, IsActive = true, RegulatorySource = "EPR" } });
        return regulatory;
    }

    static void SeedMinimalParsed(AchDbContext context, out Guid ingestionId)
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

    static Mock<IExternalFileNamePolicy> BuildExternalPolicyMock()
    {
        var mock = new Mock<IExternalFileNamePolicy>();
        mock.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext context, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = context.ProvidedExternalFileName ?? context.InternalFileName ?? "incoming.txt",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed },
                CorrelationEvidence = new ExternalFileNameCorrelationEvidence(),
                Components = new ExternalFileNameComponents { FullName = context.ProvidedExternalFileName ?? "incoming.txt" }
            });
        return mock;
    }
}
