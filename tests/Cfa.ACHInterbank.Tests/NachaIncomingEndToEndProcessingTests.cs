using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.NachaFunctional;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaIncomingEndToEndProcessingTests
{
    [Fact]
    public async Task ProcessAchColombiaIncomingGoldenFile_ShouldParsePersistCorrelateAndDecide()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var result = await fixture.Sut.ProcessAsync(BuildRequest(
            NachaTestDataPaths.AchColombiaIncoming001,
            "ACH",
            "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0"));

        result.ValidationPassed.Should().BeTrue(string.Join(" | ", result.Errors));
        result.PersistencePassed.Should().BeTrue();
        result.FlowType.Should().Be(NachaIncomingFlowType.IncomingCreditFromExternalOriginator);
        result.EntryCount.Should().Be(1);
        result.AddendaCount.Should().Be(1);
        result.BatchCount.Should().Be(1);
        result.FileControlCount.Should().Be(1);
        result.Decisions.Should().ContainSingle(x =>
            x.DecisionType == NachaIncomingDecisionType.ApplyCreditMovement
            && x.SoapOperation == NachaSoapOperationCandidate.ProcTransacciones
            && x.RequiresMonetaryMovement);
        fixture.Context.NachaHeaders.Should().ContainSingle();
        fixture.Context.EntryDetails.Should().ContainSingle();
        fixture.Context.FileControls.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessCenitIncomingGoldenFile_ShouldParsePersistCorrelateAndDecide()
    {
        await using var fixture = BuildFixture("CENIT", 2, "87654321");
        var result = await fixture.Sut.ProcessAsync(BuildRequest(
            NachaTestDataPaths.CenitIncoming001,
            "CENIT",
            "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0"));

        result.ValidationPassed.Should().BeTrue(string.Join(" | ", result.Errors));
        result.PersistencePassed.Should().BeTrue();
        result.ClearingHouseCode.Should().Be("CENIT");
        result.ProfileCode.Should().Be("OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0");
        result.Decisions.Should().ContainSingle(x => x.SoapOperation == NachaSoapOperationCandidate.ProcTransacciones);
    }

    [Fact]
    public async Task ProcessAchColombiaReturnGoldenFile_ShouldParsePersistAndRegisterDifferentialResponse()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var result = await fixture.Sut.ProcessAsync(BuildRequest(
            NachaTestDataPaths.AchColombiaReturn001,
            "ACH",
            "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0"));

        result.ValidationPassed.Should().BeTrue(string.Join(" | ", result.Errors));
        result.IsReturnFile.Should().BeTrue();
        result.FlowType.Should().Be(NachaIncomingFlowType.ReturnFile);
        result.Decisions.Should().ContainSingle(x =>
            x.DecisionType == NachaIncomingDecisionType.RegisterDifferentialResponse
            && x.SoapOperation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion
            && !x.RequiresMonetaryMovement
            && x.ReasonCode == "R01");
    }

    [Fact]
    public async Task ProcessCenitReturnGoldenFile_ShouldParsePersistAndRegisterDifferentialResponse()
    {
        await using var fixture = BuildFixture("CENIT", 2, "87654321");
        var result = await fixture.Sut.ProcessAsync(BuildRequest(
            NachaTestDataPaths.CenitReturn001,
            "CENIT",
            "OFFICIAL_CENIT_ENTRADA_DEVOLUCION_V1_0"));

        result.ValidationPassed.Should().BeTrue(string.Join(" | ", result.Errors));
        result.IsReturnFile.Should().BeTrue();
        result.ClearingHouseCode.Should().Be("CENIT");
        result.Decisions.Should().ContainSingle(x =>
            x.SoapOperation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion
            && !x.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task ProcessIncomingFile_ShouldRejectInvalidFileName()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var request = BuildPathRequest(
            NachaTestDataPaths.AchColombiaIncoming001,
            "ACH",
            "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            fileNameOverride: "nombre-invalido.txt",
            isSimulation: false);

        var result = await fixture.Sut.ProcessAsync(request);

        result.ValidationPassed.Should().BeFalse();
        result.PersistencePassed.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Contains("RRRRTTT.ZZZ.N", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessIncomingFile_ShouldRejectInvalidRecordLength()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var content = NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001) + "X";

        var result = await fixture.Sut.ProcessAsync(BuildContentRequest("ACH_COL_IN_001.ach", content, "ACH"));

        result.ValidationPassed.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Contains("no es multiplo de 106", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessIncomingFile_ShouldRejectInvalidControlTotals()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var content = MutateSegment(NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001), 5, 21, 10, "9999999999");

        var result = await fixture.Sut.ProcessAsync(BuildContentRequest("ACH_COL_IN_001.ach", content, "ACH"));

        result.ValidationPassed.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Contains("EntryHash invalido", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessIncomingFile_ShouldRejectIntermediatePadding()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var records = Split(NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001));
        records.Insert(2, new string('9', 106));
        records.RemoveAt(records.Count - 1);

        var result = await fixture.Sut.ProcessAsync(BuildContentRequest("ACH_COL_IN_001.ach", string.Concat(records), "ACH"));

        result.ValidationPassed.Should().BeFalse();
        result.Errors.Should().Contain(x => x.Contains("Padding intermedio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessIncomingFile_ShouldDetectDuplicateFile()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var request = BuildRequest(NachaTestDataPaths.AchColombiaIncoming001, "ACH", "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0");

        var first = await fixture.Sut.ProcessAsync(request);
        var second = await fixture.Sut.ProcessAsync(BuildPathRequest(
            NachaTestDataPaths.AchColombiaIncoming001,
            "ACH",
            "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            correlationId: "phase-6b4-duplicate"));

        first.ValidationPassed.Should().BeTrue();
        second.IsDuplicate.Should().BeTrue();
        second.Decisions.Should().ContainSingle(x => x.DecisionType == NachaIncomingDecisionType.IgnoreDuplicate);
        fixture.Context.IncomingNachaFileIngestions.Should().ContainSingle();
    }

    [Fact]
    public async Task DifferentialResponse_ShouldPrepareRegistrarRespuestaTransaccion_AndNotRequireMonetaryMovement()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var result = await fixture.Sut.ProcessAsync(BuildRequest(NachaTestDataPaths.AchColombiaReturn001, "ACH", "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0"));

        result.Decisions.Should().OnlyContain(x =>
            x.SoapOperation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion
            && x.RequiresMonetaryMovement == false);
    }

    [Fact]
    public async Task PrenotificationApproved_ShouldMarkPrenotificationApprovedWithoutMonetaryMovement()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var result = await fixture.Sut.ProcessAsync(BuildContentRequest("1234567.001.1.ach", BuildPrenotificationContent(), "ACH"));

        result.ValidationPassed.Should().BeTrue(string.Join(" | ", result.Errors));
        result.FlowType.Should().Be(NachaIncomingFlowType.PrenotificationResponse);
        result.Decisions.Should().ContainSingle(x =>
            x.DecisionType == NachaIncomingDecisionType.ApprovePrenotification
            && x.SoapOperation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion
            && !x.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task IncomingExternalCredit_ShouldPrepareProcTransacciones()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var result = await fixture.Sut.ProcessAsync(BuildRequest(NachaTestDataPaths.AchColombiaIncoming001, "ACH", "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0"));

        result.Decisions.Should().ContainSingle(x =>
            x.DecisionType == NachaIncomingDecisionType.ApplyCreditMovement
            && x.SoapOperation == NachaSoapOperationCandidate.ProcTransacciones
            && x.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task IncomingCfaOriginatedDebitResponse_ShouldPrepareProcContrapartidasWhenApplicable()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var content = BuildDebitResponseContent();

        var result = await fixture.Sut.ProcessAsync(BuildContentRequest("1234567.001.1.ach", content, "ACH"));

        result.ValidationPassed.Should().BeTrue(string.Join(" | ", result.Errors));
        result.Decisions.Should().ContainSingle(x =>
            x.DecisionType == NachaIncomingDecisionType.ApplyDebitMovement
            && x.SoapOperation == NachaSoapOperationCandidate.ProcContrapartidas
            && x.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task UnmatchedEntry_ShouldRequireManualReview()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var content = BuildDebitResponseContent(traceNumber: "765432100009999", originEntityCode: "76543210");

        var result = await fixture.Sut.ProcessAsync(BuildContentRequest("1234567.001.1.ach", content, "ACH"));

        result.Decisions.Should().ContainSingle(x =>
            x.DecisionType == NachaIncomingDecisionType.ManualReviewRequired
            && x.SoapOperation == NachaSoapOperationCandidate.None
            && !x.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task ProcessIncomingFile_ShouldWritePhase6B4Trace()
    {
        await using var fixture = BuildFixture("ACH", 1, "12345678");
        var result = await fixture.Sut.ProcessAsync(BuildRequest(NachaTestDataPaths.AchColombiaIncoming001, "ACH", "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0"));

        result.Trace["Phase"].Should().Be("6B.4");
        result.Trace["ProductiveExecution"].Should().Be("false");
        result.Trace["NoGoReason"].Should().Contain("NO-GO");
        result.Trace["SoapOperationCandidate"].Should().Contain(nameof(NachaSoapOperationCandidate.ProcTransacciones));
    }

    private static ProcessorFixture BuildFixture(string clearingHouseCode, int clearingHouseId, string originCode)
    {
        var context = BuildContext();
        SeedCatalog(context, clearingHouseCode, clearingHouseId, originCode);

        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                IsAmbiguous = false,
                ClearingHouseId = clearingHouseId,
                DetectedClearingHouseId = clearingHouseId,
                AchCycleId = $"CYCLE-{clearingHouseCode}-20260524-1",
                OperationalDate = new DateTime(2026, 5, 24),
                Confidence = 1m,
                Status = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
                ResolutionMode = "TestGoldenFile",
                EvidenceJson = "{}"
            });

        var state = new Mock<IAchStateTransitionService>();
        state.Setup(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction());

        var parser = new NachaParserService(context, Mock.Of<ILogger<NachaParserService>>(), state.Object);
        var postParse = Mock.Of<IIncomingNachaPostParseProcessor>();
        var ingestion = new IncomingNachaIngestionAppService(
            context,
            resolver.Object,
            parser,
            postParse,
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var sut = new NachaIncomingFileProcessor(
            ingestion,
            new IncomingNachaFunctionalClassifier(),
            context,
            Mock.Of<ILogger<NachaIncomingFileProcessor>>());

        return new ProcessorFixture(context, sut);
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedCatalog(AchDbContext context, string clearingHouseCode, int clearingHouseId, string originCode)
    {
        var clearingHouse = new ClearingHouse
        {
            Id = clearingHouseId,
            Code = clearingHouseCode,
            Name = clearingHouseCode == "CENIT" ? "CENIT" : "ACH Colombia",
            OriginCode = originCode
        };
        context.ClearingHouses.Add(clearingHouse);

        var cycleId = $"CYCLE-{clearingHouseCode}-20260524-1";
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            CycleName = "Ciclo 1 UAT",
            ClearingHouseId = clearingHouseId,
            ProcessingDate = new DateTime(2026, 5, 24),
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0)
        });

        var cfa = new FinancialInstitution { Id = 1, Name = "CFA Cooperativa Financiera UAT", RoutingNumber = "1234567", TransitCode = "8", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
        cfa.CalculateCheckDigit();
        var external = new FinancialInstitution { Id = 2, Name = "Banco Externo UAT", RoutingNumber = "7654321", TransitCode = "0", IsDefaultSource = false, Status = FinancialInstitutionStatus.Active };
        external.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(cfa, external);

        var customer = new Customer
        {
            FirstName = "Cliente",
            LastName = "UAT",
            DocumentType = "CC",
            DocumentNumber = "900000001",
            PersonType = "NAT",
            Gender = "N"
        };
        context.Customers.Add(customer);
        context.CustomerAccounts.Add(new CustomerAccount { Customer = customer, AccountNumber = "999988887777" });
        var paddedCustomer = new Customer
        {
            FirstName = "Cliente",
            LastName = "UAT Padding",
            DocumentType = "CC",
            DocumentNumber = "      900000001",
            PersonType = "NAT",
            Gender = "N"
        };
        context.Customers.Add(paddedCustomer);
        context.CustomerAccounts.Add(new CustomerAccount { Customer = paddedCustomer, AccountNumber = "999988887777" });

        context.AchBatches.Add(new AchBatch
        {
            Id = 900,
            AchCycleId = cycleId,
            CompanyEntryDescriptionId = 1,
            EffectiveEntryDate = new DateTime(2026, 5, 24),
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "1234567800",
            OriginOrOdfi = "12345678"
        });

        context.AchFileRejectionCodes.AddRange(
            new AchFileRejectionCode { Code = "D01", Description = "Duplicado", IsActive = true },
            new AchFileRejectionCode { Code = "D02", Description = "Padding invalido", IsActive = true },
            new AchFileRejectionCode { Code = "D04", Description = "Conteo invalido", IsActive = true },
            new AchFileRejectionCode { Code = "D05", Description = "Hash invalido", IsActive = true });

        context.SaveChanges();
    }

    private static Mock<IExternalFileNamePolicy> BuildExternalPolicyMock()
    {
        var mock = new Mock<IExternalFileNamePolicy>();
        mock.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext context, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = context.ProvidedExternalFileName ?? context.InternalFileName ?? "incoming.ach",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed },
                CorrelationEvidence = new ExternalFileNameCorrelationEvidence(),
                Components = new ExternalFileNameComponents { FullName = context.ProvidedExternalFileName ?? "incoming.ach" }
            });
        return mock;
    }

    private static NachaIncomingFileRequest BuildRequest(string path, string clearingHouseCode, string profileCode)
        => BuildPathRequest(path, clearingHouseCode, profileCode);

    private static NachaIncomingFileRequest BuildPathRequest(
        string path,
        string clearingHouseCode,
        string profileCode,
        string? fileNameOverride = null,
        string? correlationId = null,
        bool isSimulation = true)
        => new()
        {
            FileName = fileNameOverride ?? Path.GetFileName(path),
            Content = NachaTestDataPaths.ReadRequiredText(path),
            ClearingHouseCode = clearingHouseCode,
            ExpectedProfileCode = profileCode,
            ReceivedAt = new DateTime(2026, 5, 24, 12, 0, 0),
            Source = "GoldenFile",
            CorrelationId = correlationId ?? $"phase-6b4-{clearingHouseCode.ToLowerInvariant()}-{Path.GetFileNameWithoutExtension(path).ToLowerInvariant()}",
            IsSimulation = isSimulation,
            UploadedBy = "uat-functional"
        };

    private static NachaIncomingFileRequest BuildContentRequest(string fileName, string content, string clearingHouseCode)
        => new()
        {
            FileName = fileName,
            Content = content,
            ClearingHouseCode = clearingHouseCode,
            ExpectedProfileCode = clearingHouseCode == "CENIT" ? "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_0" : "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            ReceivedAt = new DateTime(2026, 5, 24, 12, 0, 0),
            Source = "GoldenMutation",
            CorrelationId = $"phase-6b4-{Guid.NewGuid():N}",
            IsSimulation = true,
            UploadedBy = "uat-functional"
        };

    private static string BuildDebitResponseContent(string traceNumber = "123456780000001", string originEntityCode = "12345678")
    {
        var records = Split(NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001));
        records[1] = MutateSegment(records[1], 0, 1, 3, "225");
        records[2] = MutateSegment(records[2], 0, 1, 2, "27");
        records[2] = MutateSegment(records[2], 0, 87, 15, traceNumber);
        records[3] = MutateSegment(records[3], 0, 87, 7, traceNumber[^7..]);
        records[1] = MutateSegment(records[1], 0, 83, 8, originEntityCode);
        records[4] = MutateSegment(records[4], 0, 1, 3, "225");
        records[4] = MutateSegment(records[4], 0, 20, 18, "000000000000150000");
        records[4] = MutateSegment(records[4], 0, 38, 18, "000000000000000000");
        records[4] = MutateSegment(records[4], 0, 91, 8, originEntityCode);
        records[5] = MutateSegment(records[5], 0, 31, 18, "000000000000150000");
        records[5] = MutateSegment(records[5], 0, 49, 18, "000000000000000000");
        return string.Concat(records);
    }

    private static string BuildPrenotificationContent()
    {
        var records = Split(NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001));
        records[2] = MutateSegment(records[2], 0, 1, 2, "23");
        records[2] = MutateSegment(records[2], 0, 29, 18, "000000000000000000");
        records[4] = MutateSegment(records[4], 0, 38, 18, "000000000000000000");
        records[5] = MutateSegment(records[5], 0, 49, 18, "000000000000000000");
        return string.Concat(records);
    }

    private static List<string> Split(string content)
        => Enumerable.Range(0, content.Length / 106)
            .Select(i => content.Substring(i * 106, 106))
            .ToList();

    private static string MutateSegment(string content, int recordIndex, int start, int length, string replacement)
    {
        var absoluteStart = recordIndex * 106 + start;
        var fixedReplacement = replacement.PadRight(length);
        return string.Concat(content.AsSpan(0, absoluteStart), fixedReplacement.AsSpan(0, length), content.AsSpan(absoluteStart + length));
    }

    private sealed record ProcessorFixture(AchDbContext Context, INachaIncomingFileProcessor Sut) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await Context.DisposeAsync();
    }
}
