using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class ClearingHouseCycleConfigSeeder(
    AchDbContext context,
    ICycleNumberResolver? cycleNumberResolver = null) : IDbSeeder
{
    int IDbSeeder.Order => 2;

    public async Task SeedAsync()
    {
        var clearingHouses = await context.ClearingHouses
            .Where(house => house.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode
                || house.Code == RegulatoryCycleScheduleCatalog.CenitCode)
            .Select(house => new { house.Id, house.Code })
            .ToListAsync();

        foreach (var code in new[] { RegulatoryCycleScheduleCatalog.AchColombiaCode, RegulatoryCycleScheduleCatalog.CenitCode })
        {
            var house = clearingHouses.SingleOrDefault(item => item.Code == code)
                ?? throw new InvalidOperationException($"Seeder ClearingHouseCycleConfigSeeder: falta la cámara '{code}'.");

            await RegulatoryCycleSeedRepair.ApplyAsync(context, house.Id, house.Code, 2025, cycleNumberResolver);
        }
    }
}
