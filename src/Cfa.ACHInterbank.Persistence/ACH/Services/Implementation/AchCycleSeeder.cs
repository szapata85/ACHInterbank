using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchCycleSeeder(
    AchDbContext context,
    ICycleNumberResolver? cycleNumberResolver = null) : IAchCycleSeeder
{
    public async Task SeedCyclesIfNotExistsAsync(int clearingHouseId, int year)
    {
        var clearingHouseCode = await context.ClearingHouses
            .Where(house => house.Id == clearingHouseId)
            .Select(house => house.Code)
            .SingleOrDefaultAsync();

        if (!string.Equals(clearingHouseCode, RegulatoryCycleScheduleCatalog.AchColombiaCode, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(clearingHouseCode, RegulatoryCycleScheduleCatalog.CenitCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await RegulatoryCycleSeedRepair.ApplyAsync(
            context,
            clearingHouseId,
            clearingHouseCode!,
            year,
            cycleNumberResolver ?? new CycleNumberResolver());
    }
}
