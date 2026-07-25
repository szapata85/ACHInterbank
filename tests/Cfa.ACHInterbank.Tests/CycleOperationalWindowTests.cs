using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Tests.TestSupport;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CycleOperationalWindowTests
{
    public static TheoryData<DateTime, bool> SameDayWindowBoundaries => new()
    {
        { TestClock.OperationalDate.AddTicks(-1), false },
        { TestClock.OperationalDate, true },
        { TestClock.OperationalDate.AddHours(12), true },
        { TestClock.OperationalDate.AddHours(23).AddMinutes(59), true },
        { TestClock.OperationalDate.AddHours(23).AddMinutes(59).AddMilliseconds(0.001), false },
        { TestClock.OperationalDate.AddHours(23).AddMinutes(59).AddSeconds(30), false },
        { TestClock.OperationalDate.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(0.999), false },
        { TestClock.OperationalDate.AddDays(1), false }
    };

    [Theory]
    [MemberData(nameof(SameDayWindowBoundaries))]
    public void SameDayWindow_UsesInclusiveExactEndTime(DateTime now, bool expected)
    {
        var window = ContrapartidaDispatchJobService.BuildCycleWindow(
            TestClock.OperationalDate,
            TimeSpan.Zero,
            new TimeSpan(23, 59, 0));

        Assert.Equal(expected, now >= window.Start && now <= window.End);
    }

    [Fact]
    public void CrossMidnightWindow_UsesPreviousDateForOpening()
    {
        var window = ContrapartidaDispatchJobService.BuildCycleWindow(
            TestClock.OperationalDate,
            new TimeSpan(22, 0, 0),
            new TimeSpan(2, 0, 0));

        Assert.Equal(TestClock.OperationalDate.AddDays(-1).AddHours(22), window.Start);
        Assert.Equal(TestClock.OperationalDate.AddHours(2), window.End);
        Assert.True(TestClock.OperationalDate.AddDays(-1).AddHours(22) >= window.Start);
        Assert.True(TestClock.OperationalDate.AddHours(1) <= window.End);
        Assert.False(TestClock.OperationalDate.AddHours(2).AddTicks(1) <= window.End);
        Assert.False(TestClock.OperationalDate.AddDays(1) >= window.Start && TestClock.OperationalDate.AddDays(1) <= window.End);
    }

    [Fact]
    public void FixedClock_ReproducesRunNearMidnightWithoutWaiting()
    {
        var localNearMidnight = TestClock.OperationalDate.AddHours(23).AddMinutes(59).AddSeconds(30);
        var utcNearMidnight = new DateTimeOffset(localNearMidnight, TimeSpan.FromHours(-5)).ToUniversalTime();
        var clock = new FixedTimeProvider(utcNearMidnight, TestClock.BogotaTimeZone);

        Assert.Equal(localNearMidnight, clock.GetLocalNow().DateTime);
        Assert.False(localNearMidnight <= TestClock.OperationalDate.AddHours(23).AddMinutes(59));
    }
}
