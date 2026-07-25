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
    public static readonly TimeZoneInfo BogotaTimeZone = ResolveBogotaTimeZone();

    public static FixedTimeProvider Create() => new(UtcNow, BogotaTimeZone);

    private static TimeZoneInfo ResolveBogotaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }
}
