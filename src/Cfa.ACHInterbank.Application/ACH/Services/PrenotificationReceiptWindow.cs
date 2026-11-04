namespace Cfa.ACHInterbank.Application.ACH.Services;

/// <summary>
/// APN-I11: translates one chamber's local operational-date range into UTC
/// intervals for actual receipt timestamps. This is a conservative window for
/// negative/incomplete evidence, NOT a certification that all files arrived.
/// </summary>
public readonly record struct PrenotificationReceiptWindow(
    DateTime OperationalStartDate,
    DateTime OperationalEndExclusive,
    DateTime UtcOperationalStartInclusive,
    DateTime UtcUnclassifiedStartInclusive,
    DateTime UtcEndExclusive)
{
    public static PrenotificationReceiptWindow Create(
        DateTime sourceOperationalDate,
        DateTimeOffset responseDeadline,
        string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new InvalidOperationException("APN_I11_MISSING_OPERATIONAL_TIME_ZONE");

        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(
            timeZoneId.Trim(), out var windowsId))
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }

        var firstLocalDate = sourceOperationalDate.Date;
        var lastLocalDate = TimeZoneInfo.ConvertTime(responseDeadline, zone).Date;
        if (lastLocalDate < firstLocalDate)
            throw new InvalidOperationException("APN_I11_DEADLINE_BEFORE_SOURCE_OPERATIONAL_DATE");

        // End is exclusive and based on LOCAL midnight, not DateTimeOffset.UtcDateTime.Date.
        // ACHCOL/CENIT may release valid same-day files after 00:00 UTC on the next day.
        var endLocalExclusive = lastLocalDate.AddDays(1);
        return new PrenotificationReceiptWindow(
            firstLocalDate,
            endLocalExclusive,
            AtUtc(firstLocalDate, zone),
            AtUtc(firstLocalDate.AddDays(-1), zone),
            AtUtc(endLocalExclusive, zone));
    }

    private static DateTime AtUtc(DateTime date, TimeZoneInfo zone)
    {
        var localMidnight = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        if (zone.IsAmbiguousTime(localMidnight) || zone.IsInvalidTime(localMidnight))
            throw new InvalidOperationException("APN_I11_AMBIGUOUS_OPERATIONAL_MIDNIGHT");
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, zone);
    }
}
