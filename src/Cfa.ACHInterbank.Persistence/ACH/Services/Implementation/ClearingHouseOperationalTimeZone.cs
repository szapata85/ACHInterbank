using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class ClearingHouseOperationalTimeZone
{
    public static string Resolve(AchCycle cycle)
    {
        var configured = cycle.ClearingHouse?.ClearingHouseConfig?.TimeZoneId;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        if (string.Equals(cycle.ClearingHouse?.Code, RegulatoryCycleScheduleCatalog.AchColombiaCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(cycle.ClearingHouse?.Code, RegulatoryCycleScheduleCatalog.CenitCode, StringComparison.OrdinalIgnoreCase))
        {
            return RegulatoryCycleScheduleCatalog.BogotaTimeZoneId;
        }

        throw new InvalidOperationException("La cámara del ciclo no tiene una zona horaria operativa configurada.");
    }
}

