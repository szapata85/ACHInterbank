using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class CenitReturnIn2026ParserTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public CenitReturnIn2026ParserTests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("PPD", "21", "220")]
    [InlineData("CCD", "26", "225")]
    public async Task Parser_ShouldExtractNormativeCenitReturnFields(string service, string transactionCode, string serviceClass)
    {
        await using var context = await CreateContextAsync();
        var profile = await context.CfgProfiles.SingleAsync(x => x.ProfileCode == CenitReturnIn2026Layout.ProfileCode);
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        var parser = CreateParser(context);
        var content = BuildNormativeSyntheticFile(service, transactionCode, serviceClass);

        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));
        var result = await parser.ParseAndSaveDetailedAsync(stream, "1234567.001.20260815.1", new NachaParseRequest
        {
            ResolvedClearingHouseId = clearingHouse.Id,
            ResolvedAchCycleId = "CENIT-RETURN-2026",
            OperationalDate = new DateTime(2026, 8, 15),
            CorrelationId = $"cenit-return-{service}",
            SelectedProfileId = profile.Id,
            SelectedProfileCode = profile.ProfileCode,
            IncomingNachaFileIngestionId = Guid.NewGuid()
        }, CancellationToken.None);

        result.Failures.Should().BeEmpty();
        result.TotalBatches.Should().Be(1);
        result.TotalEntries.Should().Be(1);
        result.TotalAddendas.Should().Be(1);

        var batch = await context.BatchHeaders.AsNoTracking().SingleAsync();
        batch.StandardEntryClassCode.Should().Be(service);
        var entry = await context.EntryDetails.AsNoTracking().SingleAsync();
        entry.TransactionCode.Should().Be(transactionCode);
        entry.ReceivingParticipantEntityCode.Should().Be("12345678");
        entry.AccountNumber.Should().Be("00123456789012345");
        entry.Amount.Should().Be(100m);
        entry.RecipIdNumber.Should().Be("900123456");
        entry.SequenceNumber.Should().Be("876543210000001");

        var addenda = await context.AddendaRecords.AsNoTracking().SingleAsync();
        addenda.CodeTypeAddendumRecord.Should().Be("99");
        addenda.ReturnReasonCode.Should().Be("R01");
        addenda.OriginalTraceNumber.Should().Be("123456780000099");
        addenda.IdUserOrig.Should().Be("12345678");
        addenda.InfofromOriginator.Should().Be("REFERENCIA ORIGINAL PRESERVADA");
        addenda.NewTraceNumber.Should().Be("876543210000001");
        addenda.EntryDetailSequenceNumber.Should().Be("0000001");
    }

    [Fact]
    public async Task Parser_ShouldRejectUnknownCenitReturnCause()
    {
        var exception = await ParseInvalidAsync(BuildNormativeSyntheticFile("PPD", "21", "220", reason: "R99"));
        exception.Message.Should().Contain("CENIT-2026-T7-RETURN-REASON");
    }

    [Fact]
    public async Task Parser_ShouldRejectInvalidOriginalSequence()
    {
        var exception = await ParseInvalidAsync(BuildNormativeSyntheticFile("PPD", "21", "220", originalTrace: "ABC"));
        exception.Message.Should().Contain("CENIT-2026-T7-ORIGINAL-TRACE");
    }

    [Fact]
    public async Task Parser_ShouldRejectUnsupportedCtxWithoutSimulatingSupport()
    {
        var exception = await ParseInvalidAsync(BuildNormativeSyntheticFile("CTX", "21", "220"));
        exception.Message.Should().Contain(CenitReturnIn2026Layout.CtxScopeStatus);
    }

    [Fact]
    public async Task Parser_ShouldRejectReturnOfReturnInOrdinaryReturnProfile()
    {
        var exception = await ParseInvalidAsync(BuildNormativeSyntheticFile("PPD", "21", "220", reason: "R74"));
        exception.Message.Should().Contain("CENIT_ROR_NOT_ORDINARY_RETURN");
    }

    [Fact]
    public void RawContract_ShouldDistinguishOrdinaryReturnFromReturnOfReturn()
    {
        var ordinary = BuildType7("R34", "123456780000099", "876543210000001");
        CenitReturnIn2026Layout.TryParseReturnAddenda(ordinary, out var parsed).Should().BeTrue();
        parsed!.OriginalReceivingDfi.Should().Be("12345678");
        CenitReturnIn2026Layout.IsOrdinaryReturnCause(parsed.ReturnReasonCode).Should().BeTrue();
        CenitReturnIn2026Layout.IsReturnOfReturnCause(parsed.ReturnReasonCode).Should().BeFalse();

        var ror = BuildType7("R74", "123456780000099", "876543210000001");
        CenitReturnIn2026Layout.TryParseReturnAddenda(ror, out parsed).Should().BeTrue();
        CenitReturnIn2026Layout.IsReturnOfReturnCause(parsed!.ReturnReasonCode).Should().BeTrue();
        CenitReturnIn2026Layout.IsOrdinaryReturnCause(parsed.ReturnReasonCode).Should().BeFalse();
    }

    [Fact]
    public async Task ApplicationPipeline_ShouldParseCorrelateApplyAuditAndRemainIdempotent()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        await EnsureOriginalTransactionAndReturnCatalogAsync(context, clearingHouse);

        var cycleResolver = new Mock<IIncomingNachaCycleResolver>();
        cycleResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                ClearingHouseId = clearingHouse.Id,
                DetectedClearingHouseId = clearingHouse.Id,
                AchCycleId = "CENIT-RETURN-2026",
                OperationalDate = new DateTime(2026, 8, 15),
                Confidence = 1m,
                Status = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
                ResolutionMode = "NormativeSyntheticFixture",
                EvidenceJson = "{}"
            });

        var stateTransitions = new AchStateTransitionService(context);
        var dispatchPlanner = new Mock<IIncomingNachaDispatchPlanner>();
        dispatchPlanner.Setup(x => x.PlanForIngestionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        var postParse = new IncomingNachaPostParseProcessor(
            context,
            new IncomingNachaFunctionalClassifier(),
            new IncomingNachaTransactionLinker(context),
            Mock.Of<IIncomingNachaPrenotificationResolver>(),
            dispatchPlanner.Object,
            Mock.Of<IAchRegulatoryCatalogService>(),
            stateTransitions);
        var parser = CreateParser(context, stateTransitions);
        var ingestion = new IncomingNachaIngestionAppService(
            context,
            cycleResolver.Object,
            parser,
            postParse,
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var content = BuildNormativeSyntheticFile("PPD", "26", "225");
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));
        var response = await ingestion.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = stream,
            FileName = "1234567.001.20260815.1",
            ContentType = "text/plain",
            RequestedBy = "ret-cenit-in-001",
            CorrelationId = "ret-cenit-in-001-e2e",
            RequestedClearingHouseId = clearingHouse.Id
        }, CancellationToken.None);

        response.ParsingStatus.Should().Be(IncomingNachaParsingStatus.Exitoso);
        response.SelectedProfileCode.Should().Be(CenitReturnIn2026Layout.ProfileCode);
        response.TotalEntries.Should().Be(1);
        response.TotalAddendas.Should().Be(1);

        var classification = await context.IncomingNachaEntryClassifications.SingleAsync();
        classification.FunctionalClass.Should().Be(IncomingNachaFunctionalClass.Devolucion);
        classification.ReturnReasonCode.Should().Be("R01");
        classification.RequiresManualResolution.Should().BeFalse();
        var link = await context.IncomingNachaTransactionLinks.SingleAsync();
        link.LinkType.Should().Be(IncomingNachaLinkType.ExactOriginalTraceRef);
        link.AchTransactionId.Should().Be(91001);
        var transaction = await context.AchTransactions.SingleAsync(x => x.Id == 91001);
        transaction.State.Should().Be(AchTransferStateEnum.ReturnedByEpr);
        transaction.ReturnReasonCode.Should().Be("R01");
        (await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "TransicionDisparada")).Should().Be(1);
        (await context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 91001)).Should().Be(1);

        await postParse.ProcessAsync(response.IngestionId, "ret-cenit-in-001-replay", CancellationToken.None);

        (await context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 91001)).Should().Be(1);
        (await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "EventoDuplicadoIgnorado")).Should().Be(1);
    }

    private async Task<InvalidOperationException> ParseInvalidAsync(string content)
    {
        await using var context = await CreateContextAsync();
        var profile = await context.CfgProfiles.SingleAsync(x => x.ProfileCode == CenitReturnIn2026Layout.ProfileCode);
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        var parser = CreateParser(context);
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));
        return await Assert.ThrowsAsync<InvalidOperationException>(() => parser.ParseAndSaveDetailedAsync(stream, "1234567.001.20260815.1", new NachaParseRequest
        {
            ResolvedClearingHouseId = clearingHouse.Id,
            ResolvedAchCycleId = "CENIT-RETURN-2026",
            OperationalDate = new DateTime(2026, 8, 15),
            CorrelationId = "cenit-return-invalid",
            SelectedProfileId = profile.Id,
            SelectedProfileCode = profile.ProfileCode,
            IncomingNachaFileIngestionId = Guid.NewGuid()
        }, CancellationToken.None));
    }

    private static NachaParserService CreateParser(AchDbContext context, IAchStateTransitionService? stateTransitionService = null) => new(
        context,
        Mock.Of<ILogger<NachaParserService>>(),
        stateTransitionService ?? Mock.Of<IAchStateTransitionService>());

    private Task<AchDbContext> CreateContextAsync() => _fixture.CreateSeededContextAsync();

    private static async Task<ClearingHouse> EnsureCenitOperationalContextAsync(AchDbContext context)
    {
        var clearingHouse = await context.ClearingHouses.FirstOrDefaultAsync(x => x.Code == "CENIT");
        if (clearingHouse is null)
        {
            clearingHouse = new ClearingHouse { Id = 7001, Code = "CENIT", Name = "CENIT", OriginCode = "87654321" };
            context.ClearingHouses.Add(clearingHouse);
        }

        if (!await context.AchCycles.AnyAsync(x => x.Id == "CENIT-RETURN-2026"))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = "CENIT-RETURN-2026",
                CycleName = "Ciclo 2",
                ProcessingDate = new DateTime(2026, 8, 15),
                CutoffTime = new TimeSpan(18, 0, 0),
                ClearingHouse = clearingHouse
            });
        }

        await context.SaveChangesAsync();
        return clearingHouse;
    }

    private static async Task EnsureOriginalTransactionAndReturnCatalogAsync(AchDbContext context, ClearingHouse clearingHouse)
    {
        if (!await context.AchCycles.AnyAsync(x => x.Id == "CENIT-ORIGINAL-2026"))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = "CENIT-ORIGINAL-2026",
                CycleName = "Ciclo 1",
                ProcessingDate = new DateTime(2026, 8, 15),
                CutoffTime = new TimeSpan(10, 0, 0),
                ClearingHouseId = clearingHouse.Id
            });
        }

        context.AchTransactions.Add(new AchTransaction
        {
            Id = 91001,
            TraceNumber = "123456780000099",
            OriginalTraceRef = "123456780000099",
            AchCycleId = "CENIT-ORIGINAL-2026",
            Type = TransactionTypeEnum.Debit,
            State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = new DateTime(2026, 8, 15),
            TransactionCode = "27",
            ReceivingDFI = "12345678",
            OriginatingDFI = "87654321",
            Amount = 100m,
            Reference = "RET-CENIT-IN-001",
            SourceAccountNumber = "ORIGIN-001",
            DestinationAccountNumber = "00123456789012345",
            RecipientIdNumber = "900123456"
        });

        var returnCode = await context.AchReturnCodes.FirstOrDefaultAsync(x => x.ClearingHouseId == clearingHouse.Id && x.Code == "R01");
        if (returnCode is null)
        {
            context.AchReturnCodes.Add(new AchReturnCode
            {
                ClearingHouseId = clearingHouse.Id,
                Code = "R01",
                FlowType = AchReturnFlowType.Return,
                Description = "Fondos insuficientes",
                BusinessOutcome = IncomingNachaBusinessOutcome.Returned,
                AppliesToDebit = true,
                RequiresAddenda = true,
                EffectiveFrom = new DateTime(2023, 1, 1),
                IsActive = true,
                RegulatorySource = "CENIT Anexo A"
            });
        }

        await context.SaveChangesAsync();
    }

    private static Mock<IExternalFileNamePolicy> BuildExternalPolicyMock()
    {
        var policy = new Mock<IExternalFileNamePolicy>();
        policy.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext request, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = request.ProvidedExternalFileName ?? request.InternalFileName ?? "incoming",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed },
                CorrelationEvidence = new ExternalFileNameCorrelationEvidence(),
                Components = new ExternalFileNameComponents { FullName = request.ProvidedExternalFileName ?? request.InternalFileName ?? "incoming" }
            });
        return policy;
    }

    private static string BuildNormativeSyntheticFile(
        string service,
        string transactionCode,
        string serviceClass,
        string reason = "R01",
        string originalTrace = "123456780000099")
    {
        const string receivingDfi = "12345678";
        const string originDfi = "87654321";
        const long amountCents = 10_000;
        var debit = transactionCode is "26" or "36" or "56" ? amountCents : 0;
        var credit = debit == 0 ? amountCents : 0;
        var records = new List<string>
        {
            BuildType1(),
            BuildType5(service, serviceClass, originDfi),
            BuildType6(transactionCode, receivingDfi, originDfi, amountCents),
            BuildType7(reason, originalTrace, $"{originDfi}0000001"),
            BuildType8(serviceClass, receivingDfi, originDfi, debit, credit),
            BuildType9(receivingDfi, debit, credit)
        };
        records.AddRange(Enumerable.Repeat(new string('9', 106), 4));
        return string.Concat(records);
    }

    private static string BuildType1()
    {
        var line = Blank('1');
        Put(line, 2, 2, "01");
        Put(line, 4, 10, "0000000000");
        Put(line, 14, 10, "0000000000");
        Put(line, 24, 8, "20260815");
        Put(line, 32, 4, "1200");
        Put(line, 36, 1, "A");
        Put(line, 37, 3, "106");
        Put(line, 40, 2, "10");
        Put(line, 42, 1, "1");
        Put(line, 43, 23, "CENIT");
        Put(line, 66, 23, "CFA");
        return new string(line);
    }

    private static string BuildType5(string service, string serviceClass, string originDfi)
    {
        var line = Blank('5');
        Put(line, 2, 3, serviceClass);
        Put(line, 5, 16, "CFA PRUEBAS");
        Put(line, 41, 10, "9001234567");
        Put(line, 51, 3, service);
        Put(line, 54, 10, "DEVOLUCION");
        Put(line, 64, 8, "20260815");
        Put(line, 72, 8, "20260815");
        Put(line, 83, 1, "1");
        Put(line, 84, 8, originDfi);
        Put(line, 92, 7, "0000001");
        return new string(line);
    }

    private static string BuildType6(string transactionCode, string receivingDfi, string originDfi, long amountCents)
    {
        var line = Blank('6');
        Put(line, 2, 2, transactionCode);
        Put(line, 4, 8, receivingDfi);
        Put(line, 12, 1, DigitoChequeoHelper.CalcularDigitoChequeo(receivingDfi));
        Put(line, 13, 17, "00123456789012345");
        Put(line, 30, 18, amountCents.ToString().PadLeft(18, '0'));
        Put(line, 48, 15, "900123456");
        Put(line, 63, 22, "USUARIO PRUEBA");
        Put(line, 87, 1, "1");
        Put(line, 88, 15, $"{originDfi}0000001");
        return new string(line);
    }

    private static string BuildType7(string reason, string originalTrace, string sequence)
    {
        var line = Blank('7');
        Put(line, 2, 2, "99");
        Put(line, 4, 3, reason);
        Put(line, 7, 15, originalTrace);
        Put(line, 30, 8, "12345678");
        Put(line, 38, 44, "REFERENCIA ORIGINAL PRESERVADA");
        Put(line, 82, 15, sequence);
        return new string(line);
    }

    private static string BuildType8(string serviceClass, string receivingDfi, string originDfi, long debit, long credit)
    {
        var line = Blank('8');
        Put(line, 2, 3, serviceClass);
        Put(line, 5, 6, "000002");
        Put(line, 11, 10, long.Parse(receivingDfi).ToString().PadLeft(10, '0'));
        Put(line, 21, 18, debit.ToString().PadLeft(18, '0'));
        Put(line, 39, 18, credit.ToString().PadLeft(18, '0'));
        Put(line, 57, 10, "9001234567");
        Put(line, 92, 8, originDfi);
        Put(line, 100, 7, "0000001");
        return new string(line);
    }

    private static string BuildType9(string receivingDfi, long debit, long credit)
    {
        var line = Blank('9');
        Put(line, 2, 6, "000001");
        Put(line, 8, 6, "000001");
        Put(line, 14, 8, "00000002");
        Put(line, 22, 10, long.Parse(receivingDfi).ToString().PadLeft(10, '0'));
        Put(line, 32, 18, debit.ToString().PadLeft(18, '0'));
        Put(line, 50, 18, credit.ToString().PadLeft(18, '0'));
        return new string(line);
    }

    private static char[] Blank(char recordType)
    {
        var line = Enumerable.Repeat(' ', 106).ToArray();
        line[0] = recordType;
        return line;
    }

    private static void Put(char[] target, int startPosition, int length, string value)
    {
        var formatted = value.Length >= length ? value[..length] : value.PadRight(length);
        Array.Copy(formatted.ToCharArray(), 0, target, startPosition - 1, length);
    }
}
