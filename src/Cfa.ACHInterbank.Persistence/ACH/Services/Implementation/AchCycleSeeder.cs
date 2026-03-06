using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchCycleSeeder : IAchCycleSeeder
{
    private readonly AchDbContext _context;

    public AchCycleSeeder(AchDbContext context)
    {
        _context = context;
    }

    public async Task SeedCyclesIfNotExistsAsync(int clearingHouseId, int year)
    {
        bool exists = await _context.ClearingHouseCycleConfigs
            .AnyAsync(c => c.ClearingHouseId == clearingHouseId
                        && c.EffectiveFrom.Year <= year
                        && (c.EffectiveTo == null || c.EffectiveTo.Value.Year >= year));

        if (exists) return;

        var configs = new List<ClearingHouseCycleConfig>();

        if (clearingHouseId == 1) // ACH Colombia
        {
            configs.AddRange(new[]
            {
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 1", StartTime = new TimeSpan(19,01,0), EndTime = new TimeSpan(8,30,0), CutoffTime = new TimeSpan(8,30,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 2", StartTime = new TimeSpan(8,31,0), EndTime = new TimeSpan(11,00,0), CutoffTime = new TimeSpan(11,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 3", StartTime = new TimeSpan(11,01,0), EndTime = new TimeSpan(14,00,0), CutoffTime = new TimeSpan(14,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 4", StartTime = new TimeSpan(14,01,0), EndTime = new TimeSpan(16,00,0), CutoffTime = new TimeSpan(16,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 5", StartTime = new TimeSpan(16,01,0), EndTime = new TimeSpan(18,00,0), CutoffTime = new TimeSpan(18,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) }
            });
        }
        else if (clearingHouseId == 2) // CENIT
        {
            configs.AddRange(new[]
            {
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 1", StartTime = new TimeSpan(19,01,0), EndTime = new TimeSpan(8,30,0), CutoffTime = new TimeSpan(8,30,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 2", StartTime = new TimeSpan(8,31,0), EndTime = new TimeSpan(11,00,0), CutoffTime = new TimeSpan(11,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 3", StartTime = new TimeSpan(11,01,0), EndTime = new TimeSpan(14,00,0), CutoffTime = new TimeSpan(14,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 4", StartTime = new TimeSpan(14,01,0), EndTime = new TimeSpan(16,00,0), CutoffTime = new TimeSpan(16,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 5", StartTime = new TimeSpan(16,01,0), EndTime = new TimeSpan(18,00,0), CutoffTime = new TimeSpan(18,00,0), EffectiveFrom = DateTime.SpecifyKind(new DateTime(year,1,1), DateTimeKind.Utc) }
            });
        }

        if (configs.Any())
        {
            await _context.ClearingHouseCycleConfigs.AddRangeAsync(configs);
            await _context.SaveChangesAsync();
        }
    }
}
