using Cfa.ACHInterbank.Application.ACH.Services;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationReceiptWindowI11Tests
{
    private const string Bogota = "America/Bogota";

    [Fact]
    public void I11_AchColombiaCycleFive_UsesLocalDayBoundaryNotUtcMidnight()
    {
        var window = PrenotificationReceiptWindow.Create(
            new DateTime(2026, 11, 3),
            new DateTimeOffset(2026, 11, 3, 20, 30, 0, TimeSpan.Zero),
            Bogota);

        Assert.Equal(new DateTime(2026, 11, 3), window.OperationalStartDate);
        Assert.Equal(new DateTime(2026, 11, 4), window.OperationalEndExclusive);
        Assert.Equal(new DateTime(2026, 11, 3, 5, 0, 0, DateTimeKind.Utc), window.UtcOperationalStartInclusive);
        Assert.Equal(new DateTime(2026, 11, 2, 5, 0, 0, DateTimeKind.Utc), window.UtcUnclassifiedStartInclusive);
        Assert.Equal(new DateTime(2026, 11, 4, 5, 0, 0, DateTimeKind.Utc), window.UtcEndExclusive);
        // A late/ambiguous file at 23:45 local still belongs to the same operational date.
        Assert.True(new DateTime(2026, 11, 4, 4, 45, 0, DateTimeKind.Utc) < window.UtcEndExclusive);
    }

    [Fact]
    public void I11_CenitFinalOutputAt1915Local_IsWithinUtcWindowAfterMidnight()
    {
        var deadline = new DateTimeOffset(2026, 11, 4, 0, 15, 0, TimeSpan.Zero);
        var window = PrenotificationReceiptWindow.Create(new DateTime(2026, 11, 3), deadline, Bogota);

        Assert.Equal(new DateTime(2026, 11, 4), window.OperationalEndExclusive);
        Assert.True(window.UtcOperationalStartInclusive <= deadline.UtcDateTime);
        Assert.True(deadline.UtcDateTime < window.UtcEndExclusive);
        // A CENIT response at 19:20 local is 00:20 UTC next day.
        Assert.True(new DateTime(2026, 11, 4, 0, 20, 0, DateTimeKind.Utc) < window.UtcEndExclusive);
        Assert.Equal(new DateTime(2026, 11, 4, 5, 0, 0, DateTimeKind.Utc), window.UtcEndExclusive);
    }

    [Fact]
    public void I11_AchColombiaLateCycle_CanCrossWeekendWithLocalDateBounds()
    {
        var window = PrenotificationReceiptWindow.Create(
            new DateTime(2026, 11, 6), // Friday
            new DateTimeOffset(2026, 11, 9, 20, 30, 0, TimeSpan.Zero), // Monday 15:30 Bogota
            Bogota);

        Assert.Equal(new DateTime(2026, 11, 6), window.OperationalStartDate);
        Assert.Equal(new DateTime(2026, 11, 10), window.OperationalEndExclusive);
        Assert.Equal(new DateTime(2026, 11, 10, 5, 0, 0, DateTimeKind.Utc), window.UtcEndExclusive);
    }

    [Fact]
    public void I11_ExclusiveEndAtLocalMidnight_IsNotIncluded()
    {
        var window = PrenotificationReceiptWindow.Create(
            new DateTime(2026, 11, 3),
            new DateTimeOffset(2026, 11, 4, 0, 15, 0, TimeSpan.Zero), Bogota);
        Assert.Equal(new DateTime(2026, 11, 4, 5, 0, 0, DateTimeKind.Utc), window.UtcEndExclusive);
        Assert.True(new DateTime(2026, 11, 4, 4, 59, 59, DateTimeKind.Utc) < window.UtcEndExclusive);
        Assert.False(new DateTime(2026, 11, 4, 5, 0, 0, DateTimeKind.Utc) < window.UtcEndExclusive);
    }

    [Fact]
    public void I11_MissingZoneOrDeadlineBeforeSource_FailsClosed()
    {
        Assert.Throws<InvalidOperationException>(() => PrenotificationReceiptWindow.Create(
            new DateTime(2026, 11, 3),
            new DateTimeOffset(2026, 11, 4, 0, 15, 0, TimeSpan.Zero), " "));
        Assert.Throws<InvalidOperationException>(() => PrenotificationReceiptWindow.Create(
            new DateTime(2026, 11, 4),
            new DateTimeOffset(2026, 11, 3, 20, 30, 0, TimeSpan.Zero), Bogota));
    }
}
