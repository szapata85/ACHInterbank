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
        if (_context.ClearingHouseCycleConfigs.Any())
        {
            return;
        }

        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        _context.ClearingHouseCycleConfigs.AddRange(BuildAchColombiaSeed());
        _context.ClearingHouseCycleConfigs.AddRange(BuildCenitSeed());

        await _context.SaveChangesAsync();
        _context.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    private static IEnumerable<ClearingHouseCycleConfig> BuildAchColombiaSeed()
    {
        // Escenarios cubiertos: vigentes, futuros, inactivos, cambio de cantidad de ciclos y cambio de horarios.
        return new[]
        {
            // Históricos inactivos (cantidad previa de ciclos).
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 1", StartTime = new TimeSpan(19, 01, 0), EndTime = new TimeSpan(8, 30, 0), CutoffTime = new TimeSpan(8, 30, 0), IsActive = false, EffectiveFrom = UtcDate(2024, 1, 1), EffectiveTo = UtcDate(2024, 12, 31) },
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 2", StartTime = new TimeSpan(8, 31, 0), EndTime = new TimeSpan(11, 00, 0), CutoffTime = new TimeSpan(11, 00, 0), IsActive = false, EffectiveFrom = UtcDate(2024, 1, 1), EffectiveTo = UtcDate(2024, 12, 31) },
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 3", StartTime = new TimeSpan(11, 01, 0), EndTime = new TimeSpan(15, 00, 0), CutoffTime = new TimeSpan(15, 00, 0), IsActive = false, EffectiveFrom = UtcDate(2024, 1, 1), EffectiveTo = UtcDate(2024, 12, 31) },

            // Vigentes (cantidad incrementada y horarios ajustados).
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 1", StartTime = new TimeSpan(19, 01, 0), EndTime = new TimeSpan(8, 15, 0), CutoffTime = new TimeSpan(8, 15, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 2", StartTime = new TimeSpan(8, 16, 0), EndTime = new TimeSpan(10, 45, 0), CutoffTime = new TimeSpan(10, 45, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 3", StartTime = new TimeSpan(10, 46, 0), EndTime = new TimeSpan(13, 15, 0), CutoffTime = new TimeSpan(13, 15, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 4", StartTime = new TimeSpan(13, 16, 0), EndTime = new TimeSpan(15, 30, 0), CutoffTime = new TimeSpan(15, 30, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 5", StartTime = new TimeSpan(15, 31, 0), EndTime = new TimeSpan(18, 0, 0), CutoffTime = new TimeSpan(18, 0, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },

            // Futura (nueva ventana/horarios).
            new ClearingHouseCycleConfig { ClearingHouseId = 1, CycleName = "Ciclo 6", StartTime = new TimeSpan(18, 1, 0), EndTime = new TimeSpan(19, 0, 0), CutoffTime = new TimeSpan(19, 0, 0), IsActive = true, EffectiveFrom = UtcDate(2027, 1, 1) }
        };
    }

    private static IEnumerable<ClearingHouseCycleConfig> BuildCenitSeed()
    {
        return new[]
        {
            // CENIT obligatorio: cinco ciclos diarios activos.
            new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 1", StartTime = new TimeSpan(19, 0, 0), EndTime = new TimeSpan(8, 0, 0), CutoffTime = new TimeSpan(8, 0, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 2", StartTime = new TimeSpan(8, 1, 0), EndTime = new TimeSpan(10, 30, 0), CutoffTime = new TimeSpan(10, 30, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 3", StartTime = new TimeSpan(10, 31, 0), EndTime = new TimeSpan(13, 0, 0), CutoffTime = new TimeSpan(13, 0, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 4", StartTime = new TimeSpan(13, 1, 0), EndTime = new TimeSpan(15, 30, 0), CutoffTime = new TimeSpan(15, 30, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) },
            new ClearingHouseCycleConfig { ClearingHouseId = 2, CycleName = "Ciclo 5", StartTime = new TimeSpan(15, 31, 0), EndTime = new TimeSpan(18, 0, 0), CutoffTime = new TimeSpan(18, 0, 0), IsActive = true, EffectiveFrom = UtcDate(2025, 1, 1) }
        };
    }

    private static DateTime UtcDate(int year, int month, int day)
        => DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
}
