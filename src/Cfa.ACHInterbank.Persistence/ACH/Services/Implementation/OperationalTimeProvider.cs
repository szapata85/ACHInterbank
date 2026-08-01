namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Supplies the system UTC clock while fixing local operational calculations to CFA's configured timezone.
/// </summary>
internal sealed class OperationalTimeProvider : TimeProvider
{
    private readonly TimeProvider _utcClock;

    private OperationalTimeProvider(TimeProvider utcClock, TimeZoneInfo localTimeZone)
    {
        _utcClock = utcClock;
        LocalTimeZone = localTimeZone;
    }

    internal static OperationalTimeProvider SystemBogota { get; } = new(
        TimeProvider.System,
        OperationalTimeSnapshotProvider.ResolveBogotaTimeZone());

    public override TimeZoneInfo LocalTimeZone { get; }

    public override DateTimeOffset GetUtcNow() => _utcClock.GetUtcNow();
}
