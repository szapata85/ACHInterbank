using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Services;

/// <summary>
/// Interprets a receiver notification timestamp as a Bogotá banking date for
/// the CENIT R23 next-business-day deadline. Persisted DateTimeKind.Unspecified
/// values are already operational/local dates; UTC and Local instants must be
/// converted to Bogotá before discarding the time of day.
/// </summary>
public static class CenitR23BankingDate
{
    public static DateOnly FromNotificationTimestamp(DateTime timestamp)
    {
        if (timestamp.Kind == DateTimeKind.Unspecified)
        {
            return DateOnly.FromDateTime(timestamp);
        }

        var bogota = TimeZoneInfo.FindSystemTimeZoneById(RegulatoryCycleScheduleCatalog.BogotaTimeZoneId);
        var utc = timestamp.Kind == DateTimeKind.Utc ? timestamp : timestamp.ToUniversalTime();
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, bogota));
    }
}
