using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;
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
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.Ambiguous,
                IsAmbiguous = true,
                IsFinal = false,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, state.Object);

        await sut.ProcessAsync(ingestionId, "tester");

        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(e => e.EventType == "LinkingBloqueado"));
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
        linker.Setup(x => x.LinkAsync(It.IsAny<EntryDetail>(), It.IsAny<AddendaRecord?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaLinkingResult
            {
                LinkType = IncomingNachaLinkType.ExactOriginalTraceRef,
                AchTransactionId = 100,
                IsFinal = true,
                ConfidenceScore = 1,
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        state.Setup(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction { Id = 100 });

        var sut = new IncomingNachaPostParseProcessor(context, classifier.Object, linker.Object, state.Object);
        await sut.ProcessAsync(ingestionId, "tester");

        state.Verify(x => x.TransitionAsync(100, AchTransferStateEnum.ReturnedByEpr, AchStateEventSourceEnum.Epr, "R01", It.IsAny<string?>(), "123456789012345", It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(e => e.EventType == "TransicionDisparada"));
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
            Notes = "n"
        };

        var header = new NachaHeader { NachaID = "N1", IncomingNachaFileIngestionId = ingestionId };
        var entry = new EntryDetail { EntryDetailID = 10, NachaID = "N1", SequenceNumber = "123456789012345", TransactionCode = "21", Amount = 0, AccountNumber = "001" };
        var addenda = new AddendaRecord { AddendaID = 20, NachaID = "N1", CodeTypeAddendumRecord = "99", ReturnReasonCode = "R01", OriginalTraceNumber = "123456789012345", EntryDetailSequenceNumber = "9012345" };

        context.IncomingNachaFileIngestions.Add(ingestion);
        context.NachaHeaders.Add(header);
        context.EntryDetails.Add(entry);
        context.AddendaRecords.Add(addenda);
        context.SaveChanges();

        entryId = entry.EntryDetailID;
        addendaId = addenda.AddendaID;
    }
}
