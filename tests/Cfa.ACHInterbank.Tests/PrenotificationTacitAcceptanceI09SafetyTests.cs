using Cfa.ACHInterbank.Application.ACH.Services;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationTacitAcceptanceI09SafetyTests
{
    private static readonly DateTimeOffset AchColEnd =
        new(2026, 11, 3, 20, 30, 0, TimeSpan.Zero); // 15:30 Bogota, ACHCOL 7 ciclos
    private static readonly DateTimeOffset CenitEnd =
        new DateTimeOffset(2026, 11, 3, 0, 15, 0, TimeSpan.Zero).AddDays(1); // 19:15 Bogota, CENIT 5 ciclos

    [Theory]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, false, true)]
    public void I09_FailClosed_WhenAnySafetyBarrierFails(
        bool currentAttempt, bool transmitted, bool inboundPending, bool responsePresent)
    {
        Assert.False(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            AchColEnd, AchColEnd, AchColEnd.AddHours(1),
            currentAttempt, transmitted, inboundPending, responsePresent));
    }

    [Theory]
    [InlineData("ACHCOL")]
    [InlineData("CENIT")]
    public void I09_DoesNotAcceptBeforeRecalculatedReleaseEnd(string house)
    {
        var deadline = house == "ACHCOL" ? AchColEnd : CenitEnd;
        Assert.False(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            deadline.AddHours(-1), deadline, deadline.AddTicks(-1), true, true, false, false));
        Assert.True(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            deadline.AddHours(-1), deadline, deadline, true, true, false, false));
    }

    [Fact]
    public void I09_OldLaterDeadline_IsNotShortenedByPolicyRecalculation()
    {
        Assert.False(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            AchColEnd.AddMinutes(30), AchColEnd, AchColEnd, true, true, false, false));
        Assert.True(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            AchColEnd.AddMinutes(30), AchColEnd, AchColEnd.AddMinutes(30), true, true, false, false));
    }

    [Fact]
    public void I09_MissingRecalculatedDeadline_NeverAccepts_EvenWithOldDueDate()
        => Assert.False(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            AchColEnd.AddHours(-1), null, AchColEnd.AddDays(1), true, true, false, false));

    [Fact]
    public void I09_LegacyPrenotification_RequiresRealTransmissionAndRecalculatedDeadline()
    {
        Assert.False(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            null, AchColEnd, AchColEnd, true, false, false, false));
        Assert.True(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            null, AchColEnd, AchColEnd, true, true, false, false));
    }

    [Fact]
    public void I09_CenitOperationalDay_WaitsUntilOutputIsAvailable()
    {
        var early = CenitEnd.AddMinutes(-30); // CENIT cycle 5 cutoff at 18:45 local
        Assert.False(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            early, CenitEnd, early, true, true, false, false));
        Assert.True(PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
            early, CenitEnd, CenitEnd, true, true, false, false));
    }
}
