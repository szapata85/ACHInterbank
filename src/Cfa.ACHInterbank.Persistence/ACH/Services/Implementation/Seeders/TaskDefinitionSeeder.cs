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
        if (!_context.TaskDefinitions.Any(t => t.Code == "AchCycleSeeder"))
        {
            _context.TaskDefinitions.Add(new TaskDefinition
            {
                Code = "AchCycleSeeder",
                Name = "Actualizar ciclos de compensación",
                Description = "Verifica y actualiza la programación de los ciclos de ACH Colombia y CENIT.",
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
                Name = "Preparar ciclos operativos",
                Description = "Crea los ciclos operativos diarios a partir de la configuración vigente de cada cámara.",
                ManualExecutionEnabled = true,
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
                Name = "Actualizar días festivos",
                Description = "Mantiene actualizado el calendario de días no laborables utilizado por los procesos ACH.",
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
                Name = "Despachar débitos originados por CFA",
                Description = "Evalúa y envía mediante Proc_Contrapartidas los movimientos débito elegibles del ciclo vigente.",
                ManualExecutionEnabled = true,
                RetryOnFailure = false,
                ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
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
                Name = "Procesar créditos recibidos",
                Description = "Evalúa entradas NACHA-M y envía mediante Proc_Transacciones únicamente los créditos elegibles.",
                ManualExecutionEnabled = true,
                RetryOnFailure = false,
                ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
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
                Name = "Procesar respuestas diferenciales",
                Description = "Procesa de forma idempotente solicitudes autorizadas de reproceso de respuestas ACH, sin movimientos monetarios.",
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
                N = 1,
                TimeZoneId = "America/Bogota",
                ManualExecutionEnabled = true,
                StartAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        }
        await _context.SaveChangesAsync();

        var metadata = new Dictionary<string, (string Name, string Description, bool? Manual, bool Monetary)>(StringComparer.OrdinalIgnoreCase)
        {
            ["AchCycleSeeder"] = ("Actualizar ciclos de compensación", "Verifica y actualiza la programación de los ciclos de ACH Colombia y CENIT.", false, false),
            ["AchCycleScheduler"] = ("Preparar ciclos operativos", "Crea los ciclos operativos diarios a partir de la configuración vigente de cada cámara.", true, false),
            ["SeedBankHolidays"] = ("Actualizar días festivos", "Mantiene actualizado el calendario de días no laborables utilizado por los procesos ACH.", true, false),
            ["AchTacitAcceptanceJob"] = ("Aplicar aceptación tácita", "Evalúa las prenotificaciones cuyo plazo finalizó y aplica las reglas operativas vigentes.", false, false),
            ["AchContrapartidasByCycle"] = ("Despachar débitos originados por CFA", "Evalúa y envía los movimientos débito elegibles del ciclo vigente.", true, true),
            ["IncomingNachaPostProcessing"] = ("Procesar créditos recibidos", "Evalúa entradas NACHA-M y envía únicamente los movimientos crédito elegibles.", true, true),
            ["ach-response-reprocess-dispatcher"] = ("Procesar respuestas diferenciales", "Procesa de forma idempotente solicitudes autorizadas de reproceso de respuestas ACH, sin movimientos monetarios.", true, false)
        };
        var persisted = _context.TaskDefinitions.Where(x => metadata.Keys.Contains(x.Code)).ToList();
        foreach (var task in persisted)
        {
            var values = metadata[task.Code];
            task.Name = values.Name;
            task.Description = values.Description;
            if (values.Manual.HasValue) task.ManualExecutionEnabled = values.Manual.Value;
            if (values.Monetary)
            {
                task.RetryOnFailure = false;
                task.ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning;
            }
        }

        await _context.SaveChangesAsync();
    }
}
