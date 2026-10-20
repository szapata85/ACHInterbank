namespace Cfa.ACHInterbank.Tests.TestSupport;

internal sealed class FixedTimeProvider : TimeProvider
{
    public FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo localTimeZone)
    {
        UtcNow = utcNow;
        LocalTimeZone = localTimeZone;
    }

    public DateTimeOffset UtcNow { get; }

    public override DateTimeOffset GetUtcNow() => UtcNow;

    public override TimeZoneInfo LocalTimeZone { get; }
}

internal static class TestClock
{
    public static readonly DateTime OperationalDate = new(2026, 7, 24);
    public static readonly DateTimeOffset UtcNow = new(2026, 7, 24, 17, 0, 0, TimeSpan.Zero);
    public static readonly TimeZoneInfo BogotaTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "TestAmericaBogota",
        TimeSpan.FromHours(-5),
        "Test America/Bogota",
        "Test America/Bogota");

    public static FixedTimeProvider Create() => new(UtcNow, BogotaTimeZone);
}
