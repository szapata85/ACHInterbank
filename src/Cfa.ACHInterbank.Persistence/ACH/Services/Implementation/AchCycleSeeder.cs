using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchCycleSeeder : IAchCycleSeeder
{
    private readonly AchDbContext _context;
    private readonly IAchCycleScheduler _cycleScheduler;

    public AchCycleSeeder(AchDbContext context, IAchCycleScheduler cycleScheduler)
    {
        _context = context;
        _cycleScheduler = cycleScheduler;
    }

    public async Task SeedCyclesIfNotExistsAsync(int clearingHouseId, int year)
    {
        var exists = await _context.AchCycles.AnyAsync(c => c.ClearingHouseId == clearingHouseId && c.ProcessingDate.Year == year);
        if (exists) return;

        var clearingHouse = await _context.ClearingHouses.FindAsync(clearingHouseId);
        if (clearingHouse == null) throw new Exception("Clearing house not found");

        var templates = GetCycleTemplatesForClearingHouse(clearingHouse.Code);
        var startDate = new DateTime(year, 1, 2); // Ajustable

        var cycles = new List<AchCycle>();

        foreach (var template in templates)
        {
            var processingDate = _cycleScheduler.GetNextValidProcessingDate(startDate);

            cycles.Add(new AchCycle
            {
                CycleName = template.CycleName,
                CutoffTime = template.CutoffTime,
                RescheduleOnHoliday = template.RescheduleOnHoliday,
                ProcessingDate = processingDate,
                ClearingHouseId = clearingHouseId
            });

            // Avanzar la fecha base solo si quieres repartir los ciclos en días distintos
            startDate = processingDate.AddDays(1);
        }

        _context.AchCycles.AddRange(cycles);
        await _context.SaveChangesAsync();
    }

    private List<AchCycleTemplate> GetCycleTemplatesForClearingHouse(string code)
    {
        return code switch
        {
            "ACHCOL" => new List<AchCycleTemplate>
            {
                new() { CycleName = "ACH-AM-1", CutoffTime = new TimeSpan(8, 0, 0), RescheduleOnHoliday = true },
                new() { CycleName = "ACH-AM-2", CutoffTime = new TimeSpan(10, 0, 0), RescheduleOnHoliday = false },
                new() { CycleName = "ACH-PM-1", CutoffTime = new TimeSpan(13, 0, 0), RescheduleOnHoliday = true },
                new() { CycleName = "ACH-PM-2", CutoffTime = new TimeSpan(15, 30, 0), RescheduleOnHoliday = false },
                new() { CycleName = "ACH-END", CutoffTime = new TimeSpan(17, 45, 0), RescheduleOnHoliday = true }
            },
            "CENITCO" => new List<AchCycleTemplate>
            {
                new() { CycleName = "CENIT-AM-1", CutoffTime = new TimeSpan(7, 30, 0), RescheduleOnHoliday = true },
                new() { CycleName = "CENIT-AM-2", CutoffTime = new TimeSpan(10, 30, 0), RescheduleOnHoliday = false },
                new() { CycleName = "CENIT-PM-1", CutoffTime = new TimeSpan(14, 0, 0), RescheduleOnHoliday = true },
                new() { CycleName = "CENIT-PM-2", CutoffTime = new TimeSpan(16, 30, 0), RescheduleOnHoliday = false },
                new() { CycleName = "CENIT-END", CutoffTime = new TimeSpan(18, 0, 0), RescheduleOnHoliday = true }
            },
            _ => throw new NotSupportedException($"No templates for clearing house code: {code}")
        };
    }
}

