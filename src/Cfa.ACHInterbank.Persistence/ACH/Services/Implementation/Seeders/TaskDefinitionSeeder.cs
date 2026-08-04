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
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 30, 0, TimeSpan.Zero)
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
                StartAt = new DateTimeOffset(2025, 1, 1, 2, 0, 0, TimeSpan.Zero)
            });
        }

        var bankHolidaySeedTask = _context.TaskDefinitions.FirstOrDefault(t => t.Code == "SeedBankHolidays");
        if (bankHolidaySeedTask is null)
        {
            bankHolidaySeedTask = new TaskDefinition
            {
                Code = "SeedBankHolidays",
                Name = "Sembrar festivos (Ley Emiliani)",
                PeriodicityType = PeriodicityTypeEnum.Cron,
                CronExpression = "0 10 0 1 1 ? *",
                TimeZoneId = "America/Bogota",
                CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar,
                ManualExecutionEnabled = true,
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 10, 0, TimeSpan.Zero)
            };
            _context.TaskDefinitions.Add(bankHolidaySeedTask);
        }
        else if (!bankHolidaySeedTask.ManualExecutionEnabled)
        {
            bankHolidaySeedTask.ManualExecutionEnabled = true;
            _context.Entry(bankHolidaySeedTask)
                .Property(t => t.ManualExecutionEnabled)
                .IsModified = true;
        }


        if (!_context.TaskDefinitions.Any(t => t.Code == "AchTacitAcceptanceJob"))
        {
            _context.TaskDefinitions.Add(new TaskDefinition
            {
                Code = "AchTacitAcceptanceJob",
                Name = "Aplicar aceptación tácita ACH",
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
                N = 30,
                TimeZoneId = "America/Bogota",
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        }

        if (!_context.TaskDefinitions.Any(t => t.Code == "AchContrapartidasByCycle"))
        {
            _context.TaskDefinitions.Add(new TaskDefinition
            {
                Code = "AchContrapartidasByCycle",
                Name = "Enviar contrapartidas por ciclo y cámara",
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
                N = 5,
                TimeZoneId = "America/Bogota",
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        }

        if (!_context.TaskDefinitions.Any(t => t.Code == "IncomingNachaPostProcessing"))
        {
            _context.TaskDefinitions.Add(new TaskDefinition
            {
                Code = "IncomingNachaPostProcessing",
                Name = "Procesamiento posterior NACHA entrante a Proc_Transacciones",
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
                N = 3,
                TimeZoneId = "America/Bogota",
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        }
        if (!_context.TaskDefinitions.Any(t => t.Code == "ach-response-reprocess-dispatcher"))
        {
            _context.TaskDefinitions.Add(new TaskDefinition
            {
                Code = "ach-response-reprocess-dispatcher",
                Name = "Dispatcher de reprocesos de respuestas ACH",
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
                N = 1,
                TimeZoneId = "America/Bogota",
                ManualExecutionEnabled = true,
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}
