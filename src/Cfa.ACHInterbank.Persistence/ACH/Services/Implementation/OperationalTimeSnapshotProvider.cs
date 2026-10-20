using System.Collections.Concurrent;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class OperationalTimeSnapshotProvider : IOperationalTimeSnapshotProvider
{
    internal const string IanaTimeZoneId = "America/Bogota";
    internal const string WindowsTimeZoneId = "SA Pacific Standard Time";

    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _bogotaTimeZone;
    private readonly ConcurrentDictionary<string, OperationalTimeSnapshot> _snapshots = new(StringComparer.Ordinal);

    public OperationalTimeSnapshotProvider(TimeProvider? timeProvider = null)
        : this(timeProvider ?? TimeProvider.System, ResolveBogotaTimeZone())
    {
    }

    internal OperationalTimeSnapshotProvider(TimeProvider timeProvider, TimeZoneInfo bogotaTimeZone)
    {
        _timeProvider = timeProvider;
        _bogotaTimeZone = bogotaTimeZone;
    }

    public OperationalTimeSnapshot CaptureNow()
    {
        var captured = _timeProvider.GetUtcNow();
        var bogota = TimeZoneInfo.ConvertTime(captured, _bogotaTimeZone);
        var local = DateTime.SpecifyKind(bogota.DateTime, DateTimeKind.Unspecified);
        return new OperationalTimeSnapshot(
            captured.UtcDateTime,
            local,
            DateOnly.FromDateTime(local),
            _bogotaTimeZone.Id);
    }

    public OperationalTimeSnapshot GetOrCreate(
        string operationKey,
        DateOnly operationalDate,
        TimeOnly preferredFileCreationTime)
    {
        if (string.IsNullOrWhiteSpace(operationKey))
        {
            throw new ArgumentException("La clave de la operación temporal es obligatoria.", nameof(operationKey));
        }

        return _snapshots.GetOrAdd(operationKey, _ =>
        {
            var captured = _timeProvider.GetUtcNow();
            var local = DateTime.SpecifyKind(
                operationalDate.ToDateTime(preferredFileCreationTime),
                DateTimeKind.Unspecified);
            return new OperationalTimeSnapshot(
                captured.UtcDateTime,
                local,
                operationalDate,
                _bogotaTimeZone.Id);
        });
    }

    internal static TimeZoneInfo ResolveBogotaTimeZone(Func<string, TimeZoneInfo?>? resolver = null)
    {
        resolver ??= id =>
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        };

        return resolver(IanaTimeZoneId)
            ?? resolver(WindowsTimeZoneId)
            ?? throw new InvalidOperationException(
                $"ACH_OPERATIONAL_TIMEZONE_NOT_FOUND: no se pudo resolver {IanaTimeZoneId} ni su alias controlado de Windows.");
    }
}
