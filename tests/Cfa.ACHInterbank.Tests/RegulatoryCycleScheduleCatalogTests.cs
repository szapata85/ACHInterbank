using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Tests;

public sealed class RegulatoryCycleScheduleCatalogTests
{
    [Fact]
    public void AchColombiaV35HistoricalPolicy_PreservesExactFiveCycleSnapshot()
    {
        var policy = RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalPolicy;

        Assert.Equal(RegulatoryCycleScheduleCatalog.AchColombiaCode, policy.ClearingHouseCode);
        Assert.Equal(RegulatoryCycleScheduleCatalog.HistoricalBootstrapPolicyVersion, policy.PolicyVersion);
        Assert.Equal(RegulatoryCycleScheduleCatalog.HistoricalBootstrapEffectiveFrom, policy.EffectiveFrom);
        Assert.Equal(5, policy.Cycles.Count);

        AssertCycle(policy.Cycles[0], 1, 19, 1, 8, 30, 9, 30, allowsMonetaryDebit: true);
        AssertCycle(policy.Cycles[1], 2, 8, 31, 11, 0, 12, 0, allowsMonetaryDebit: true);
        AssertCycle(policy.Cycles[2], 3, 11, 1, 14, 0, 15, 0, allowsMonetaryDebit: true);
        AssertCycle(policy.Cycles[3], 4, 14, 1, 16, 0, 17, 0, allowsMonetaryDebit: true);
        AssertCycle(policy.Cycles[4], 5, 16, 1, 18, 0, 19, 0, allowsMonetaryDebit: false);

        Assert.Equal(policy.Cycles, RegulatoryCycleScheduleCatalog.AchColombiaV35HistoricalFingerprint);
        Assert.Equal(policy.Cycles, RegulatoryCycleScheduleCatalog.GetRequired(RegulatoryCycleScheduleCatalog.AchColombiaCode));
    }

    [Fact]
    public void AchColombiaSevenCycleConfirmedPolicy_IsEffectiveOctober14WithSevenExactWindows()
    {
        var policy = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleConfirmedPolicy;
        Assert.Equal("ACHCOL", policy.ClearingHouseCode);
        Assert.Equal("ACHCOL-7C-20261014", policy.PolicyVersion);
        Assert.Equal(new DateTime(2026, 10, 14, 0, 0, 0, DateTimeKind.Utc), policy.EffectiveFrom);
        Assert.Equal(7, policy.Cycles.Count);
        var starts = new[] { "19:01", "07:01", "09:01", "11:01", "12:31", "14:31", "16:01" };
        var closes = new[] { "07:00", "09:00", "11:00", "12:30", "14:30", "16:00", "18:00" };
        var statementEnds = new[] { "07:30", "09:30", "11:30", "13:00", "15:00", "16:30", "18:30" };
        var paymentEnds = new[] { "07:45", "09:45", "11:45", "13:15", "15:15", "16:45", "18:45" };
        var releases = new[] { "08:00", "10:00", "12:00", "13:30", "15:30", "17:00", "19:00" };
        for (var index = 0; index < 7; index++)
        {
            var cycle = policy.Cycles[index];
            Assert.Equal(index + 1, cycle.CycleNumber);
            Assert.Equal(TimeSpan.Parse(starts[index]), cycle.StartTime);
            Assert.Equal(TimeSpan.Parse(closes[index]), cycle.EndTime);
            Assert.Equal(cycle.EndTime, cycle.CutoffTime);
            Assert.Equal(cycle.EndTime, cycle.CompensationStatementStartTime);
            Assert.Equal(TimeSpan.Parse(statementEnds[index]), cycle.CompensationStatementEndTime);
            Assert.Equal(cycle.CompensationStatementEndTime, cycle.CompensationPaymentStartTime);
            Assert.Equal(TimeSpan.Parse(paymentEnds[index]), cycle.CompensationPaymentEndTime);
            Assert.Equal(cycle.CompensationPaymentEndTime, cycle.OutputReleaseStartTime);
            Assert.Equal(TimeSpan.Parse(releases[index]), cycle.OutputReleaseEndTime);
            Assert.Equal(cycle.OutputReleaseEndTime, cycle.OutputReleaseTime);
        }
        Assert.False(policy.Cycles[6].AllowsMonetaryDebit);
    }

    [Fact]
    public void AchColombiaSevenCycleCandidate_RemainsFailClosedWithoutOfficialEffectiveFrom()
    {
        var candidate = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate;

        Assert.Equal(7, candidate.Cycles.Count);
        Assert.Null(candidate.EffectiveFrom);
        Assert.False(candidate.IsActivatable);

        var error = Assert.Throws<InvalidOperationException>(() => candidate.RequireActivatable());
        Assert.Contains("EffectiveFrom", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AchColombiaSevenCycleCandidate_RemainsFailClosedWhenCycleFourStartIsUnresolved()
    {
        var candidate = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate with
        {
            EffectiveFrom = DateTime.SpecifyKind(new DateTime(2099, 1, 2), DateTimeKind.Utc)
        };

        Assert.Null(candidate.Cycles.Single(cycle => cycle.CycleNumber == 4).StartTime);
        Assert.False(candidate.IsActivatable);

        var error = Assert.Throws<InvalidOperationException>(() => candidate.RequireActivatable());
        Assert.Contains("Ciclo 4", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("inicio", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AchColombiaSevenCycleCandidate_ModelsKnownOrdinaryTransactionPermissions()
    {
        var candidate = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate;

        foreach (var cycle in candidate.Cycles.Where(cycle => cycle.CycleNumber <= 6))
        {
            Assert.True(cycle.AllowsMonetaryDebit);
            Assert.True(cycle.AllowsMonetaryCredit);
            Assert.True(cycle.AllowsDebitPrenotification);
            Assert.True(cycle.AllowsCreditPrenotification);
        }

        Assert.True(candidate.Cycles.Single(cycle => cycle.CycleNumber == 5).AllowsMonetaryDebit);
        Assert.True(candidate.Cycles.Single(cycle => cycle.CycleNumber == 6).AllowsMonetaryDebit);

        var cycle7 = candidate.Cycles.Single(cycle => cycle.CycleNumber == 7);
        Assert.False(cycle7.AllowsMonetaryDebit);
        Assert.True(cycle7.AllowsMonetaryCredit);
        Assert.True(cycle7.AllowsDebitPrenotification);
        Assert.True(cycle7.AllowsCreditPrenotification);
    }

    [Theory]
    [InlineData(1, "P4,P5,P6,P7")]
    [InlineData(2, "P5,P6,P7,C1")]
    [InlineData(3, "P6,P7,C1,C2")]
    [InlineData(4, "P7,C1,C2,C3")]
    [InlineData(5, "C1,C2,C3,C4")]
    [InlineData(6, "C2,C3,C4,C5")]
    [InlineData(7, "C3,C4,C5,C6")]
    public void AchColombiaSevenCycleCandidate_ModelsFourSourceMovingReturnWindow(
        int returnCycleNumber,
        string expectedSourcesCsv)
    {
        var window = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate.ReturnWindows
            .Single(item => item.ReturnCycleNumber == returnCycleNumber);

        var actual = window.EligibleSources
            .Select(source => $"{(source.SourceDay == RegulatoryCycleSourceDay.PreviousOperationalDay ? "P" : "C")}{source.CycleNumber}")
            .ToArray();

        Assert.Equal(expectedSourcesCsv.Split(','), actual);
        Assert.Equal(4, window.EligibleSources.Count);
    }

    [Theory]
    [InlineData(1, 3, RegulatoryCycleSourceDay.PreviousOperationalDay)]
    [InlineData(2, 4, RegulatoryCycleSourceDay.PreviousOperationalDay)]
    [InlineData(3, 5, RegulatoryCycleSourceDay.PreviousOperationalDay)]
    [InlineData(4, 6, RegulatoryCycleSourceDay.PreviousOperationalDay)]
    [InlineData(5, 7, RegulatoryCycleSourceDay.PreviousOperationalDay)]
    [InlineData(6, 1, RegulatoryCycleSourceDay.CurrentOperationalDay)]
    [InlineData(7, 2, RegulatoryCycleSourceDay.CurrentOperationalDay)]
    public void AchColombiaSevenCycleCandidate_BlocksFifthReturnSourceOutsideMovingWindow(
        int returnCycleNumber,
        int fifthSourceCycleNumber,
        RegulatoryCycleSourceDay sourceDay)
    {
        var window = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate.ReturnWindows
            .Single(item => item.ReturnCycleNumber == returnCycleNumber);

        Assert.False(window.Allows(fifthSourceCycleNumber, sourceDay));
    }

    [Fact]
    public void AchColombiaSevenCycleCandidate_PreservesKnownStageWindowsWithoutResolvingCycleFourStart()
    {
        var candidate = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate;
        var cycle1 = candidate.Cycles.Single(cycle => cycle.CycleNumber == 1);
        var cycle7 = candidate.Cycles.Single(cycle => cycle.CycleNumber == 7);

        Assert.Equal(new TimeSpan(19, 1, 0), cycle1.StartTime);
        Assert.Equal(new TimeSpan(7, 0, 0), cycle1.EndTime);
        Assert.Equal(new TimeSpan(7, 0, 0), cycle1.CompensationStatementStartTime);
        Assert.Equal(new TimeSpan(7, 30, 0), cycle1.CompensationStatementEndTime);
        Assert.Equal(new TimeSpan(7, 30, 0), cycle1.CompensationPaymentStartTime);
        Assert.Equal(new TimeSpan(7, 45, 0), cycle1.CompensationPaymentEndTime);
        Assert.Equal(new TimeSpan(7, 45, 0), cycle1.OutputReleaseStartTime);
        Assert.Equal(new TimeSpan(8, 0, 0), cycle1.OutputReleaseEndTime);
        Assert.Equal(new TimeSpan(8, 0, 0), cycle1.OutputReleaseTime);

        Assert.Equal(new TimeSpan(16, 1, 0), cycle7.StartTime);
        Assert.Equal(new TimeSpan(18, 0, 0), cycle7.EndTime);
        Assert.False(cycle7.AllowsMonetaryDebit);
    }

    private static void AssertCycle(
        RegulatoryCycleSchedule cycle,
        int number,
        int startHour,
        int startMinute,
        int closeHour,
        int closeMinute,
        int releaseHour,
        int releaseMinute,
        bool allowsMonetaryDebit)
    {
        Assert.Equal(number, cycle.CycleNumber);
        Assert.Equal(new TimeSpan(startHour, startMinute, 0), cycle.StartTime);
        Assert.Equal(new TimeSpan(closeHour, closeMinute, 0), cycle.EndTime);
        Assert.Equal(cycle.EndTime, cycle.CutoffTime);
        Assert.Equal(new TimeSpan(releaseHour, releaseMinute, 0), cycle.OutputReleaseTime);
        Assert.Equal(allowsMonetaryDebit, cycle.AllowsMonetaryDebit);
        Assert.Null(cycle.CompensationStatementStartTime);
        Assert.Null(cycle.CompensationStatementEndTime);
        Assert.Null(cycle.CompensationPaymentStartTime);
        Assert.Null(cycle.CompensationPaymentEndTime);
        Assert.Null(cycle.OutputReleaseStartTime);
        Assert.Null(cycle.OutputReleaseEndTime);
    }
}
