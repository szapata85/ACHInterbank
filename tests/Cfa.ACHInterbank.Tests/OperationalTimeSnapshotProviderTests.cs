using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public class OperationalTimeSnapshotProviderTests
{
    private static readonly TimeZoneInfo Bogota = TimeZoneInfo.CreateCustomTimeZone(
        "America/Bogota-Test",
        TimeSpan.FromHours(-5),
        "Bogota test",
        "Bogota test");

    [Theory]
    [InlineData("2026-07-16T04:59:00Z", 2026, 7, 15, 23, 59)]
    [InlineData("2026-07-16T05:01:00Z", 2026, 7, 16, 0, 1)]
    public void CaptureNow_UsesBogotaAcrossUtcMidnight(
        string utc,
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        var provider = new OperationalTimeSnapshotProvider(
            new MutableTimeProvider(DateTimeOffset.Parse(utc)),
            Bogota);

        var snapshot = provider.CaptureNow();

        Assert.Equal(new DateOnly(year, month, day), snapshot.OperationalDate);
        Assert.Equal(new DateTime(year, month, day, hour, minute, 0), snapshot.BogotaTimestamp);
        Assert.Equal(DateTimeKind.Utc, snapshot.CapturedAtUtc.Kind);
    }

    [Fact]
    public void GetOrCreate_ReusesOneSnapshotForAllFileFields()
    {
        var time = new MutableTimeProvider(DateTimeOffset.Parse("2026-07-16T13:00:00Z"));
        var provider = new OperationalTimeSnapshotProvider(time, Bogota);

        var first = provider.GetOrCreate("NACHA:cycle-synthetic", new DateOnly(2026, 7, 16), new TimeOnly(8, 0));
        time.UtcNow = DateTimeOffset.Parse("2026-07-17T02:00:00Z");
        var retry = provider.GetOrCreate("NACHA:cycle-synthetic", new DateOnly(2026, 7, 17), new TimeOnly(21, 0));

        Assert.Same(first, retry);
        Assert.Equal(new DateOnly(2026, 7, 16), retry.OperationalDate);
        Assert.Equal(new DateTime(2026, 7, 16, 8, 0, 0), retry.BogotaTimestamp);
        Assert.Equal(new DateTime(2026, 7, 16, 13, 0, 0, DateTimeKind.Utc), retry.CapturedAtUtc);
    }

    [Fact]
    public void ResolveBogotaTimeZone_UsesControlledWindowsFallback()
    {
        var expected = Bogota;
        var resolved = OperationalTimeSnapshotProvider.ResolveBogotaTimeZone(
            id => id == OperationalTimeSnapshotProvider.WindowsTimeZoneId ? expected : null);

        Assert.Same(expected, resolved);
    }

    [Fact]
    public void ResolveBogotaTimeZone_FailsExplicitlyWhenNeitherIdentifierExists()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => OperationalTimeSnapshotProvider.ResolveBogotaTimeZone(_ => null));

        Assert.Contains("ACH_OPERATIONAL_TIMEZONE_NOT_FOUND", exception.Message, StringComparison.Ordinal);
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
