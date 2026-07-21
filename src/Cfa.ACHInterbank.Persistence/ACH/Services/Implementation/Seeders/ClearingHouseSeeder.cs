using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class ClearingHouseSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public ClearingHouseSeeder(AchDbContext context)
    {
        _context = context;
    }

    int IDbSeeder.Order => 1;

    public async Task SeedAsync()
    {
        var clearingHouseConfigId = await _context.ClearingHouseConfigs
            .OrderBy(config => config.Id)
            .Select(config => (int?)config.Id)
            .FirstOrDefaultAsync();

        if (!clearingHouseConfigId.HasValue)
        {
            throw new InvalidOperationException(
                "Seeder ClearingHouseSeeder: falta la configuración de cámara requerida para crear ClearingHouses.");
        }

        var existingCodes = await _context.ClearingHouses
            .Select(house => house.Code)
            .ToListAsync();

        var missingClearingHouses = new[]
        {
            new ClearingHouse { Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "000101006", ClearingHouseId = clearingHouseConfigId.Value },
            new ClearingHouse { Name = "CENIT", Code = "CENIT", OriginCode = "011111111", ClearingHouseId = clearingHouseConfigId.Value }
        }
        .Where(house => !existingCodes.Contains(house.Code, StringComparer.OrdinalIgnoreCase))
        .ToList();

        if (missingClearingHouses.Count > 0)
        {
            _context.ClearingHouses.AddRange(missingClearingHouses);
            await _context.SaveChangesAsync();
        }
    }
}
