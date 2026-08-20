using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitReturnOfReturn2026Tests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public CenitReturnOfReturn2026Tests(OfficialNachaGenerationFixture fixture) => _fixture = fixture;

    [Fact]
    public void NormativeSyntheticFixture_ShouldPreserveAnnex17Contract()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "Nacha",
            "NormativeSynthetic",
            "Cenit",
            "Ror",
            "CENIT_ROR_IN_001.ach");

        var records = File.ReadAllLines(path, Encoding.Latin1);

        records.Should().HaveCount(10);
        records.Should().OnlyContain(value => value.Length == CenitReturnOfReturn2026Layout.RecordLength);
        records.Take(6).Select(value => value[0]).Should().Equal('1', '5', '6', '7', '8', '9');
        records.Skip(6).Should().OnlyContain(value => value == new string('9', CenitReturnOfReturn2026Layout.RecordLength));

        var addenda = records[3];
        addenda.Substring(1, 2).Should().Be("99");
        addenda.Substring(3, 3).Should().Be("R61");
        addenda.Substring(6, 15).Should().Be("223456780003312");
        addenda.Substring(21, 8).Should().Be(new string(' ', 8));
        addenda.Substring(29, 8).Should().Be("12345678");
        addenda.Substring(37, 3).Should().Be(new string(' ', 3));
        addenda.Substring(40, 15).Should().Be("765432158175914");
        addenda.Substring(55, 3).Should().Be("228");
        addenda.Substring(58, 2).Should().Be("02");
        addenda.Substring(60, 21).Should().Be(new string(' ', 21));
        addenda.Substring(81, 15).Should().Be("876543259602872");
        addenda.Substring(96, 10).Should().Be(new string(' ', 10));
    }

    [Fact]
    public async Task Builder_ShouldGenerateNormativeSyntheticRorPpd_WithAnnex17Positions()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var builder = CreateBuilder(context);
        var entry = new NachaReturnOutEntry(
            1, "21", "12345678", "0", "00123456789012345", 100m,
            "900123456", "RECEPTOR CENIT", "AB", "876543210000001",
            "R60", "123456780000099", string.Empty, "87654321", string.Empty,
            "876543210000001", "765432100000111", "228", "02");

        var result = await builder.BuildReturnOutAsync(new NachaReturnOutBuildRequest(
            new DateTime(2026, 8, 16, 14, 30, 0, DateTimeKind.Utc),
            "A", "0123456789", "0987654321", "CENIT", "CFA", "ROR",
            [new NachaReturnOutBatch("220", "ORIGINADOR CENIT", string.Empty, "9001234567", "PPD", "ROR",
                new DateTime(2026, 8, 16), new DateTime(2026, 8, 16), "228", "87654321", 1, [entry])],
            ClearingHouseCode: "CENIT",
            ClearingHouseName: "CENIT",
            NormativeVersion: CenitReturnOfReturn2026Layout.NormativeVersion,
            FlowTypeCode: CenitReturnOfReturn2026Layout.FlowTypeCode));

        var records = Split(result.Content);
        result.ProfileCode.Should().Be(CenitReturnOfReturn2026Layout.OutProfileCode);
        records.Should().HaveCount(10);
        records.Should().OnlyContain(value => value.Length == 106);
        records.Take(6).Select(value => value[0]).Should().Equal('1', '5', '6', '7', '8', '9');
        var addenda = records[3];
        addenda.Substring(1, 2).Should().Be("99");
        addenda.Substring(3, 3).Should().Be("R60");
        addenda.Substring(6, 15).Should().Be("123456780000099");
        addenda.Substring(21, 8).Should().Be(new string(' ', 8));
        addenda.Substring(29, 8).Should().Be("87654321");
        addenda.Substring(37, 3).Should().Be(new string(' ', 3));
        addenda.Substring(40, 15).Should().Be("765432100000111");
        addenda.Substring(55, 3).Should().Be("228");
        addenda.Substring(58, 2).Should().Be("02");
        addenda.Substring(60, 21).Should().Be(new string(' ', 21));
        addenda.Substring(81, 15).Should().Be("876543210000001");
        CenitReturnOfReturn2026Layout.TryParseAddenda(addenda, out var parsed).Should().BeTrue();
        parsed!.SourceReturnTraceNumber.Should().Be("765432100000111");
    }

    [Fact]
    public async Task Parser_ShouldExtractAnnex17FieldsFromNormativeSyntheticRor()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var builder = CreateBuilder(context);
        var generated = await builder.BuildReturnOutAsync(BuildMinimalRequest("PPD"));
        var profile = await context.CfgProfiles.SingleAsync(x => x.ProfileCode == CenitReturnOfReturn2026Layout.InProfileCode);
        var clearingHouse = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == "CENIT");
        if (clearingHouse is null)
        {
            clearingHouse = new ClearingHouse { Id = 7701, Code = "CENIT", Name = "CENIT", OriginCode = "87654321" };
            context.ClearingHouses.Add(clearingHouse);
        }
        context.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-ROR-IN-2026",
            CycleName = "Ciclo ROR",
            ProcessingDate = new DateTime(2026, 8, 16),
            CutoffTime = TimeSpan.FromHours(18),
            ClearingHouse = clearingHouse
        });
        await context.SaveChangesAsync();

        var parser = new NachaParserService(context, Mock.Of<Microsoft.Extensions.Logging.ILogger<NachaParserService>>(), Mock.Of<IAchStateTransitionService>());
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(generated.Content));
        var parsed = await parser.ParseAndSaveDetailedAsync(stream, "1234567.001.20260816.1", new NachaParseRequest
        {
            ResolvedClearingHouseId = clearingHouse.Id,
            ResolvedAchCycleId = "CENIT-ROR-IN-2026",
            OperationalDate = new DateTime(2026, 8, 16),
            CorrelationId = "ret-cenit-ror-001-parser",
            SelectedProfileId = profile.Id,
            SelectedProfileCode = profile.ProfileCode,
            IncomingNachaFileIngestionId = Guid.NewGuid()
        }, CancellationToken.None);

        parsed.Failures.Should().BeEmpty();
        var addenda = await context.AddendaRecords.AsNoTracking().SingleAsync();
        addenda.BusinessType.Should().Be("ReturnOfReturn");
        addenda.ReturnReasonCode.Should().Be("R60");
        addenda.OriginalTraceNumber.Should().Be("123456780000099");
        addenda.IdUserOrig.Should().Be("12345678");
        addenda.NewTraceNumber.Should().Be("765432100000111");
        addenda.PurposeOfTransaction.Should().Be("228");
        addenda.InvoiceOrAccountNumber.Should().Be("02");
    }

    [Theory]
    [InlineData("CCD", "CENIT_ROR_CCD_NOT_NORMATIVELY_DEFINED")]
    [InlineData("CTX", "CTX_OUT_OF_CURRENT_PLATFORM_SCOPE")]
    public async Task Builder_ShouldRejectServicesWithoutSpecificRorContract(string sec, string expected)
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var builder = CreateBuilder(context);
        var request = BuildMinimalRequest(sec);

        var exception = await Assert.ThrowsAsync<NachaGenerationException>(() => builder.BuildReturnOutAsync(request));
        exception.Code.Should().Be(expected);
    }

    [Fact]
    public void Classifier_ShouldSeparateRorFromOrdinaryReturn()
    {
        var classifier = new IncomingNachaFunctionalClassifier();
        var result = classifier.Classify(
            new EntryDetail { TransactionCode = "21", Amount = 100m },
            new AddendaRecord
            {
                CodeTypeAddendumRecord = "99",
                BusinessType = "ReturnOfReturn",
                ReturnReasonCode = "R60",
                OriginalTraceNumber = "123456780000099",
                NewTraceNumber = "765432100000111"
            });

        result.FunctionalClass.Should().Be(IncomingNachaFunctionalClass.DevolucionDevolucion);
        result.RequiresLink.Should().BeTrue();
        result.OriginalTraceRef.Should().Be("765432100000111");
    }

    [Fact]
    public async Task Service_ShouldPersistBothDirections_AndRemainIdempotent()
    {
        await using var context = BuildContext();
        var original = SeedDomain(context);
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.ValidateReturnOfReturnAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, true));
        var sequence = new Mock<IAchReturnTraceSequenceService>();
        sequence.Setup(x => x.ReserveRangeAsync("87654321", It.IsAny<DateOnly>(), 1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnTraceRange(1, 1));
        var sut = new CenitReturnOfReturnService(context, regulatory.Object, sequence.Object, new AchReturnGenerationLockService(), Mock.Of<IOperationalCalendarService>());

        var outResult = await sut.CreateOutgoingAsync(new(1001, "R60", "C4", new DateTime(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc)));
        var outDuplicate = await sut.CreateOutgoingAsync(new(1001, "R60", "C4", new DateTime(2026, 8, 16, 10, 1, 0, DateTimeKind.Utc)));
        outResult.IsSuccessful.Should().BeTrue();
        outDuplicate.WasDuplicate.Should().BeTrue();

        var incoming = await sut.IngestIncomingAsync(new(
            2001, original.Id, "C4", "R61", "21", "123456780000555", original.TraceNumber,
            "12345678", "765432100000111", "228", "02", 100m,
            new DateTime(2026, 8, 16, 11, 0, 0, DateTimeKind.Utc), "ror-in-1"));
        var incomingDuplicate = await sut.IngestIncomingAsync(new(
            2001, original.Id, "C4", "R61", "21", "123456780000555", original.TraceNumber,
            "12345678", "765432100000111", "228", "02", 100m,
            new DateTime(2026, 8, 16, 11, 1, 0, DateTimeKind.Utc), "ror-in-1"));

        incoming.IsSuccessful.Should().BeTrue();
        incomingDuplicate.WasDuplicate.Should().BeTrue();
        var flows = await context.ReturnOfReturnFlows.AsNoTracking().ToListAsync();
        flows.Should().HaveCount(2);
        flows.Single(x => x.Direction == "Out").ParentIncomingReturnStateEventId.Should().Be(1001);
        flows.Single(x => x.Direction == "In").ParentOutgoingReturnGeneratedId.Should().Be(2001);
        flows.Should().OnlyContain(x => x.OriginalTransactionId == original.Id);
    }

    [Fact]
    public async Task ApplicationPipeline_ShouldIngestCorrelatePersistAndReplayRorInIdempotently()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var original = SeedDomain(context);
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.ValidateReturnOfReturnAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, true));
        var rorService = new CenitReturnOfReturnService(context, regulatory.Object, Mock.Of<IAchReturnTraceSequenceService>(), new AchReturnGenerationLockService(), Mock.Of<IOperationalCalendarService>());
        var dispatch = new Mock<IIncomingNachaDispatchPlanner>();
        dispatch.Setup(x => x.PlanForIngestionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var stateTransitions = new AchStateTransitionService(context);
        var postParse = new IncomingNachaPostParseProcessor(
            context,
            new IncomingNachaFunctionalClassifier(),
            new IncomingNachaTransactionLinker(context),
            Mock.Of<IIncomingNachaPrenotificationResolver>(),
            dispatch.Object,
            regulatory.Object,
            stateTransitions,
            cenitReturnOfReturnService: rorService);
        var cycleResolver = new Mock<IIncomingNachaCycleResolver>();
        cycleResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                ClearingHouseId = 7001,
                DetectedClearingHouseId = 7001,
                AchCycleId = "C4",
                OperationalDate = new DateTime(2026, 8, 16),
                Confidence = 1m,
                Status = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
                ResolutionMode = "NormativeSyntheticFixture",
                EvidenceJson = "{}"
            });
        var externalName = new Mock<IExternalFileNamePolicy>();
        externalName.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext request, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = request.ProvidedExternalFileName ?? request.InternalFileName ?? "incoming",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed },
                CorrelationEvidence = new ExternalFileNameCorrelationEvidence(),
                Components = new ExternalFileNameComponents { FullName = request.ProvidedExternalFileName ?? request.InternalFileName ?? "incoming" }
            });
        var parser = new NachaParserService(context, Mock.Of<ILogger<NachaParserService>>(), stateTransitions);
        var ingestion = new IncomingNachaIngestionAppService(
            context, cycleResolver.Object, parser, postParse, externalName.Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());
        var generated = await CreateBuilder(context).BuildReturnOutAsync(BuildMinimalRequest("PPD"));

        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(generated.Content));
        var response = await ingestion.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = stream,
            FileName = "1234567.001.20260816.1",
            ContentType = "text/plain",
            RequestedBy = "ret-cenit-ror-001",
            CorrelationId = "ret-cenit-ror-001-e2e",
            RequestedClearingHouseId = 7001
        }, CancellationToken.None);

        response.ParsingStatus.Should().Be(IncomingNachaParsingStatus.Exitoso);
        response.SelectedProfileCode.Should().Be(CenitReturnOfReturn2026Layout.InProfileCode);
        var persistedIngestion = await context.IncomingNachaFileIngestions.SingleAsync(x => x.Id == response.IngestionId);
        persistedIngestion.OperationalDate.Should().Be(new DateTime(2026, 8, 16));
        persistedIngestion.ReceivedAtUtc.Should().NotBeNull();
        persistedIngestion.ReceivedAtUtc!.Value.Date.Should().NotBe(persistedIngestion.OperationalDate!.Value.Date);
        var classification = await context.IncomingNachaEntryClassifications.SingleAsync();
        classification.FunctionalClass.Should().Be(IncomingNachaFunctionalClass.DevolucionDevolucion);
        var link = await context.IncomingNachaTransactionLinks.SingleAsync(x => x.EntryDetailId != 30);
        link.AchTransactionId.Should().Be(original.Id);
        var flow = await context.ReturnOfReturnFlows.SingleAsync();
        flow.Direction.Should().Be("In");
        flow.ParentOutgoingReturnGeneratedId.Should().Be(2001);
        flow.OrchestratedAtUtc.Date.Should().Be((persistedIngestion.EffectiveDate ?? persistedIngestion.OperationalDate)!.Value.Date);

        await postParse.ProcessAsync(response.IngestionId, "ret-cenit-ror-001-replay", CancellationToken.None);
        context.ReturnOfReturnFlows.Should().ContainSingle();
        (await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "CenitRorDuplicadoIgnorado")).Should().Be(1);
    }

    [Fact]
    public async Task Service_ShouldRejectAlteredPreservedParentFields()
    {
        await using var context = BuildContext();
        var original = SeedDomain(context);
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.ValidateReturnOfReturnAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, true));
        var sut = new CenitReturnOfReturnService(context, regulatory.Object, Mock.Of<IAchReturnTraceSequenceService>(), new AchReturnGenerationLockService(), Mock.Of<IOperationalCalendarService>());

        var result = await sut.IngestIncomingAsync(new(
            2001, original.Id, "C4", "R63", "21", "123456780000555", original.TraceNumber,
            "99999999", "765432100000111", "228", "02", 100m,
            new DateTime(2026, 8, 16, 11, 0, 0, DateTimeKind.Utc), "ror-in-altered"));

        result.IsSuccessful.Should().BeFalse();
        result.Code.Should().Be("ROR_PARENT_FIELDS_MISMATCH");
        context.ReturnOfReturnFlows.Should().BeEmpty();
    }

    [Fact]
    public async Task Service_ShouldRejectSameDayRorOutsideLastCycleBeforeReservingTrace()
    {
        await using var context = BuildContext();
        SeedDomain(context);
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.ValidateReturnOfReturnAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, true));
        var sequence = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        var sut = new CenitReturnOfReturnService(
            context, regulatory.Object, sequence.Object, new AchReturnGenerationLockService(), Mock.Of<IOperationalCalendarService>());

        var result = await sut.CreateOutgoingAsync(new(
            1001, "R60", "C3", new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc)));

        result.IsSuccessful.Should().BeFalse();
        result.Code.Should().Be("CENIT_ROR_SAME_DAY_CYCLE_INVALID");
        sequence.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Service_ShouldRejectRorAfterNextBusinessDayDeadline()
    {
        await using var context = BuildContext();
        SeedDomain(context);
        context.AchCycles.Add(new AchCycle
        {
            Id = "NEXT-C2", CycleName = "Ciclo 2", ProcessingDate = new(2026, 8, 18),
            CutoffTime = TimeSpan.FromHours(11), ClearingHouseId = 7001
        });
        await context.SaveChangesAsync();
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.ValidateReturnOfReturnAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, true));
        var calendar = new Mock<IOperationalCalendarService>();
        calendar.Setup(x => x.GetNextBusinessDayAsync(new DateOnly(2026, 8, 17), 7001, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateOnly(2026, 8, 17));
        var sequence = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        var sut = new CenitReturnOfReturnService(
            context, regulatory.Object, sequence.Object, new AchReturnGenerationLockService(), calendar.Object);

        var result = await sut.CreateOutgoingAsync(new(
            1001, "R68", "NEXT-C2", new DateTime(2026, 8, 18, 10, 0, 0, DateTimeKind.Utc)));

        result.IsSuccessful.Should().BeFalse();
        result.Code.Should().Be("CENIT_ROR_DEADLINE_EXCEEDED");
        sequence.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Service_ShouldRejectReturnOfReturnOverAnotherReturnOfReturn()
    {
        await using var context = BuildContext();
        var original = SeedDomain(context);
        context.ReturnOfReturnFlows.Add(new ReturnOfReturnFlow
        {
            ReturnOfReturnTransactionId = original.Id,
            ReasonCode = "R60",
            Direction = "In",
            Status = "Applied"
        });
        await context.SaveChangesAsync();
        var sequence = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        var sut = new CenitReturnOfReturnService(
            context,
            Mock.Of<IAchRegulatoryCatalogService>(),
            sequence.Object,
            new AchReturnGenerationLockService(),
            Mock.Of<IOperationalCalendarService>());

        var result = await sut.CreateOutgoingAsync(new(
            1001, "R61", "C4", new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc)));

        result.IsSuccessful.Should().BeFalse();
        result.Code.Should().Be("ROR_ON_ROR_NOT_ALLOWED");
        sequence.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplicationPipeline_ShouldCreateAndGenerateRorOutWithNormativeProfileIdempotently()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        SeedDomain(context);
        var regulatory = new Mock<IAchRegulatoryCatalogService>();
        regulatory.Setup(x => x.ValidateReturnOfReturnAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null, true));
        var sequence = new Mock<IAchReturnTraceSequenceService>();
        sequence.Setup(x => x.ReserveRangeAsync("87654321", It.IsAny<DateOnly>(), 1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnTraceRange(7, 7));
        var service = new CenitReturnOfReturnService(context, regulatory.Object, sequence.Object, new AchReturnGenerationLockService(), Mock.Of<IOperationalCalendarService>());
        var created = await service.CreateOutgoingAsync(new(1001, "R60", "C4", new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc)));
        var duplicate = await service.CreateOutgoingAsync(new(1001, "R60", "C4", new DateTime(2026, 8, 16, 12, 1, 0, DateTimeKind.Utc)));
        created.IsSuccessful.Should().BeTrue();
        duplicate.WasDuplicate.Should().BeTrue();
        sequence.Verify(x => x.ReserveRangeAsync("87654321", It.IsAny<DateOnly>(), 1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);

        var naming = new Mock<IExternalFileNamePolicy>();
        naming.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "1234567.001.20260816.1",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed }
            });
        var generator = new AchReturnOfReturnFileGenerationService(
            context,
            naming.Object,
            nachaFileBuilder: CreateBuilder(context));
        var generated = await generator.GenerateNachaAsync(new(
            [(int)created.FlowId!.Value],
            new DateTime(2026, 8, 16, 12, 5, 0, DateTimeKind.Utc),
            "ret-cenit-ror-001",
            "api"), CancellationToken.None);
        var regenerated = await generator.GenerateNachaAsync(new(
            [(int)created.FlowId.Value],
            new DateTime(2026, 8, 16, 12, 6, 0, DateTimeKind.Utc),
            "ret-cenit-ror-001",
            "api"), CancellationToken.None);

        generated.IsGenerated.Should().BeTrue(string.Join(" | ", generated.Failures.Select(x => $"{x.Code}:{x.Message}")));
        generated.FileName.Should().Be("1234567.001.20260816.1");
        var records = Split(generated.ContentText!);
        records.Should().HaveCount(10);
        records[3].Substring(3, 3).Should().Be("R60");
        records[3].Substring(40, 15).Should().Be("765432100000222");
        generated.AuditId.Should().NotBeNull();
        regenerated.IsGenerated.Should().BeFalse();
        regenerated.Failures.Should().Contain(x => x.Code == "DUPLICATE_PRODUCTIVE_GENERATION");
    }

    private static NachaFileBuilder CreateBuilder(AchDbContext context)
        => new(context, Mock.Of<IBankHoliday>(), Mock.Of<INachaDataLoader>(), Mock.Of<INachaTransactionValidationService>(),
            Mock.Of<INachaFixedWidthRecordRenderer>(), Mock.Of<INachaRecordDataProvider>(), Mock.Of<INachaSemanticValidator>(),
            configResolver: new NachaConfigResolver(context));

    private static NachaReturnOutBuildRequest BuildMinimalRequest(string sec)
        => new(new DateTime(2026, 8, 16), "A", "0123456789", "0987654321", "CENIT", "CFA", "ROR",
            [new NachaReturnOutBatch("220", "CENIT", "", "9001234567", sec, "ROR", new(2026, 8, 16), new(2026, 8, 16), "228", "87654321", 1,
                [new(1, "21", "12345678", "0", "1", 100m, "1", "N", "", "876543210000001", "R60", "123456780000099", "", "12345678", "", "876543210000001", "765432100000111", "228", "02")])],
            ClearingHouseCode: "CENIT", ClearingHouseName: "CENIT",
            NormativeVersion: CenitReturnOfReturn2026Layout.NormativeVersion,
            FlowTypeCode: CenitReturnOfReturn2026Layout.FlowTypeCode);

    private static IReadOnlyList<string> Split(string content)
        => Enumerable.Range(0, content.Length / 106).Select(index => content.Substring(index * 106, 106)).ToList();

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AchTransaction SeedDomain(AchDbContext context)
    {
        context.ClearingHouses.Add(new ClearingHouse { Id = 7001, Code = "CENIT", Name = "CENIT", OriginCode = "87654321" });
        context.AchCycles.AddRange(
            new AchCycle { Id = "C1", CycleName = "Ciclo 1", ProcessingDate = new(2026, 8, 16), CutoffTime = TimeSpan.FromHours(9), ClearingHouseId = 7001 },
            new AchCycle { Id = "C2", CycleName = "Ciclo 2", ProcessingDate = new(2026, 8, 16), CutoffTime = TimeSpan.FromHours(11), ClearingHouseId = 7001 },
            new AchCycle { Id = "C3", CycleName = "Ciclo 3", ProcessingDate = new(2026, 8, 16), CutoffTime = TimeSpan.FromHours(13), ClearingHouseId = 7001 },
            new AchCycle { Id = "C4", CycleName = "Ciclo 4", ProcessingDate = new(2026, 8, 16), CutoffTime = TimeSpan.FromHours(15), ClearingHouseId = 7001 });
        context.AchBatches.Add(new AchBatch { Id = 10, AchCycleId = "C1", CompanyName = "CFA", CompanyIdentification = "9001234567", EffectiveEntryDate = new(2026, 8, 16) });
        var original = new AchTransaction
        {
            Id = 100, AchCycleId = "C1", AchBatchId = 10, Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.ReturnedByEpr, Amount = 100m, TransactionCode = "22",
            TraceNumber = "123456780000099", OriginatingDFI = "87654321", ReceivingDFI = "12345678",
            SourceAccountNumber = "1", DestinationAccountNumber = "2", Reference = "REF", TransactionExternalId = "EXT",
            CompanyName = "CFA", CompanyIdentification = "9001234567", EffectiveEntryDate = new(2026, 8, 15)
        };
        context.AchTransactions.Add(original);
        context.AchTransactionStateEvents.Add(new AchTransactionStateEvent
        {
            Id = 1001, AchTransactionId = original.Id, FromState = AchTransferStateEnum.Certified,
            ToState = AchTransferStateEnum.ReturnedByEpr, Source = AchStateEventSourceEnum.Epr,
            ReasonCode = "R02", OccurredAtUtc = new(2026, 8, 15, 10, 0, 0, DateTimeKind.Utc)
        });
        var header = new NachaHeader { NachaID = "N1", AchCycleId = "C2", ClearingHouseId = 7001 };
        var batch = new BatchHeader { BatchID = 20, NachaID = "N1", StandardEntryClassCode = "PPD", CompensationDate = "228" };
        var entry = new EntryDetail { EntryDetailID = 30, NachaID = "N1", BatchHeaderId = 20, TransactionCode = "21", ReceivingParticipantEntityCode = "12345678", CheckDigit = "0", AccountNumber = "2", Amount = 100m, RecipIdNumber = "900123456", RecipUserName = "RECEPTOR CENIT", DiscreData = "AB", SequenceNumber = "765432100000222" };
        var addenda = new AddendaRecord { AddendaID = 40, NachaID = "N1", EntryDetailId = 30, CodeTypeAddendumRecord = "99", ReturnReasonCode = "R02", OriginalTraceNumber = original.TraceNumber, IdUserOrig = "12345678" };
        context.AddRange(header, batch, entry, addenda);
        context.IncomingNachaTransactionLinks.Add(new IncomingNachaTransactionLink { Id = Guid.NewGuid(), EntryDetailId = 30, AddendaRecordId = 40, AchTransactionId = original.Id, IsFinal = true });
        context.Set<AchReturnGenerated>().Add(new AchReturnGenerated
        {
            Id = 2001, OriginalTransactionId = original.Id, ReturnCycleId = "C2", ReturnReasonCode = "R02",
            Amount = 100m, NewSequenceNumber = "765432100000111", OriginalSequenceNumber = original.TraceNumber,
            ReceiverEntityCode = "87654321", OriginatorEntityCode = "12345678", SequenceDate = new(2026, 8, 16), GeneratedAtUtc = new(2026, 8, 16)
        });
        context.SaveChanges();
        return original;
    }
}
