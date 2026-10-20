using System.Text;
using Cfa.ACHInterbank.Application.ACH.Configuration;
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
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitReturnOut2026Tests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public CenitReturnOut2026Tests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("PPD", "22", "21", "220")]
    [InlineData("CCD", "27", "26", "225")]
    public async Task Builder_ShouldGenerateNormativeSyntheticCenitReturnOut(
        string sec,
        string originalCode,
        string returnCode,
        string serviceClass)
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var builder = CreateBuilder(context);
        var entry = BuildEntry(returnCode);

        var result = await builder.BuildReturnOutAsync(new NachaReturnOutBuildRequest(
            new DateTime(2026, 8, 15, 14, 30, 0, DateTimeKind.Utc),
            "A",
            "0123456789",
            "0987654321",
            "CENIT",
            "CFA",
            "RETURN",
            [BuildBatch(sec, serviceClass, entry)],
            ClearingHouseCode: "CENIT",
            ClearingHouseName: "CENIT",
            NormativeVersion: CenitReturnOut2026Layout.NormativeVersion));

        var records = Split(result.Content);
        result.ProfileCode.Should().Be(CenitReturnOut2026Layout.ProfileCode);
        result.NormativeVersion.Should().Be(CenitReturnOut2026Layout.NormativeVersion);
        records.Should().HaveCount(10);
        records.Should().OnlyContain(record => record.Length == CenitReturnOut2026Layout.RecordLength);
        records.Take(6).Select(record => record[0]).Should().Equal('1', '5', '6', '7', '8', '9');
        records.Skip(6).Should().OnlyContain(record => record.All(character => character == '9'));
        records[1].Substring(50, 3).Should().Be(sec);
        records[2].Substring(1, 2).Should().Be(returnCode);
        records[2].Substring(12, 17).Should().Be("00123456789012345");
        records[3].Substring(1, 2).Should().Be("99");
        records[3].Substring(3, 3).Should().Be("R02");
        records[3].Substring(6, 15).Should().Be("123456780000099");
        records[3].Substring(29, 8).Should().Be("87654321");
        records[3].Substring(81, 15).Should().Be("876543210000001");
        records[4].Substring(4, 6).Should().Be("000002");
        records[5].Substring(1, 6).Should().Be("000001");
        records[5].Substring(7, 6).Should().Be("000001");
        records[5].Substring(13, 8).Should().Be("00000002");
        originalCode.Should().NotBe(returnCode);
    }

    [Fact]
    public async Task ApplicationPipeline_ShouldGeneratePersistAuditAndRemainIdempotent()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        await SeedApplicationScenarioAsync(context, "PPD", "22", TransactionTypeEnum.Credit);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(service => service.EvaluateOutgoingReturnAsync(
                It.Is<AchReturnEligibilityRequest>(request => request.TransactionId == 91001 && request.ReturnReasonCode == "R02"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, "R02", 7001, "Credit", "Pending", []));
        var naming = new Mock<IExternalFileNamePolicy>();
        naming.Setup(policy => policy.GenerateExternalNameAsync(
                It.Is<ExternalFileNameContext>(value => value.ClearingHouseCode == "CENIT"
                    && value.CycleId == "CENIT-C2"
                    && value.ExternalFileType == ExternalFileType.ReturnOut),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalFileNamePolicyResult
            {
                ExternalFileName = "7654321.001.1",
                Components = new ExternalFileNameComponents
                {
                    FullName = "7654321.001.1",
                    FileIdModifier = 'A',
                    ExternalSequence = 1
                },
                Validation = new ExternalFileNameValidationResult
                {
                    Disposition = ExternalFileValidationDisposition.Passed
                }
            });
        var service = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: naming.Object,
            nachaFileBuilder: CreateBuilder(context),
            cenitReturnPolicy: new CenitIncomingReturnPolicy());

        var request = new GenerateReturnsFileRequest(
            "CENIT-C1",
            [new ReturnSelectionItemDto(91001, "R02")],
            "CENIT-C2");
        var response = await service.GenerateReturnsFileAsync(request);

        response.FileName.Should().Be("7654321.001.1");
        response.TotalReturns.Should().Be(1);
        var records = Split(Encoding.UTF8.GetString(response.Content));
        records[1].Substring(50, 3).Should().Be("PPD");
        records[2].Substring(1, 2).Should().Be("21");
        records[2].Substring(3, 8).Should().Be("12345678");
        records[2].Substring(12, 17).Should().Be("00123456789012345");
        records[3].Substring(3, 3).Should().Be("R02");
        records[3].Substring(6, 15).Should().Be("123456780000099");
        records[3].Substring(29, 8).Should().Be("87654321");
        (await context.AchReturnsGenerated.SingleAsync()).ReturnCycleId.Should().Be("CENIT-C2");
        (await context.AchTransactions.SingleAsync(transaction => transaction.Id == 91001)).State
            .Should().Be(AchTransferStateEnum.ReturnedByEpr);
        (await context.AchTransactionStateEvents.CountAsync(value => value.AchTransactionId == 91001)).Should().Be(1);

        var identifierMap = new Mock<INachaFileIdentifierMapService>();
        identifierMap.Setup(value => value.ResolveIdentifierAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync('A');
        var rebuilt = await new AchOutboundReturnArtifactService(context, CreateBuilder(context), identifierMap.Object)
            .BuildAsync(response.FileName);
        rebuilt.Content.Should().Equal(response.Content);
        rebuilt.TransactionIds.Should().Equal(91001);

        await FluentActions.Invoking(() => service.GenerateReturnsFileAsync(request))
            .Should().ThrowAsync<AchReturnAlreadyGeneratedException>();
        (await context.AchReturnsGenerated.CountAsync()).Should().Be(1);
        (await context.AchTransactionStateEvents.CountAsync(value => value.AchTransactionId == 91001)).Should().Be(1);
    }

    [Theory]
    [InlineData("CTX", "R02", "CTX_OUT_OF_CURRENT_PLATFORM_SCOPE")]
    [InlineData("PPD", "R60", "CENIT_ROR_NOT_ORDINARY_RETURN")]
    public async Task ApplicationPipeline_ShouldRejectOutOfScopeContracts(string sec, string cause, string expectedCode)
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        await SeedApplicationScenarioAsync(context, sec, "22", TransactionTypeEnum.Credit);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(service => service.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, cause, 7001, "Credit", "Pending", []));
        var service = new AchReturnsService(
            context,
            regulatoryCatalogService: Mock.Of<IAchRegulatoryCatalogService>(),
            returnEligibilityService: eligibility.Object,
            returnGenerationLockService: new TestReturnGenerationLockService(),
            externalFileNamePolicy: Mock.Of<IExternalFileNamePolicy>(),
            nachaFileBuilder: CreateBuilder(context),
            cenitReturnPolicy: new CenitIncomingReturnPolicy());

        await FluentActions.Invoking(() => service.GenerateReturnsFileAsync(new GenerateReturnsFileRequest(
                "CENIT-C1",
                [new ReturnSelectionItemDto(91001, cause)],
                "CENIT-C2")))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{expectedCode}*");
        (await context.AchReturnsGenerated.CountAsync()).Should().Be(0);
    }

    private static NachaFileBuilder CreateBuilder(AchDbContext context)
        => new(
            context,
            Mock.Of<IBankHoliday>(),
            Mock.Of<INachaDataLoader>(),
            Mock.Of<INachaTransactionValidationService>(),
            Mock.Of<INachaFixedWidthRecordRenderer>(),
            Mock.Of<INachaRecordDataProvider>(),
            Mock.Of<INachaSemanticValidator>(),
            configResolver: new NachaConfigResolver(context));

    private static NachaReturnOutBatch BuildBatch(string sec, string serviceClass, NachaReturnOutEntry entry)
        => new(
            serviceClass,
            "ORIGINADOR CENIT",
            string.Empty,
            "9001234567",
            sec,
            "RETURN",
            new DateTime(2026, 8, 15),
            new DateTime(2026, 8, 15),
            string.Empty,
            "87654321",
            1,
            [entry]);

    private static NachaReturnOutEntry BuildEntry(string transactionCode)
        => new(
            1,
            transactionCode,
            "12345678",
            "0",
            "00123456789012345",
            100m,
            "900123456",
            "RECEPTOR CENIT",
            string.Empty,
            "876543210000001",
            "R02",
            "123456780000099",
            string.Empty,
            "87654321",
            "REFERENCIA ORIGINAL PRESERVADA",
            "876543210000001");

    private static IReadOnlyList<string> Split(string content)
        => Enumerable.Range(0, content.Length / CenitReturnOut2026Layout.RecordLength)
            .Select(index => content.Substring(index * CenitReturnOut2026Layout.RecordLength, CenitReturnOut2026Layout.RecordLength))
            .ToList();

    private static async Task SeedApplicationScenarioAsync(
        AchDbContext context,
        string sec,
        string transactionCode,
        TransactionTypeEnum type)
    {
        var date = new DateTime(2026, 8, 15);
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 7001,
            Code = "CENIT",
            Name = "CENIT",
            OriginCode = "7654321"
        });
        for (var number = 1; number <= 4; number++)
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = $"CENIT-C{number}",
                CycleName = $"Ciclo {number}",
                ProcessingDate = date,
                StartTime = TimeSpan.FromHours(number),
                EndTime = TimeSpan.FromHours(number + 1),
                CutoffTime = TimeSpan.FromHours(number + 1),
                ClearingHouseId = 7001
            });
        }

        context.AchTransactions.Add(new AchTransaction
        {
            Id = 91001,
            AchBatchId = 81001,
            AchCycleId = "CENIT-C1",
            Type = type,
            State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = date,
            TransactionCode = transactionCode,
            TraceNumber = "123456780000099",
            ReceivingDFI = "87654321",
            OriginatingDFI = "12345678",
            Amount = 100m,
            Reference = "NORMATIVE-SYNTHETIC-CENIT-RETURN-OUT",
            TransactionExternalId = "CENIT-ORIGINAL-91001",
            SourceAccountNumber = "ORIGIN-ACCOUNT",
            DestinationAccountNumber = "00123456789012345",
            RecipientIdNumber = "900123456",
            CompanyName = "RECEPTOR CENIT",
            CompanyIdentification = "9001234567",
            DiscretionaryData = string.Empty
        });

        var ingestionId = Guid.NewGuid();
        var headerId = $"CENIT-RAW-{Guid.NewGuid():N}";
        var ingestion = new IncomingNachaFileIngestion
        {
            Id = ingestionId,
            FileName = "1234567.001.20260815.1",
            FileHashSha256 = new string('A', 64),
            UploadedBy = "test",
            CorrelationId = "cenit-return-out-e2e"
        };
        var header = new NachaHeader
        {
            NachaID = headerId,
            ImmediateDestination = "0987654321",
            ImmediateOrigin = "0123456789",
            ImmediateDestinationName = "CFA",
            ImmediateOriginName = "CENIT",
            IncomingNachaFileIngestionId = ingestionId,
            ClearingHouseId = 7001,
            AchCycleId = "CENIT-C1"
        };
        var batch = new BatchHeader
        {
            BatchID = 71001,
            NachaID = headerId,
            BatchNumber = 1,
            StandardEntryClassCode = sec,
            ServiceClassCode = "220"
        };
        var entry = new EntryDetail
        {
            EntryDetailID = 61001,
            NachaID = headerId,
            BatchHeaderId = batch.BatchID,
            BatchNumber = 1,
            TransactionCode = transactionCode,
            SequenceNumber = "123456780000099",
            ReceivingParticipantEntityCode = "87654321",
            AccountNumber = "00123456789012345",
            Amount = 100m,
            NachaHeader = header,
            BatchHeader = batch
        };
        context.IncomingNachaFileIngestions.Add(ingestion);
        context.NachaHeaders.Add(header);
        context.BatchHeaders.Add(batch);
        context.EntryDetails.Add(entry);
        context.IncomingNachaTransactionLinks.Add(new IncomingNachaTransactionLink
        {
            IncomingNachaFileIngestionId = ingestionId,
            EntryDetailId = entry.EntryDetailID,
            AchTransactionId = 91001,
            LinkType = IncomingNachaLinkType.ExactTrace15,
            ConfidenceScore = 1m,
            LinkedBy = "test",
            IsFinal = true,
            EntryDetail = entry
        });
        await context.SaveChangesAsync();
    }
}
