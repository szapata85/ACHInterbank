using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Tests.TestSupport;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CycleOperationalWindowTests
{
    private const string Bogota = "America/Bogota";
    private readonly OperationalCycleWindowResolver _sut = new();

    [Theory]
    [InlineData("2026-08-04T12:29:59.9999999+00:00", false)]
    [InlineData("2026-08-04T12:30:00+00:00", true)]
    [InlineData("2026-08-04T17:00:00+00:00", true)]
    [InlineData("2026-08-05T03:30:00+00:00", true)]
    [InlineData("2026-08-05T03:30:00.0000001+00:00", false)]
    public void SameDayWindow_AtInclusiveBoundaries_ResolvesExpectedStatus(string instant, bool expectedInside)
    {
        var result = _sut.Resolve(
            new DateTime(2026, 8, 4),
            new TimeSpan(7, 30, 0),
            new TimeSpan(22, 30, 0),
            Bogota,
            DateTimeOffset.Parse(instant));

        Assert.Equal(expectedInside, result.IsInside);
    }

    [Theory]
    [InlineData("2026-08-04T00:00:59.9999999+00:00", false)]
    [InlineData("2026-08-04T00:01:00+00:00", true)]
    [InlineData("2026-08-04T05:00:00+00:00", true)]
    [InlineData("2026-08-04T13:30:00+00:00", true)]
    [InlineData("2026-08-04T13:30:00.0000001+00:00", false)]
    public void AchColombiaCycleOne_AtExactRegulatoryBoundaries_UsesPreviousCalendarDate(
        string instant,
        bool expectedInside)
    {
        var result = _sut.Resolve(
            new DateTime(2026, 8, 4),
            new TimeSpan(19, 1, 0),
            new TimeSpan(8, 30, 0),
            Bogota,
            DateTimeOffset.Parse(instant));

        Assert.Equal(new DateTime(2026, 8, 3, 19, 1, 0), result.LocalStart);
        Assert.Equal(new DateTime(2026, 8, 4, 8, 30, 0), result.LocalEnd);
        Assert.Equal(expectedInside, result.IsInside);
    }

    [Fact]
    public void OvernightWindow_ForMondayProcessingDate_OpensOnPreviousCalendarSunday()
    {
        var result = _sut.Resolve(
            new DateTime(2026, 8, 3),
            new TimeSpan(19, 1, 0),
            new TimeSpan(8, 30, 0),
            Bogota,
            new DateTimeOffset(2026, 8, 3, 1, 0, 0, TimeSpan.Zero));

        Assert.Equal(DayOfWeek.Sunday, result.LocalStart.DayOfWeek);
        Assert.Equal(new DateTime(2026, 8, 2, 19, 1, 0), result.LocalStart);
        Assert.Equal(new DateTime(2026, 8, 3), result.ProcessingDate);
    }

    [Fact]
    public void OvernightWindow_ForProcessingDateAfterHoliday_DoesNotMoveOpeningToPreviousBusinessDay()
    {
        var processingDateAfterHoliday = new DateTime(2026, 8, 18);

        var result = _sut.Resolve(
            processingDateAfterHoliday,
            new TimeSpan(19, 1, 0),
            new TimeSpan(8, 30, 0),
            Bogota,
            new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(processingDateAfterHoliday.AddDays(-1).AddHours(19).AddMinutes(1), result.LocalStart);
        Assert.Equal(processingDateAfterHoliday, result.ProcessingDate);
    }

    [Fact]
    public void UtcInstant_WithBogotaConfiguration_ConvertsToOperationalLocalTime()
    {
        var result = _sut.Resolve(
            new DateTime(2026, 8, 4),
            TimeSpan.Zero,
            new TimeSpan(23, 59, 59),
            Bogota,
            new DateTimeOffset(2026, 8, 4, 15, 45, 0, TimeSpan.Zero));

        Assert.Equal(new DateTime(2026, 8, 4, 10, 45, 0), result.LocalNow);
    }

    [Fact]
    public void ConfiguredTimeZone_WithDifferentHostClockZones_ProducesSameWindowDecision()
    {
        var instant = new DateTimeOffset(2026, 8, 4, 13, 30, 0, TimeSpan.Zero);
        var utcHostClock = new FixedTimeProvider(instant, TimeZoneInfo.Utc);
        var otherHostClock = new FixedTimeProvider(
            instant,
            TimeZoneInfo.CreateCustomTimeZone("HostPlusNine", TimeSpan.FromHours(9), "Host +9", "Host +9"));

        var first = _sut.Resolve(new DateTime(2026, 8, 4), new TimeSpan(19, 1, 0), new TimeSpan(8, 30, 0), Bogota, utcHostClock.GetUtcNow());
        var second = _sut.Resolve(new DateTime(2026, 8, 4), new TimeSpan(19, 1, 0), new TimeSpan(8, 30, 0), Bogota, otherHostClock.GetUtcNow());

        Assert.Equal(first, second);
        Assert.True(first.IsInside);
    }
}
