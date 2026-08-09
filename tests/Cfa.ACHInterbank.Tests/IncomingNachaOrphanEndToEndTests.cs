using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaOrphanEndToEndTests
{
    [Fact]
    public async Task ResolveAsync_ShouldLinkAndApplyThroughOfficialPipeline_WithCauseEventAndAudit()
    {
        await using var context = BuildContext();
        var scenario = SeedScenario(context);
        var sut = BuildResolver(context);

        var result = await sut.ResolveAsync(Request(scenario.LinkId, scenario.TransactionId));

        Assert.True(result.IsResolved);
        Assert.Equal("Applied", result.Status);
        var link = await context.IncomingNachaTransactionLinks.SingleAsync(x => x.Id == scenario.LinkId);
        Assert.Equal(IncomingNachaLinkType.Manual, link.LinkType);
        Assert.True(link.IsFinal);
        Assert.Equal(scenario.TransactionId, link.AchTransactionId);
        Assert.Equal("operador.uat", link.LinkedBy);

        var transaction = await context.AchTransactions.SingleAsync(x => x.Id == scenario.TransactionId);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, transaction.State);
        Assert.Equal("R01", transaction.ReturnReasonCode);
        Assert.Equal("123456780000001", transaction.OriginalTraceRef);

        var stateEvent = await context.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == scenario.TransactionId);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, stateEvent.ToState);
        Assert.Equal(AchStateEventSourceEnum.Operator, stateEvent.Source);
        Assert.Equal("R01", stateEvent.ReasonCode);
        Assert.Equal(77, stateEvent.AchReturnCodeId);
        Assert.False(string.IsNullOrWhiteSpace(stateEvent.IdempotencyKey));

        var manualEvent = await context.IncomingNachaProcessingEvents
            .SingleAsync(x => x.EventType == "OrphanManualResolution");
        Assert.Equal("operador.uat", manualEvent.RaisedBy);
        Assert.Equal("Applied", manualEvent.EventStatus);
        using var evidence = JsonDocument.Parse(manualEvent.EvidenceJson);
        Assert.True(evidence.RootElement.GetProperty("applied").GetBoolean());
        Assert.True(evidence.RootElement.GetProperty("stateChanged").GetBoolean());
        Assert.Equal(scenario.TransactionId, evidence.RootElement.GetProperty("resolvedAchTransactionId").GetInt32());
    }

    [Fact]
    public async Task ResolveAsync_ShouldBeIdempotent_WhenSameAssociationIsRepeated()
    {
        await using var context = BuildContext();
        var scenario = SeedScenario(context);
        var sut = BuildResolver(context);

        var first = await sut.ResolveAsync(Request(scenario.LinkId, scenario.TransactionId));
        var replay = await sut.ResolveAsync(Request(scenario.LinkId, scenario.TransactionId));

        Assert.True(first.IsResolved);
        Assert.True(replay.IsResolved);
        Assert.True(replay.IsIdempotentReplay);
        Assert.Equal("AlreadyApplied", replay.Status);
        Assert.Equal(1, await context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == scenario.TransactionId));
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "OrphanManualResolution"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldRejectIncompatibleTransaction_WithoutApplyingReturn()
    {
        await using var context = BuildContext();
        var scenario = SeedScenario(context);
        var incompatible = await context.AchTransactions.SingleAsync(x => x.Id == scenario.TransactionId);
        incompatible.DestinationAccountNumber = "999999";
        await context.SaveChangesAsync();
        var sut = BuildResolver(context);

        var result = await sut.ResolveAsync(Request(scenario.LinkId, scenario.TransactionId));

        Assert.False(result.IsResolved);
        Assert.Equal("IncompatibleTransaction", result.Status);
        Assert.Equal(AchTransferStateEnum.Pending, incompatible.State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
        var link = await context.IncomingNachaTransactionLinks.SingleAsync(x => x.Id == scenario.LinkId);
        Assert.False(link.IsFinal);
        Assert.Equal(IncomingNachaLinkType.Ambiguous, link.LinkType);
    }

    [Fact]
    public async Task CommandCenter_ShouldExposeOrphanAndCompatibleCandidate_WithoutApplyingIt()
    {
        await using var context = BuildContext();
        var scenario = SeedScenario(context);
        var service = new IncomingNachaCommandCenterService(context, Mock.Of<IIncomingNachaStateMachineService>());

        var orphans = await service.GetOrphansAsync(new IncomingNachaOrphanQuery());
        var candidates = await service.GetOrphanCandidatesAsync(scenario.LinkId, scenario.TransactionId.ToString());

        var orphan = Assert.Single(orphans.Items);
        Assert.Equal("Sin relación", orphan.ResolutionStatus);
        Assert.Equal("R01", orphan.ReturnReasonCode);
        Assert.Equal("Fondos insuficientes", orphan.ReturnReasonDescription);
        Assert.Equal([scenario.TransactionId], orphan.CandidateTransactionIds);
        var candidate = Assert.Single(candidates);
        Assert.Equal(scenario.TransactionId, candidate.AchTransactionId);
        Assert.True(candidate.IsCompatible);
        Assert.Empty(candidate.IncompatibilityReasons);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
    }

    private static IncomingNachaOrphanManualResolutionRequest Request(Guid linkId, int transactionId)
        => new()
        {
            IncomingNachaTransactionLinkId = linkId,
            ResolutionAction = IncomingNachaOrphanResolutionAction.LinkToTransaction,
            ResolvedAchTransactionId = transactionId,
            ResolutionReason = "Validación operativa controlada",
            Comment = "Coinciden cámara, valor, cuenta y rastreo original.",
            CorrelationId = "RET-ORPHAN-E2E",
            ResolvedBy = "operador.uat"
        };

    private static IncomingNachaOrphanManualResolutionService BuildResolver(AchDbContext context)
    {
        var resultResolver = new Mock<IIncomingNachaAchResultResolver>();
        resultResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaAchResultRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaAchResultResolution(
                true, 77, "R01", "Fondos insuficientes", IncomingNachaBusinessOutcome.Returned, "Resolved"));
        var postProcessor = new IncomingNachaPostParseProcessor(
            context,
            Mock.Of<IIncomingNachaFunctionalClassifier>(),
            Mock.Of<IIncomingNachaTransactionLinker>(),
            Mock.Of<IIncomingNachaPrenotificationResolver>(),
            Mock.Of<IIncomingNachaDispatchPlanner>(),
            Mock.Of<IAchRegulatoryCatalogService>(),
            new AchStateTransitionService(context),
            resultResolver: resultResolver.Object);
        return new IncomingNachaOrphanManualResolutionService(context, postProcessor);
    }

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (Guid LinkId, int TransactionId) SeedScenario(AchDbContext context)
    {
        const int clearingHouseId = 7002;
        const int transactionId = 501;
        const string cycleId = "ACH-ORPHAN-1";
        var ingestionId = Guid.NewGuid();
        var linkId = Guid.NewGuid();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = clearingHouseId,
            Code = "ACHCOL",
            Name = "ACH Colombia",
            OriginCode = "000101006"
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            CycleName = "Ciclo controlado",
            ClearingHouseId = clearingHouseId,
            ProcessingDate = DateTime.UtcNow.Date,
            CutoffTime = new TimeSpan(23, 0, 0)
        });
        context.AchTransactions.Add(new AchTransaction
        {
            Id = transactionId,
            Amount = 125.50m,
            TransactionExternalId = "ORIGINAL-501",
            Reference = "REF-501",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            TraceNumber = "123456780000001",
            EffectiveEntryDate = DateTime.UtcNow.Date,
            State = AchTransferStateEnum.Pending,
            SourceAccountNumber = "111111",
            DestinationAccountNumber = "222222",
            OriginatingDFI = "00010001",
            ReceivingDFI = "00020002",
            AchCycleId = cycleId,
            AchBatchId = 1
        });
        context.AchReturnCodes.Add(new AchReturnCode
        {
            Id = 77,
            ClearingHouseId = clearingHouseId,
            Code = "R01",
            FlowType = AchReturnFlowType.Return,
            Description = "Fondos insuficientes",
            BusinessOutcome = IncomingNachaBusinessOutcome.Returned,
            AppliesToCredit = true,
            AppliesToReturn = true,
            RequiresAddenda = true,
            EffectiveFrom = DateTime.UtcNow.Date.AddYears(-1),
            IsActive = true,
            RegulatorySource = "ACH Colombia V35"
        });
        context.IncomingNachaFileIngestions.Add(new IncomingNachaFileIngestion
        {
            Id = ingestionId,
            FileName = "0001000.001.20260808.1.OUT",
            FileHashSha256 = new string('A', 64),
            FileSize = 1060,
            ContentType = "text/plain",
            UploadedBy = "uat",
            CorrelationId = "RET-ORPHAN-E2E",
            Notes = "fixture controlado",
            ResolvedClearingHouseId = clearingHouseId,
            ResolvedAchCycleId = cycleId,
            OperationalDate = DateTime.UtcNow.Date,
            ReceivedAtUtc = DateTime.UtcNow
        });
        context.EntryDetails.Add(new EntryDetail
        {
            EntryDetailID = 601,
            TransactionCode = "21",
            ReceivingParticipantEntityCode = "00020002",
            AccountNumber = "222222",
            Amount = 125.50m,
            RecipIdNumber = "CONTROLADO",
            RecipUserName = "Usuario controlado",
            SequenceNumber = "000100000000999"
        });
        context.AddendaRecords.Add(new AddendaRecord
        {
            AddendaID = 701,
            CodeTypeAddendumRecord = "99",
            ReturnReasonCode = "R01",
            OriginalTraceNumber = "123456780000001",
            EntryDetailSequenceNumber = "000100000000999"
        });
        context.IncomingNachaEntryClassifications.Add(new IncomingNachaEntryClassification
        {
            IncomingNachaFileIngestionId = ingestionId,
            EntryDetailId = 601,
            AddendaRecordId = 701,
            FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
            EligibilityStatus = IncomingNachaEligibilityStatus.Bloqueada,
            RequiresLink = true,
            RequiresManualResolution = true,
            OriginalTraceRef = "123456780000001",
            ReturnReasonCode = "R01",
            BusinessMeaning = "Devolución recibida sin relación inequívoca",
            ClassificationEvidenceJson = "{}"
        });
        context.IncomingNachaTransactionLinks.Add(new IncomingNachaTransactionLink
        {
            Id = linkId,
            IncomingNachaFileIngestionId = ingestionId,
            EntryDetailId = 601,
            AddendaRecordId = 701,
            LinkType = IncomingNachaLinkType.Ambiguous,
            ConfidenceScore = 0.3m,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                resolutionStatus = "Unresolved",
                resolutionReason = "Ambiguous",
                candidateTransactionIds = new[] { transactionId }
            }),
            LinkedBy = "sistema",
            IsFinal = false
        });
        context.SaveChanges();
        return (linkId, transactionId);
    }
}
