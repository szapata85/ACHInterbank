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

    int IDbSeeder.Order => 2;

    public async Task SeedAsync()
    {
        if (!_context.ClearingHouseCycleConfigs.Any())
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            _context.ClearingHouseCycleConfigs.AddRange(
                //ACH Colombia
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 1", StartTime = new TimeSpan(19, 01, 0), EndTime = new TimeSpan(8, 30, 0), CutoffTime = new TimeSpan(8, 30, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 2", StartTime = new TimeSpan(8, 31, 0), EndTime = new TimeSpan(11, 00, 0), CutoffTime = new TimeSpan(11, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 3", StartTime = new TimeSpan(11, 01, 0), EndTime = new TimeSpan(14, 00, 0), CutoffTime = new TimeSpan(14, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 4", StartTime = new TimeSpan(14, 01, 0), EndTime = new TimeSpan(16, 00, 0), CutoffTime = new TimeSpan(16, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 5", StartTime = new TimeSpan(16, 01, 0), EndTime = new TimeSpan(18, 00, 0), CutoffTime = new TimeSpan(18, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },

                //CENIT
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 1", StartTime = new TimeSpan(19, 01, 0), EndTime = new TimeSpan(8, 30, 0), CutoffTime = new TimeSpan(8, 30, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 2", StartTime = new TimeSpan(8, 31, 0), EndTime = new TimeSpan(11, 00, 0), CutoffTime = new TimeSpan(11, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 3", StartTime = new TimeSpan(11, 01, 0), EndTime = new TimeSpan(14, 00, 0), CutoffTime = new TimeSpan(14, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 4", StartTime = new TimeSpan(14, 01, 0), EndTime = new TimeSpan(16, 00, 0), CutoffTime = new TimeSpan(16, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) },
                new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 5", StartTime = new TimeSpan(16, 01, 0), EndTime = new TimeSpan(18, 00, 0), CutoffTime = new TimeSpan(18, 00, 0), IsActive = true, EffectiveFrom = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc) }
            );

            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
