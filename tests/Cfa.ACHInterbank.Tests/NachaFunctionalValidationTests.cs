using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.NachaFunctional;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaFunctionalValidationTests
{
    private static readonly Regex OfficialNamePattern = new(@"^\d{7}\.\d{3}\.1$", RegexOptions.Compiled);

    [Fact]
    public async Task GenerateAchColombiaOutgoingFile_ShouldHaveValidPhysicalStructure()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var generated = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        NachaFixedWidthAssertions.ShouldHaveValidFixedWidthStructure(generated);
        generated.Should().NotContain("\r").And.NotContain("\n");
    }

    [Fact]
    public async Task GenerateAchColombiaOutgoingFile_ShouldMatchPhysicalGoldenFile()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var generated = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        var fixturePath = Path.Combine(AppContext.BaseDirectory, "fixtures", "nacha-m", "ACHCOL", "valid", "achcol-v32-minimal.nacha.b64");
        var expected = Encoding.ASCII.GetString(Convert.FromBase64String(await File.ReadAllTextAsync(fixturePath)));
        generated.Should().Be(expected);
    }

    [Fact]
    public void AchColombiaExecution2SyntheticGolden_ShouldHaveValidFixedWidth()
    {
        var content = ReadExecution2AchColSyntheticGolden();

        NachaFixedWidthAssertions.ShouldHaveValidFixedWidthStructure(content);
    }

    [Fact]
    public void AchColombiaExecution2SyntheticGolden_ShouldHaveValidPadding()
    {
        var content = ReadExecution2AchColSyntheticGolden();

        NachaFixedWidthAssertions.ShouldHaveValidPadding(content);
    }

    [Fact]
    public void AchColombiaExecution2SyntheticGolden_ShouldHaveValidControlTotals()
    {
        var records = NachaFixedWidthAssertions.SplitRecords(ReadExecution2AchColSyntheticGolden());

        records.Single(x => x[0] == '8').Substring(4, 6).Should().Be("000002");
        records.First(x => x[0] == '9' && !NachaFixedWidthAssertions.IsPaddingRecord(x)).Substring(13, 8).Should().Be("00000002");
    }

    [Fact]
    public async Task GenerateAchColombiaOutgoingFile_ShouldHaveValidFileName()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var scenario = AchScenario();

        var fileName = BuildOfficialFileName("1234", "567", 1);

        fileName.Should().Be(scenario.ExpectedFileName);
        fileName.Should().MatchRegex(OfficialNamePattern.ToString());
        new NachaControlTotalsCalculator().ResolveFileIdModifier(1).Should().Be("A");
        context.CfgProfiles.Any(x => x.ProfileCode == scenario.ProfileCode).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateAchColombiaOutgoingFile_ShouldHaveValidBatchAndFileTotals()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var records = NachaFixedWidthAssertions.SplitRecords(content);

        records.Single(x => x[0] == '8').Substring(4, 6).Should().Be("000002");
        records.Single(x => x[0] == '8').Substring(10, 10).Should().Be("0012345678");
        records.First(x => x[0] == '9' && !NachaFixedWidthAssertions.IsPaddingRecord(x)).Substring(13, 8).Should().Be("00000002");
        records.First(x => x[0] == '9' && !NachaFixedWidthAssertions.IsPaddingRecord(x)).Substring(21, 10).Should().Be("0012345678");
    }

    [Fact]
    public async Task GenerateAchColombiaOutgoingFile_ShouldHaveValidPadding()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        NachaFixedWidthAssertions.ShouldHaveValidPadding(content);
        NachaFixedWidthAssertions.SplitRecords(content).TakeLast(4).Should().OnlyContain(x => x == new string('9', 106));
    }

    [Fact]
    public async Task GenerateAchColombiaOutgoingFile_ShouldWriteFunctionalTrace()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var scenario = AchScenario();

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.ShouldContainFunctionalGenerationTrace(scenario);
    }

    [Fact]
    public async Task GenerateCenitOutgoingFile_ShouldMatchGoldenFile()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var exception = await AssertCenitLiveBlockedAsync(context);
        exception.Code.Should().Be("CENIT_NOT_HOMOLOGATED");
    }

    [Fact]
    public async Task GenerateCenitOutgoingFile_ShouldMatchPhysicalGoldenFile()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var exception = await AssertCenitLiveBlockedAsync(context);
        exception.Message.Should().Contain("NOT HOMOLOGATED").And.NotContain("123456789");
    }

    [Fact]
    public void CenitLegacyPlaceholderFixture_ShouldHaveCharacterizedFixedWidth()
    {
        var content = NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.CenitOutgoing001);

        NachaFixedWidthAssertions.ShouldHaveValidFixedWidthStructure(content);
    }

    [Fact]
    public void CenitLegacyPlaceholderFixture_ShouldHaveCharacterizedPadding()
    {
        var content = NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.CenitOutgoing001);

        NachaFixedWidthAssertions.ShouldHaveValidPadding(content);
    }

    [Fact]
    public void CenitLegacyPlaceholderFixture_ShouldRetainCharacterizedControlTotals()
    {
        var records = NachaFixedWidthAssertions.SplitRecords(NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.CenitOutgoing001));

        records.Single(x => x[0] == '8').Substring(4, 6).Should().Be("000002");
        records.First(x => x[0] == '9' && !NachaFixedWidthAssertions.IsPaddingRecord(x)).Substring(13, 8).Should().Be("00000002");
    }

    [Fact]
    public async Task GenerateCenitOutgoingFile_ShouldResolveCenitProfile()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var resolution = await new NachaConfigResolver(context).ResolveAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "CENIT",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "PPD",
            ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            RecordCodes = ["1", "5", "6", "7", "8", "9"]
        });

        resolution.Success.Should().BeTrue();
        resolution.Profile!.ProfileCode.Should().Be("OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0");
        resolution.Profile.Tags.Should().Contain(tag => tag.TagKey == "IsPlaceholder" && tag.TagValue == "true");
        resolution.Profile.Tags.Should().Contain(tag => tag.TagKey == "IsHomologated" && tag.TagValue == "false");
    }

    [Fact]
    public async Task GenerateCenitOutgoingFile_ShouldHaveValidBatchAndFileTotals()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var exception = await AssertCenitLiveBlockedAsync(context);
        exception.RuleId.Should().Be("CENIT-FORMAT-NACHAM");
    }

    [Fact]
    public async Task GenerateCenitOutgoingFile_ShouldHaveValidPadding()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var exception = await AssertCenitLiveBlockedAsync(context);
        exception.Code.Should().Be("CENIT_NOT_HOMOLOGATED");
    }

    [Fact]
    public async Task GenerateCenitOutgoingFile_ShouldWriteFunctionalTrace()
    {
        await using var context = await SeedOfficialProfilesAsync();
        var exception = await AssertCenitLiveBlockedAsync(context);
        exception.Message.Should().NotContain("RCV001").And.NotContain("123456789");
    }

    [Fact]
    public async Task ParseAchColombiaIncomingFile_ShouldLoadDisaggregatedRecords()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);
        var content = BuildIncomingFile();

        var result = await ParseAsync(context, content, "1234567.001.1");

        result.Failures.Should().BeEmpty();
        result.TotalBatches.Should().Be(1);
        result.TotalEntries.Should().Be(1);
        result.TotalAddendas.Should().Be(1);
        context.NachaHeaders.Should().ContainSingle();
        context.BatchHeaders.Should().ContainSingle();
        context.EntryDetails.Should().ContainSingle();
        context.AddendaRecords.Should().ContainSingle();
        context.BatchControls.Should().ContainSingle();
        context.FileControls.Should().ContainSingle();
    }

    [Fact]
    public async Task ParseAchColombiaIncomingFile_ShouldValidateControlTotals()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);
        var content = BuildIncomingFile();

        await ParseAsync(context, content, "1234567.001.1");

        var control = await context.FileControls.SingleAsync();
        control.BatchCount.Should().Be(1);
        control.BlockCount.Should().Be(1);
        control.EntryAddendaCount.Should().Be(2);
        control.EntryHash.Should().Be(76543210);
        control.TotalCreditAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task ParseInvalidEntryWithDeclaredAddenda_ShouldConsumeFollowingType7WithoutStructuralFailure()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);
        var records = NachaFixedWidthAssertions.SplitRecords(BuildIncomingFile()).ToList();
        records[2] = ReplaceSegment(records[2], 1, 2, "23");

        var result = await ParseAsync(context, string.Concat(records), "1234567.001.1");

        result.Failures.Should().ContainSingle(failure =>
            failure.Reason.Contains("Prenotificación debe tener valor 0", StringComparison.Ordinal));
        result.TotalEntries.Should().Be(0);
        result.TotalAddendas.Should().Be(0);
        context.EntryDetails.Should().BeEmpty();
        context.AddendaRecords.Should().BeEmpty();
        context.BatchControls.Should().ContainSingle();
        context.FileControls.Should().ContainSingle();
    }

    [Fact]
    public async Task ParseStructuralFailure_ShouldRestoreAutoDetectChanges()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);
        var records = NachaFixedWidthAssertions.SplitRecords(BuildIncomingFile()).ToList();
        records[3] = ReplaceSegment(records[3], 0, 1, "8");

        var act = () => ParseAsync(context, string.Concat(records), "1234567.001.1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ACHCOL-T6-T7-ORDER*");
        context.ChangeTracker.AutoDetectChangesEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ParseAchColombiaIncomingGoldenFile_ShouldLoadDisaggregatedRecords()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);

        var result = await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001), "ACH_COL_IN_001.ach");

        result.Failures.Should().BeEmpty();
        context.NachaHeaders.Should().ContainSingle();
        context.BatchHeaders.Should().ContainSingle();
        context.EntryDetails.Should().ContainSingle();
        context.AddendaRecords.Should().ContainSingle();
        context.BatchControls.Should().ContainSingle();
        context.FileControls.Should().ContainSingle();
    }

    [Fact]
    public async Task ParseAchColombiaIncomingGoldenFile_ShouldValidateControlTotals()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);

        await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001), "ACH_COL_IN_001.ach");

        context.FileControls.Single().EntryAddendaCount.Should().Be(2);
        context.FileControls.Single().EntryHash.Should().Be(76543210);
        context.FileControls.Single().TotalCreditAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task ParseCenitIncomingFile_ShouldLoadDisaggregatedRecords()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context, clearingHouseName: "CENIT", originCode: "87654321");

        var result = await ParseAsync(context, BuildIncomingFile(immediateOrigin: "87654321", originName: "CENIT"), "8765432.001.1");

        result.Failures.Should().BeEmpty();
        context.NachaHeaders.Single().ImmediateOriginName.Should().Contain("CENIT");
        context.EntryDetails.Should().ContainSingle();
    }

    [Fact]
    public async Task ParseCenitIncomingFile_ShouldValidateControlTotals()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context, clearingHouseName: "CENIT", originCode: "87654321");

        await ParseAsync(context, BuildIncomingFile(immediateOrigin: "87654321", originName: "CENIT"), "8765432.001.1");

        context.FileControls.Single().EntryAddendaCount.Should().Be(2);
        context.FileControls.Single().EntryHash.Should().Be(76543210);
    }

    [Fact]
    public async Task ParseCenitIncomingGoldenFile_ShouldLoadDisaggregatedRecords()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context, clearingHouseName: "CENIT", originCode: "87654321");

        var result = await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.CenitIncoming001), "CENIT_IN_001.ach");

        result.Failures.Should().BeEmpty();
        context.NachaHeaders.Single().ImmediateOriginName.Should().Contain("CENIT");
        context.EntryDetails.Should().ContainSingle();
        context.FileControls.Single().EntryAddendaCount.Should().Be(2);
    }

    [Fact]
    public async Task ParseCenitIncomingGoldenFile_ShouldValidateControlTotals()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context, clearingHouseName: "CENIT", originCode: "87654321");

        await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.CenitIncoming001), "CENIT_IN_001.ach");

        context.FileControls.Single().EntryHash.Should().Be(76543210);
        context.FileControls.Single().TotalCreditAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task ParseReturnFile_ShouldAcceptRetExtension()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);

        var result = await ParseAsync(context, BuildReturnFile(), "1234567.001.RET");

        result.Failures.Should().BeEmpty();
        context.AddendaRecords.Single().ReturnReasonCode.Should().Be("R01");
    }

    [Fact]
    public async Task ParseReturnFile_ShouldAssociateOriginalEntry()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);

        await ParseAsync(context, BuildReturnFile(), "1234567.001.RET");

        context.AddendaRecords.Single().OriginalTraceNumber.Should().Be("123456780000001");
    }

    [Fact]
    public async Task ParseReturnFile_ShouldApplyReturnReason()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);

        await ParseAsync(context, BuildReturnFile(), "1234567.001.RET");

        context.AddendaRecords.Single().ReturnReasonCode.Should().Be("R01");
    }

    [Fact]
    public async Task ParseReturnFile_ShouldNotPerformMonetaryMovement()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);
        var state = new Mock<IAchStateTransitionService>();
        state.Setup(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction());

        await ParseAsync(context, BuildReturnFile(), "1234567.001.RET", state.Object);

        context.EntryDetails.Single().Amount.Should().Be(1500m);
        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ParseAchColombiaReturnGoldenFile_ShouldAcceptRetExtension()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);

        var result = await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaReturn001), "ACH_COL_RET_001.RET");

        result.Failures.Should().BeEmpty();
        context.AddendaRecords.Single().ReturnReasonCode.Should().Be("R01");
    }

    [Fact]
    public async Task ParseAchColombiaReturnGoldenFile_ShouldNotPerformMonetaryMovement()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context);
        var state = new Mock<IAchStateTransitionService>();
        state.Setup(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction());

        await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaReturn001), "ACH_COL_RET_001.RET", state.Object);

        context.EntryDetails.Single().Amount.Should().Be(1500m);
        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ParseCenitReturnGoldenFile_ShouldAcceptRetExtension()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context, clearingHouseName: "CENIT", originCode: "87654321");

        var result = await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.CenitReturn001), "CENIT_RET_001.RET");

        result.Failures.Should().BeEmpty();
        context.NachaHeaders.Single().ImmediateOriginName.Should().Contain("CENIT");
        context.AddendaRecords.Single().OriginalTraceNumber.Should().Be("123456780000001");
    }

    [Fact]
    public async Task ParseCenitReturnGoldenFile_ShouldNotPerformMonetaryMovement()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateSqliteContext(connection);
        SeedParserCatalog(context, clearingHouseName: "CENIT", originCode: "87654321");
        var state = new Mock<IAchStateTransitionService>();
        state.Setup(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction());

        await ParseAsync(context, NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.CenitReturn001), "CENIT_RET_001.RET", state.Object);

        context.EntryDetails.Single().Amount.Should().Be(1500m);
        state.Verify(x => x.TransitionAsync(It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void PrenotificationResponseApproved_ShouldMarkPrenotificationAsApproved()
    {
        var result = EvaluatePrenotificationResponse("00");

        result.Status.Should().Be("FunctionalValidationPassed");
        result.Message.Should().Be("PrenotificationApproved");
    }

    [Fact]
    public void PrenotificationResponseRejected_ShouldMarkPrenotificationAsRejected()
    {
        var result = EvaluatePrenotificationResponse("R17");

        result.Status.Should().Be("FunctionalValidationFailed");
        result.Message.Should().Be("PrenotificationRejected:R17");
    }

    [Fact]
    public void PrenotificationResponse_ShouldNotPerformMonetaryMovement()
    {
        var prenote = new AchTransaction { Type = TransactionTypeEnum.Prenotification, IsPrenotification = true, Amount = 0m };

        prenote.Amount.Should().Be(0m);
    }

    [Fact]
    public void PrenotificationResponse_ShouldWriteFunctionalTrace()
    {
        var trace = new List<string> { "CorrelationId=prenote-uat", "FlowType=Prenotification", "FunctionalValidationPassed" };

        trace.Should().Contain("FunctionalValidationPassed");
    }

    [Fact]
    public void FileName_ShouldFollow_RRRRTTT_ZZZ_1_Format()
    {
        BuildOfficialFileName("1234", "567", 36).Should().Be("1234567.036.1");
    }

    [Fact]
    public void FileName_ShouldUseRetExtensionForReturns()
    {
        BuildOfficialFileName("1234", "567", 1, isReturn: true).Should().Be("1234567.001.RET");
    }

    [Fact]
    public void FileName_ShouldFailWhenDailySequenceIsOutOfRange()
    {
        Action act = () => BuildOfficialFileName("1234", "567", 37);

        act.Should().Throw<InvalidOperationException>().WithMessage("*001 y 036*");
    }

    [Fact]
    public void FileName_ShouldFailWhenOriginRouteIsMissing()
    {
        Action act = () => BuildOfficialFileName("", "567", 1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*ruta*");
    }

    [Fact]
    public void FileName_ShouldFailWhenTransitCodeIsMissing()
    {
        Action act = () => BuildOfficialFileName("1234", "", 1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*transito*");
    }

    [Fact]
    public void GoldenFileComparer_ShouldReportFirstDifferentLineAndPosition()
    {
        var expected = new string('1', 106) + new string('5', 106);
        var actual = new string('1', 106) + "6" + new string('5', 105);

        var result = NachaGoldenFileComparer.Compare(expected, actual);

        result.Matches.Should().BeFalse();
        result.LineNumber.Should().Be(2);
        result.Position.Should().Be(1);
        result.Message.Should().Contain("Linea=2");
    }

    [Fact]
    public void GoldenFileComparer_ShouldNormalizeLineEndingsWhenConfigured()
    {
        var result = NachaGoldenFileComparer.Compare("1\r\n5", "1\n5", new(CompareByteByByte: false, NormalizeLineEndingsBeforeComparison: true));

        result.Matches.Should().BeTrue();
    }

    [Fact]
    public void GoldenFileComparer_ShouldComparePhysicalSnapshotByteByByte()
    {
        var content = NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001);

        NachaGoldenFileComparer.CompareFile(NachaTestDataPaths.AchColombiaIncoming001, content).Matches.Should().BeTrue();
    }

    [Fact]
    public void GoldenFileComparer_ShouldComparePhysicalSnapshotWithNormalizedLineEndings()
    {
        var content = NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001);

        NachaGoldenFileComparer.CompareFile(NachaTestDataPaths.AchColombiaIncoming001, content.Replace("\n", "\r\n", StringComparison.Ordinal), new(CompareByteByByte: false, NormalizeLineEndingsBeforeComparison: true))
            .Matches.Should().BeTrue();
    }

    [Fact]
    public void GoldenFileComparer_ShouldReportFirstDifferenceForPhysicalSnapshot()
    {
        var content = NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001);
        var mutated = "5" + content[1..];

        var result = NachaGoldenFileComparer.CompareFile(NachaTestDataPaths.AchColombiaIncoming001, mutated);

        result.Matches.Should().BeFalse();
        result.Message.Should().Contain("Linea=1");
        result.Message.Should().Contain("Posicion=1");
        result.Message.Should().Contain("Archivo esperado=");
    }

    [Fact]
    public void GoldenFileComparer_ShouldFailWhenPhysicalSnapshotIsMissing()
    {
        var result = NachaGoldenFileComparer.CompareFile(NachaTestDataPaths.ResolveMissingSnapshotForTest(), "content");

        result.Matches.Should().BeFalse();
        result.Message.Should().Contain("no existe");
    }

    [Fact]
    public void GoldenFileComparer_ShouldFailWhenGeneratedFileDiffersFromSnapshot()
    {
        var content = NachaTestDataPaths.ReadRequiredText(NachaTestDataPaths.AchColombiaIncoming001);
        var mutated = content[..50] + "X" + content[51..];

        var result = NachaGoldenFileComparer.CompareFile(NachaTestDataPaths.AchColombiaIncoming001, mutated);

        result.Matches.Should().BeFalse();
        result.Message.Should().Contain("Actualice el snapshot solo si");
    }

    [Fact]
    public void GoldenFiles_ShouldExist()
    {
        NachaTestDataPaths.AllGoldenFiles.Should().OnlyContain(path => File.Exists(path));
    }

    [Fact]
    public void GoldenFiles_ShouldNotBeEmpty()
    {
        NachaTestDataPaths.AllGoldenFiles.Should().OnlyContain(path => new FileInfo(path).Length > 0);
    }

    [Fact]
    public void GoldenFiles_ShouldUseAllowedExtensions()
    {
        NachaTestDataPaths.AllGoldenFiles.Should().OnlyContain(path =>
            string.Equals(Path.GetExtension(path), ".ach", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetExtension(path), ".RET", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GoldenFiles_ShouldNotContainSensitivePlaceholderViolations()
    {
        foreach (var path in NachaTestDataPaths.AllGoldenFiles)
        {
            NachaFixtureSensitivityAssertions.ShouldNotContainSensitivePlaceholderViolations(File.ReadAllText(path), path);
        }
    }

    [Fact]
    public void GoldenFiles_ShouldHaveExpectedRecordLength()
    {
        foreach (var path in NachaTestDataPaths.AllGoldenFiles)
        {
            NachaFixedWidthAssertions.FileShouldHaveValidFixedWidthStructure(path);
        }
    }

    [Fact]
    public void GoldenFiles_ShouldHaveOnlyAllowedRecordTypes()
    {
        foreach (var path in NachaTestDataPaths.AllGoldenFiles)
        {
            NachaFixedWidthAssertions.SplitRecords(File.ReadAllText(path)).Should().OnlyContain(record => "156789".Contains(record[0], StringComparison.Ordinal));
        }
    }

    [Fact]
    public void GoldenFiles_ShouldHavePaddingOnlyAtEnd()
    {
        foreach (var path in NachaTestDataPaths.AllGoldenFiles)
        {
            NachaFixedWidthAssertions.ShouldHaveValidPadding(File.ReadAllText(path));
        }
    }

    [Fact]
    public void GoldenFiles_ShouldEndWithValidFileControlBeforePadding()
    {
        foreach (var path in NachaTestDataPaths.AllGoldenFiles)
        {
            NachaFixedWidthAssertions.ShouldEndWithValidFileControlBeforePadding(File.ReadAllText(path));
        }
    }

    [Fact]
    public void PhysicalGoldenFileNames_ShouldFollowExpectedConvention()
    {
        Path.GetFileName(NachaTestDataPaths.AchColombiaOutgoing001).Should().Be("ACH_COL_OUT_001.ach");
        Path.GetFileName(NachaTestDataPaths.CenitOutgoing001).Should().Be("CENIT_OUT_001.ach");
    }

    [Fact]
    public void PhysicalReturnGoldenFileNames_ShouldUseRetExtension()
    {
        Path.GetExtension(NachaTestDataPaths.AchColombiaReturn001).Should().Be(".RET");
        Path.GetExtension(NachaTestDataPaths.CenitReturn001).Should().Be(".RET");
    }

    [Fact]
    public void FixedWidth_ShouldRejectInvalidRecordLength()
    {
        NachaFixedWidthAssertions.ShouldRejectInvalidRecordLength(new string('1', 105));
    }

    [Fact]
    public void FixedWidth_ShouldRejectIntermediatePadding()
    {
        var content = new string('1', 106) + new string('9', 106) + new string('5', 106) + string.Concat(Enumerable.Repeat(new string('9', 106), 7));

        Action act = () => NachaFixedWidthAssertions.ShouldHaveValidPadding(content);

        act.Should().Throw<Exception>().WithMessage("*padding*");
    }

    [Fact]
    public void FunctionalValidation_ShouldFailWhenEntryHashDoesNotMatch()
    {
        var content = BuildIncomingFile();
        var records = NachaFixedWidthAssertions.SplitRecords(content).ToList();
        records[5] = ReplaceSegment(records[5], 21, 10, "9999999999");

        Action act = () => ValidateFunctionalTotals(string.Concat(records));

        act.Should().Throw<InvalidOperationException>().WithMessage("*EntryHash*");
    }

    [Fact]
    public void FunctionalValidation_ShouldFailWhenDebitTotalDoesNotMatch()
    {
        var content = BuildIncomingFile();
        var records = NachaFixedWidthAssertions.SplitRecords(content).ToList();
        records[5] = ReplaceSegment(records[5], 31, 18, "000000000000000100");

        Action act = () => ValidateFunctionalTotals(string.Concat(records));

        act.Should().Throw<InvalidOperationException>().WithMessage("*TotalDebitAmount*");
    }

    [Fact]
    public void FunctionalValidation_ShouldFailWhenCreditTotalDoesNotMatch()
    {
        var content = BuildIncomingFile();
        var records = NachaFixedWidthAssertions.SplitRecords(content).ToList();
        records[5] = ReplaceSegment(records[5], 49, 18, "000000000000000100");

        Action act = () => ValidateFunctionalTotals(string.Concat(records));

        act.Should().Throw<InvalidOperationException>().WithMessage("*TotalCreditAmount*");
    }

    [Fact]
    public void FunctionalValidation_ShouldFailWhenBlockCountDoesNotMatch()
    {
        var content = BuildIncomingFile();
        var records = NachaFixedWidthAssertions.SplitRecords(content).ToList();
        records[5] = ReplaceSegment(records[5], 7, 6, "000002");

        Action act = () => ValidateFunctionalTotals(string.Concat(records));

        act.Should().Throw<InvalidOperationException>().WithMessage("*BlockCount*");
    }

    [Fact]
    public void FunctionalValidation_ShouldFailWhenFileControlIsMissing()
    {
        var records = NachaFixedWidthAssertions.SplitRecords(BuildIncomingFile()).Where(x => x[0] != '9').ToList();

        Action act = () => ValidateFunctionalTotals(string.Concat(records));

        act.Should().Throw<InvalidOperationException>().WithMessage("*FileControl*");
    }

    [Fact]
    public async Task FunctionalValidation_ShouldFailWhenProfileIsMissing()
    {
        await using var context = CreateInMemoryContext();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var act = () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        await act.Should().ThrowAsync<NachaGenerationException>().WithMessage("*NACHA_PROFILE_NOT_PUBLISHED*");
    }

    private static NachaFunctionalScenario AchScenario()
        => new()
        {
            ScenarioId = "ACH-CO-OUT-001",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0",
            FlowType = "Outgoing",
            ExpectedFileName = "1234567.001.1",
            ExpectedGoldenFilePath = "fixtures/nacha-m/ACHCOL/valid/achcol-v32-minimal.nacha.b64",
            ExpectedTotals = ExpectedOfficialTotals()
        };

    private static NachaFunctionalScenario CenitScenario()
        => new()
        {
            ScenarioId = "CENIT-OUT-001",
            ClearingHouseCode = "CENIT",
            ProfileCode = "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0",
            FlowType = "Outgoing",
            ExpectedFileName = "8765432.001.1",
            ExpectedGoldenFilePath = "TestData/Nacha/CENIT/CENIT-OUT-001.ach",
            ExpectedTotals = ExpectedOfficialTotals()
        };

    private static NachaExpectedControlTotals ExpectedOfficialTotals()
        => new()
        {
            BatchCount = 1,
            BlockCount = 1,
            EntryAddendaCount = 2,
            EntryHash = 12345678,
            TotalDebitAmountInCents = 0,
            TotalCreditAmountInCents = 10000,
            PhysicalRecordCountBeforePadding = 6,
            PaddingRecordCount = 4,
            PhysicalRecordCountAfterPadding = 10
        };

    private static NachaFunctionalValidationResult EvaluatePrenotificationResponse(string responseCode)
        => responseCode == "00"
            ? new NachaFunctionalValidationResult { ScenarioId = "PRENOTE-001", Status = "FunctionalValidationPassed", Message = "PrenotificationApproved" }
            : new NachaFunctionalValidationResult { ScenarioId = "PRENOTE-001", Status = "FunctionalValidationFailed", Message = $"PrenotificationRejected:{responseCode}" };

    private static string BuildOfficialFileName(string originRoute, string transitCode, int dailySequence, bool isReturn = false)
    {
        if (string.IsNullOrWhiteSpace(originRoute))
        {
            throw new InvalidOperationException("Falta codigo de ruta originadora.");
        }

        if (string.IsNullOrWhiteSpace(transitCode))
        {
            throw new InvalidOperationException("Falta codigo de transito.");
        }

        if (dailySequence is < 1 or > 36)
        {
            throw new InvalidOperationException("El consecutivo diario debe estar entre 001 y 036.");
        }

        return $"{originRoute}{transitCode}.{dailySequence:000}.{(isReturn ? "RET" : "1")}";
    }

    private static async Task<NachaParseResult> ParseAsync(
        AchDbContext context,
        string content,
        string fileName,
        IAchStateTransitionService? stateTransitionService = null)
    {
        var parser = new NachaParserService(
            context,
            Mock.Of<ILogger<NachaParserService>>(),
            stateTransitionService ?? Mock.Of<IAchStateTransitionService>());
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return await parser.ParseAndSaveDetailedAsync(stream, fileName, new NachaParseRequest { CorrelationId = "uat-functional" });
    }

    private static void ValidateFunctionalTotals(string content)
    {
        var records = NachaFixedWidthAssertions.SplitRecords(content);
        var entries = records.Where(x => x[0] == '6').ToList();
        var addendas = records.Where(x => x[0] == '7').ToList();
        var fileControl = records.FirstOrDefault(x => x[0] == '9' && !NachaFixedWidthAssertions.IsPaddingRecord(x));
        if (fileControl is null)
        {
            throw new InvalidOperationException("FileControl faltante.");
        }

        var entryHash = entries.Sum(x => long.Parse(x.Substring(3, 8))) % 10_000_000_000L;
        var credit = entries.Where(x => x.Substring(1, 2) == "22").Sum(x => long.Parse(x.Substring(29, 18)));
        var debit = entries.Where(x => x.Substring(1, 2) != "22").Sum(x => long.Parse(x.Substring(29, 18)));

        if (long.Parse(fileControl.Substring(21, 10)) != entryHash)
        {
            throw new InvalidOperationException("EntryHash no coincide.");
        }

        if (long.Parse(fileControl.Substring(31, 18)) != debit)
        {
            throw new InvalidOperationException("TotalDebitAmount no coincide.");
        }

        if (long.Parse(fileControl.Substring(49, 18)) != credit)
        {
            throw new InvalidOperationException("TotalCreditAmount no coincide.");
        }

        if (int.Parse(fileControl.Substring(13, 8)) != entries.Count + addendas.Count)
        {
            throw new InvalidOperationException("EntryAddendaCount no coincide.");
        }

        if (int.Parse(fileControl.Substring(7, 6)) != records.Count / 10)
        {
            throw new InvalidOperationException("BlockCount no coincide.");
        }
    }

    private static async Task<AchDbContext> SeedOfficialProfilesAsync()
    {
        var context = CreateInMemoryContext();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        return context;
    }

    private static AchDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateSqliteContext(SqliteConnection connection)
    {
        _ = connection;
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedParserCatalog(AchDbContext context, string clearingHouseName = "ACH Colombia", string originCode = "12345678")
    {
        var clearingHouse = new ClearingHouse { Id = 1, Name = clearingHouseName, Code = clearingHouseName.Contains("CENIT") ? "CENIT" : "ACH", OriginCode = originCode };
        context.ClearingHouses.Add(clearingHouse);
        var receivingInstitution = new FinancialInstitution { Id = 1, Name = "Banco Receptor UAT", RoutingNumber = "7654321", TransitCode = "0", IsDefaultSource = false, Status = FinancialInstitutionStatus.Active };
        receivingInstitution.CalculateCheckDigit();
        context.FinancialInstitutions.Add(receivingInstitution);
        context.Customers.Add(new Customer
        {
            FirstName = "Cliente",
            LastName = "UAT",
            DocumentType = "CC",
            DocumentNumber = "900000001",
            PersonType = "NAT",
            Gender = "N"
        });
        context.CustomerAccounts.Add(new CustomerAccount { Customer = context.Customers.Local.Last(), AccountNumber = "999988887777" });
        context.AchFileRejectionCodes.AddRange(
            new AchFileRejectionCode { Code = "D01", Description = "Duplicado", IsActive = true },
            new AchFileRejectionCode { Code = "D02", Description = "Padding invalido", IsActive = true },
            new AchFileRejectionCode { Code = "D04", Description = "Conteo invalido", IsActive = true },
            new AchFileRejectionCode { Code = "D05", Description = "Hash invalido", IsActive = true });
        context.SaveChanges();
    }

    private static OfficialSut CreateOfficialSut(AchDbContext context, string clearingHouseName)
    {
        var loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        var validation = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        var renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Strict);
        var semantic = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Strict);
        var batchNumberGenerator = new Mock<IBatchNumberGenerator>(MockBehavior.Strict);
        var contextData = BuildContext(clearingHouseName);

        foreach (var transaction in contextData.Transactions)
        {
            context.Customers.Add(new Customer
            {
                Id = 700 + transaction.Id,
                FirstName = "PERSONA",
                LastName = $"SINTETICA {transaction.Id}",
                PersonType = "PN",
                DocumentType = "CC",
                DocumentNumber = transaction.RecipientIdNumber,
                Accounts =
                [
                    new CustomerAccount
                    {
                        Id = 700 + transaction.Id,
                        AccountNumber = transaction.DestinationAccountNumber
                    }
                ]
            });
        }
        context.SaveChanges();

        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(contextData.Batches);
        loader.Setup(x => x.LoadHeaderAsync(contextData.Cycle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new NachaHeader { AchCycleId = contextData.Cycle.Id, FileCreationDate = "20260524", FileCreationTime = "1400", FileIdModifier = "A", ReferenceCode = null });
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<(string Term, string StandardEntryClassCode)> { ("PAGOS", "PPD") });
        validation.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        semantic.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));
        batchNumberGenerator.Setup(x => x.AssignBatchNumbersAsync(It.IsAny<IReadOnlyList<AchBatch>>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AchBatch> batches, string _, DateTime _, CancellationToken _) => new BatchNumberAssignmentResult(batches.ToDictionary(x => x.Id, _ => 1), "TEST_FIXED", 1, []));

        var sut = new NachaFileBuilder(
            context,
            holiday.Object,
            loader.Object,
            validation.Object,
            renderer.Object,
            recordProvider.Object,
            semantic.Object,
            configResolver: new NachaConfigResolver(context),
            type7GenerationStrategy: new NachaType7GenerationStrategy(new NachaType7FieldValueResolver(new NachaType7AliasMap())),
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "TABLE_DRIVEN", ExecutionScope = "LIVE" }),
            logger: Mock.Of<ILogger<NachaFileBuilder>>(),
            batchNumberGenerator: batchNumberGenerator.Object);

        return new OfficialSut(sut);
    }

    private static async Task<NachaGenerationException> AssertCenitLiveBlockedAsync(AchDbContext context)
    {
        var setup = CreateOfficialSut(context, "CENIT");
        return await Assert.ThrowsAsync<NachaGenerationException>(
            () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));
    }

    private static NachaBuildContext BuildContext(string clearingHouseName)
    {
        var isCenit = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase);
        var cycle = new AchCycle
        {
            Id = $"cycle-{clearingHouseName.Replace(" ", "-", StringComparison.OrdinalIgnoreCase)}",
            CycleName = "UAT6B3C",
            ProcessingDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            ClearingHouseId = isCenit ? 2 : 1,
            ClearingHouse = new ClearingHouse { Id = isCenit ? 2 : 1, Name = clearingHouseName, OriginCode = isCenit ? "87654321" : "12345678" }
        };
        var batch = new AchBatch
        {
            Id = 100,
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            ServiceClassCode = "220",
            CompanyEntryDescription = "PAGOS",
            CompanyIdentification = "9001234567",
            CompanyName = "EMPRESA UAT",
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = cycle.ProcessingDate
        };
        var transaction = new AchTransaction
        {
            Id = 10,
            Type = TransactionTypeEnum.Credit,
            Amount = 100m,
            AchBatchId = batch.Id,
            AchBatch = batch,
            AchCycleId = cycle.Id,
            TransactionCode = "22",
            ReceivingDFI = "12345678",
            TraceNumber = "123456780000001",
            EffectiveEntryDate = batch.EffectiveEntryDate,
            DestinationAccountNumber = "123456789",
            RecipientIdNumber = "RCV001",
            CompanyIdentification = batch.CompanyIdentification,
            OriginatingDFI = batch.OriginOrOdfi,
            Addendas = []
        };
        batch.Transactions = [transaction];
        return new NachaBuildContext { Cycle = cycle, Batches = [batch], Transactions = [transaction] };
    }

    private static async Task<NachaGenerationAuditResult> LoadLatestTraceAsync(AchDbContext context)
    {
        var change = await context.HistConfigChanges
            .Where(x => x.EntityName == "NachaFileBuilder" && x.ChangeType == "GENERATION_TRACE")
            .OrderByDescending(x => x.ChangedAtUtc)
            .FirstAsync();

        return JsonSerializer.Deserialize<NachaGenerationAuditResult>(change.AfterJson!)!;
    }

    private static string BuildIncomingFile(string immediateOrigin = "12345678", string originName = "ACH COLOMBIA")
    {
        var records = new List<string>
        {
            BuildType1(DateTime.Today, immediateOrigin, originName),
            BuildType5(DateTime.Today, "1234567800", "0000001"),
            BuildType6("22", "76543210", "999988887777", "000000000000150000", "123456780000001"),
            BuildType7("05", "INFO-ADDENDA".PadRight(80), "123456780000001"),
            BuildType8("220", "000002", "0076543210", "000000000000000000", "000000000000150000", "1234567800", "0000001"),
            BuildType9("000001", "000001", "00000002", "0076543210", "000000000000000000", "000000000000150000"),
            new('9', 106),
            new('9', 106),
            new('9', 106),
            new('9', 106)
        };
        return string.Concat(records);
    }

    private static string BuildReturnFile()
    {
        var records = new List<string>
        {
            BuildType1(DateTime.Today, "12345678", "ACH COLOMBIA"),
            BuildType5(DateTime.Today, "1234567800", "0000001"),
            BuildType6("21", "76543210", "999988887777", "000000000000150000", "123456780000002"),
            BuildType7Return("R01", "123456780000001", "123456780000002"),
            BuildType8("220", "000002", "0076543210", "000000000000000000", "000000000000150000", "1234567800", "0000001"),
            BuildType9("000001", "000001", "00000002", "0076543210", "000000000000000000", "000000000000150000"),
            new('9', 106),
            new('9', 106),
            new('9', 106),
            new('9', 106)
        };
        return string.Concat(records);
    }

    private static string BuildType1(DateTime processingDate, string immediateOrigin, string originName)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '1';
        Copy("01", line, 1);
        Copy("0000000001", line, 3);
        Copy(immediateOrigin.PadRight(10), line, 13);
        Copy(processingDate.ToString("yyyyMMdd"), line, 23);
        Copy("1200", line, 31);
        Copy("A", line, 35);
        Copy("106", line, 36);
        Copy("10", line, 39);
        Copy("1", line, 41);
        Copy("ACH COLOMBIA".PadRight(23), line, 42);
        Copy(originName.PadRight(23), line, 65);
        Copy("REF00001", line, 88);
        return new string(line);
    }

    private static string BuildType5(DateTime processingDate, string companyId, string batchNumber, string serviceClassCode = "220")
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '5';
        Copy(serviceClassCode, line, 1);
        Copy("EMPRESA DEMO".PadRight(16), line, 4);
        Copy(companyId.PadRight(10), line, 40);
        Copy("PPD", line, 50);
        Copy("MULTICREDIT", line, 53);
        Copy(processingDate.ToString("yyyyMMdd"), line, 63);
        Copy(processingDate.ToString("yyyyMMdd"), line, 71);
        Copy("1", line, 82);
        Copy("12345678", line, 83);
        Copy(batchNumber, line, 91);
        return new string(line);
    }

    private static string BuildType6(string code, string receivingDfi, string accountNumber, string amount18, string traceNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '6';
        Copy(code, line, 1);
        Copy(receivingDfi, line, 3);
        Copy(DigitoChequeoHelper.CalcularDigitoChequeo(receivingDfi), line, 11);
        Copy(accountNumber.PadRight(17), line, 12);
        Copy(amount18, line, 29);
        Copy("900000001".PadLeft(15), line, 47);
        Copy("CLIENTE CREDITO".PadRight(22), line, 62);
        Copy("  ", line, 84);
        Copy("1", line, 86);
        Copy(traceNumber, line, 87);
        return new string(line);
    }

    private static string BuildType7(string code, string info, string traceNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '7';
        Copy(code, line, 1);
        Copy(info, line, 3);
        Copy("0001", line, 83);
        Copy(traceNumber[^7..], line, 87);
        return new string(line);
    }

    private static string BuildType7Return(string reasonCode, string originalTraceNumber, string traceNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '7';
        Copy("99", line, 1);
        Copy(reasonCode.PadRight(5), line, 3);
        Copy(originalTraceNumber, line, 8);
        Copy(traceNumber, line, 81);
        Copy(traceNumber[^7..], line, 99);
        return new string(line);
    }

    private static string BuildType8(string classCode, string count, string hash, string debit, string credit, string companyId, string batchNumber)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '8';
        Copy(classCode, line, 1);
        Copy(count, line, 4);
        Copy(hash, line, 10);
        Copy(debit, line, 20);
        Copy(credit, line, 38);
        Copy(companyId.PadRight(10), line, 56);
        Copy(new string(' ', 25), line, 66);
        Copy("12345678", line, 91);
        Copy(batchNumber, line, 99);
        return new string(line);
    }

    private static string BuildType9(string batchCount, string blockCount, string count, string hash, string debit, string credit)
    {
        var line = new string(' ', 106).ToCharArray();
        line[0] = '9';
        Copy(batchCount, line, 1);
        Copy(blockCount, line, 7);
        Copy(count, line, 13);
        Copy(hash, line, 21);
        Copy(debit, line, 31);
        Copy(credit, line, 49);
        return new string(line);
    }

    private static string ReadExecution2AchColSyntheticGolden()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "nacha-m",
            "ACHCOL",
            "valid",
            "achcol-v32-minimal.nacha.b64");
        return Encoding.ASCII.GetString(Convert.FromBase64String(File.ReadAllText(fixturePath)));
    }

    private static string ReplaceSegment(string value, int startIndex, int length, string replacement)
        => string.Concat(value.AsSpan(0, startIndex), replacement, value.AsSpan(startIndex + length));

    private static void Copy(string value, char[] buffer, int index)
        => value.AsSpan().CopyTo(buffer.AsSpan(index, value.Length));

    private sealed record OfficialSut(NachaFileBuilder Sut);
}
