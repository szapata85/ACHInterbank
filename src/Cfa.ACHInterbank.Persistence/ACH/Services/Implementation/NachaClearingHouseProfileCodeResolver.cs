using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class NachaClearingHouseProfileCodeResolver
{
    public static async Task<string> ResolveAsync(
        AchDbContext context,
        AchCycle cycle,
        CancellationToken ct)
    {
        var configuredProfileCode = await context.ClearingHouseConfigs
            .AsNoTracking()
            .Where(item => item.ClearingHouseId == cycle.ClearingHouseId && item.NachaProfileId != null)
            .Select(item => item.NachaProfile!.ClearingHouse.Code)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(configuredProfileCode))
        {
            return configuredProfileCode;
        }

        var operationalCode = cycle.ClearingHouse?.Code?.Trim();
        var operationalName = cycle.ClearingHouse?.Name?.Trim();
        var candidates = await context.CatClearingHouses
            .AsNoTracking()
            .Where(item => item.IsActive
                           && ((!string.IsNullOrEmpty(operationalCode) && item.Code == operationalCode)
                               || (!string.IsNullOrEmpty(operationalName) && item.Name == operationalName)))
            .Select(item => item.Code)
            .Distinct()
            .Take(2)
            .ToListAsync(ct);
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        throw new NachaGenerationException(
            candidates.Count == 0 ? "NACHA_CLEARING_HOUSE_PROFILE_NOT_CONFIGURED" : "NACHA_CLEARING_HOUSE_PROFILE_AMBIGUOUS",
            candidates.Count == 0
                ? "La cámara del ciclo no está asociada a un catálogo de perfiles NACHA-M."
                : "La cámara del ciclo coincide con más de un catálogo de perfiles NACHA-M.");
    }
}
