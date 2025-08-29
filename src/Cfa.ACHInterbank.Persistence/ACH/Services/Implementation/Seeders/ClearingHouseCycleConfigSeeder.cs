using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class ClearingHouseCycleConfigSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public ClearingHouseCycleConfigSeeder(AchDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (!_context.ClearingHouseCycleConfigs.Any())
        {
            _context.ClearingHouseCycleConfigs.AddRange(
                //ACH Colombia
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 1", CutoffTime = new TimeSpan(10, 30, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 2", CutoffTime = new TimeSpan(13, 00, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 3", CutoffTime = new TimeSpan(15, 30, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 4", CutoffTime = new TimeSpan(17, 30, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 5", CutoffTime = new TimeSpan(19, 00, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },

                //CENIT
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 1", CutoffTime = new TimeSpan(9, 30, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 2", CutoffTime = new TimeSpan(12, 00, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 3", CutoffTime = new TimeSpan(15, 00, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 4", CutoffTime = new TimeSpan(17, 15, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 5", CutoffTime = new TimeSpan(19, 15, 0), IsActive = true, EffectiveFrom = new DateTime(2025, 1, 1) }
            );

            await _context.SaveChangesAsync();
        }
    }
}
