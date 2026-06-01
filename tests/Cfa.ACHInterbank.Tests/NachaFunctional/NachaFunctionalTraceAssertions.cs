using Cfa.ACHInterbank.Application.ACH.Models;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests.NachaFunctional;

internal static class NachaFunctionalTraceAssertions
{
    public static void ShouldContainFunctionalGenerationTrace(
        this NachaGenerationAuditResult trace,
        NachaFunctionalScenario scenario)
    {
        trace.Mode.Should().Be("TABLE_DRIVEN");
        trace.ProfileCode.Should().Be(scenario.ProfileCode);
        trace.ClearingHouseCode.Should().Be(scenario.ClearingHouseCode);
        trace.Phase.Should().BeOneOf("6B.3B", "6B.3C");
        trace.FileIdModifier.Should().NotBeNull();
        trace.FileIdModifier!.ResolvedValue.Should().NotBeNullOrWhiteSpace();
        trace.FileTotals.Should().NotBeNull();
        trace.FileTotals!.BatchCount.Should().Be(scenario.ExpectedTotals.BatchCount);
        trace.FileTotals.BlockCount.Should().Be(scenario.ExpectedTotals.BlockCount);
        trace.FileTotals.EntryAddendaCount.Should().Be(scenario.ExpectedTotals.EntryAddendaCount);
        trace.FileTotals.EntryHash.Should().Be(scenario.ExpectedTotals.EntryHash);
        trace.FileTotals.TotalDebitAmountInCents.Should().Be(scenario.ExpectedTotals.TotalDebitAmountInCents);
        trace.FileTotals.TotalCreditAmountInCents.Should().Be(scenario.ExpectedTotals.TotalCreditAmountInCents);
        trace.FileTotals.PhysicalRecordCountBeforePadding.Should().Be(scenario.ExpectedTotals.PhysicalRecordCountBeforePadding);
        trace.FileTotals.PaddingRecordCount.Should().Be(scenario.ExpectedTotals.PaddingRecordCount);
        trace.FileTotals.PhysicalRecordCountAfterPadding.Should().Be(scenario.ExpectedTotals.PhysicalRecordCountAfterPadding);
        trace.BatchTotals.Should().NotBeEmpty();
        trace.Status.Should().Be("Ok");
    }
}
