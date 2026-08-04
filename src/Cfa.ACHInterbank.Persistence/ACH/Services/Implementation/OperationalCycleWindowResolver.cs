using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class OperationalCycleWindowResolver : IOperationalCycleWindowResolver
{
    public OperationalCycleWindow Resolve(
        DateTime processingDate,
        TimeSpan startTime,
        TimeSpan endTime,
        string timeZoneId,
        DateTimeOffset currentInstant)
    {
        var zone = ResolveTimeZone(timeZoneId);
        var date = DateTime.SpecifyKind(processingDate.Date, DateTimeKind.Unspecified);
        var localStart = startTime <= endTime
            ? date.Add(startTime)
            : date.AddDays(-1).Add(startTime);
        var localEnd = date.Add(endTime);
        var startInstant = ConvertLocalToInstant(localStart, zone);
        var endInstant = ConvertLocalToInstant(localEnd, zone);
        var normalizedCurrent = currentInstant.ToUniversalTime();
        var localNow = TimeZoneInfo.ConvertTime(normalizedCurrent, zone).DateTime;
        var status = normalizedCurrent < startInstant
            ? OperationalCycleWindowStatus.Before
            : normalizedCurrent > endInstant
                ? OperationalCycleWindowStatus.After
                : OperationalCycleWindowStatus.Inside;

        return new OperationalCycleWindow(
            date,
            startTime,
            endTime,
            timeZoneId,
            localStart,
            localEnd,
            startInstant,
            endInstant,
            normalizedCurrent,
            localNow,
            status);
    }

    public DateTimeOffset ConvertLocalToInstant(DateTime localDateTime, string timeZoneId)
        => ConvertLocalToInstant(localDateTime, ResolveTimeZone(timeZoneId));

    private static DateTimeOffset ConvertLocalToInstant(DateTime localDateTime, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(unspecified))
        {
            throw new InvalidOperationException($"La hora local {unspecified:O} no existe en la zona '{zone.Id}'.");
        }

        if (zone.IsAmbiguousTime(unspecified))
        {
            throw new InvalidOperationException($"La hora local {unspecified:O} es ambigua en la zona '{zone.Id}'.");
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, zone), TimeSpan.Zero);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("La cámara no tiene configurada una zona horaria operativa.");
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId.Trim(), out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
    }
}
