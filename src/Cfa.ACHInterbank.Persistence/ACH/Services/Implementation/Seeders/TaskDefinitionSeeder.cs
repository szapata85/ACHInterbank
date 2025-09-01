using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class TaskDefinitionSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public TaskDefinitionSeeder(AchDbContext context)
    {
        _context = context;
    }

    int IDbSeeder.Order => 4;

    public async Task SeedAsync()
    {
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        if (!_context.TaskDefinitions.Any(t => t.Code == "AchCycleSeeder"))
        {
            _context.TaskDefinitions.Add(new TaskDefinition
            {
                Code = "AchCycleSeeder",
                Name = "Seed ciclos ACH y CENIT",
                PeriodicityType = PeriodicityTypeEnum.Cron,
                CronExpression = "0 30 0 1 1 ? *",
                TimeZoneId = "America/Bogota",
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 30, 0, new TimeSpan(-5, 0, 0))
            });
        }

        if (!_context.TaskDefinitions.Any(t => t.Code == "AchCycleScheduler"))
        {
            _context.TaskDefinitions.Add(new TaskDefinition
            {
                Code = "AchCycleScheduler",
                Name = "Generar ciclos diarios",
                PeriodicityType = PeriodicityTypeEnum.DailyAtTime,
                TimeOfDayTicks = new TimeOnly(2, 0).Ticks,
                TimeZoneId = "America/Bogota",
                StartAt = new DateTimeOffset(2025, 1, 1, 2, 0, 0, new TimeSpan(-5, 0, 0))
            });
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}

