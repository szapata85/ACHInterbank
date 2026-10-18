using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public sealed class AchColombiaManagedMftOutboundHandler(
    AchDbContext context,
    IAchColombiaManagedMftExecutionService service,
    IOperationalTimeSnapshotProvider operationalTimeProvider,
    IOperationalCycleWindowResolver windowResolver) : ISchedulerContextAwareTaskHandler
{
    private const string ClearingHouseCode = "ACHCOL";

    public string Code => "AchColombiaManagedMftOutbound";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var now = operationalTimeProvider.CaptureNow();
        var currentInstant = AsUtcInstant(now.CapturedAtUtc);
        var operationalDate = now.OperationalDate.ToDateTime(TimeOnly.MinValue);
        // El ciclo nocturno puede abrir en D-1 para una ProcessingDate D; por eso
        // se considera también la siguiente fecha de proceso y se filtra por ventana real.
        var candidateEndExclusive = operationalDate.AddDays(2);

        var candidates = await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse)
                .ThenInclude(x => x!.ClearingHouseConfig)
            .Where(x => x.ClearingHouse!.Code == ClearingHouseCode
                && x.ProcessingDate >= operationalDate
                && x.ProcessingDate < candidateEndExclusive)
            .OrderBy(x => x.ProcessingDate)
            .ThenBy(x => x.CutoffTime)
            .ToListAsync(cancellationToken);

        var activeCycles = candidates
            .Where(cycle => ResolveSendingWindow(cycle, currentInstant).Status == OperationalCycleWindowStatus.Inside)
            .ToArray();

        if (activeCycles.Length == 0)
        {
            return "Intercambio saliente ACH Colombia: ciclo_activo=ninguno, enviados=0, fallidos=0.";
        }

        if (activeCycles.Length > 1)
        {
            throw new InvalidOperationException("ACHCOL_MFT_MULTIPLE_ACTIVE_OUTBOUND_CYCLES");
        }

        var activeCycle = activeCycles[0];
        var result = await service.ExecuteOutboundAsync(
            activeCycle.Id,
            AchManagedFileExecutionOrigin.Automatic,
            $"task:{Code}",
            $"task:{Code}:{activeCycle.Id}",
            cancellationToken);

        return $"Intercambio saliente ACH Colombia: ciclo_activo={activeCycle.Id}, enviados={result.Succeeded}, fallidos={result.Failed}.";
    }

    public async Task<string> ExecuteAsync(
        TaskDefinition task,
        SchedulerTaskExecutionContext schedulerContext,
        CancellationToken cancellationToken)
    {
        if (task.Paused || task.Status != TaskStatusEnum.Enabled)
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=tarea_no_habilitada, enviados=0, fallidos=0.";
        }

        if (!TryResolveOperationalTriggerContext(
                schedulerContext,
                out var operationalScheduleId,
                out var clearingHouseId,
                out var cycleConfigId))
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=trigger_operacional_requerido, enviados=0, fallidos=0.";
        }

        var schedule = await context.AchColombiaCycleOperationalSchedules.AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.ClearingHouseCycleConfig)
            .SingleOrDefaultAsync(x => x.Id == operationalScheduleId, cancellationToken);
        if (schedule is null
            || schedule.ClearingHouseId != clearingHouseId
            || schedule.ClearingHouseCycleConfigId != cycleConfigId
            || !string.Equals(schedule.ClearingHouse.Code, ClearingHouseCode, StringComparison.OrdinalIgnoreCase))
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=programacion_operacional_no_vigente, enviados=0, fallidos=0.";
        }

        if (!schedule.IsActive || !schedule.AutomaticOutboundEnabled || !schedule.ClearingHouseCycleConfig.IsActive)
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=programacion_operacional_inactiva, enviados=0, fallidos=0.";
        }

        if (!schedule.AutomaticOutboundStartTime.HasValue)
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=hora_operacional_no_definida, enviados=0, fallidos=0.";
        }

        if (schedulerContext.JobData is not null
            && schedulerContext.JobData.TryGetValue("ScheduleFingerprint", out var triggerFingerprint)
            && !string.IsNullOrWhiteSpace(triggerFingerprint)
            && !string.Equals(triggerFingerprint, BuildOperationalScheduleFingerprint(schedule), StringComparison.Ordinal))
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=trigger_operacional_obsoleto, enviados=0, fallidos=0.";
        }

        var now = operationalTimeProvider.CaptureNow();
        var currentInstant = AsUtcInstant(now.CapturedAtUtc);
        var scheduledInstant = schedulerContext.ScheduledFireTimeUtc ?? currentInstant;
        var localDate = ResolveLocalDate(scheduledInstant, schedule.EffectiveTimeZoneId);
        if (schedule.ClearingHouseCycleConfig.EffectiveFrom.Date > localDate
            || (schedule.ClearingHouseCycleConfig.EffectiveTo.HasValue
                && schedule.ClearingHouseCycleConfig.EffectiveTo.Value.Date < localDate))
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=configuracion_regulatoria_fuera_de_vigencia, enviados=0, fallidos=0.";
        }

        var operationalStart = windowResolver.ConvertLocalToInstant(
            localDate.Add(schedule.AutomaticOutboundStartTime.Value),
            schedule.EffectiveTimeZoneId);
        if (currentInstant < operationalStart)
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=hora_operacional_futura, enviados=0, fallidos=0.";
        }

        if (schedule.AutomaticOutboundLatestStartTime.HasValue)
        {
            var latestStart = windowResolver.ConvertLocalToInstant(
                localDate.Add(schedule.AutomaticOutboundLatestStartTime.Value),
                schedule.EffectiveTimeZoneId);
            if (currentInstant > latestStart)
            {
                var reason = schedulerContext.MisfireDetected
                    ? "misfire_despues_latest_start"
                    : "despues_latest_start";
                return $"Intercambio saliente ACH Colombia: ejecución omitida, motivo={reason}, enviados=0, fallidos=0.";
            }
        }

        var mftConfiguration = await context.AchManagedFileTransferConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClearingHouseId == schedule.ClearingHouseId, cancellationToken);
        if (mftConfiguration is null || !mftConfiguration.ProfileEnabled || !mftConfiguration.AutomaticOutboundEnabled)
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=perfil_mft_o_outbound_automatico_inactivo, enviados=0, fallidos=0.";
        }

        var cycle = await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse)
                .ThenInclude(x => x!.ClearingHouseConfig)
            .SingleOrDefaultAsync(x => x.ClearingHouseId == schedule.ClearingHouseId
                && x.ClearingHouseCycleConfigId == schedule.ClearingHouseCycleConfigId
                && x.ProcessingDate.Date == localDate, cancellationToken);
        if (cycle is null)
        {
            return "Intercambio saliente ACH Colombia: ejecución omitida, motivo=ciclo_operativo_no_encontrado, enviados=0, fallidos=0.";
        }

        var idempotencyKey = $"task:{Code}:CH:{schedule.ClearingHouseId}:DATE:{localDate:yyyyMMdd}:CYCLE:{cycle.Id}:FLOW:OUTBOUND";
        var result = await service.ExecuteOutboundAsync(
            cycle.Id,
            AchManagedFileExecutionOrigin.Automatic,
            $"task:{Code}",
            idempotencyKey,
            cancellationToken);

        return $"Intercambio saliente ACH Colombia: ciclo_operativo={cycle.Id}, fecha_operativa={localDate:yyyy-MM-dd}, enviados={result.Succeeded}, fallidos={result.Failed}.";
    }

    private static bool TryResolveOperationalTriggerContext(
        SchedulerTaskExecutionContext schedulerContext,
        out int operationalScheduleId,
        out int clearingHouseId,
        out int cycleConfigId)
    {
        operationalScheduleId = 0;
        clearingHouseId = 0;
        cycleConfigId = 0;
        var data = schedulerContext.JobData;
        return data is not null
            && data.TryGetValue("OperationalScheduleId", out var scheduleValue)
            && int.TryParse(scheduleValue, out operationalScheduleId)
            && data.TryGetValue("ClearingHouseId", out var clearingHouseValue)
            && int.TryParse(clearingHouseValue, out clearingHouseId)
            && data.TryGetValue("CycleConfigId", out var cycleConfigValue)
            && int.TryParse(cycleConfigValue, out cycleConfigId)
            && data.TryGetValue("Flow", out var flow)
            && string.Equals(flow, "outbound", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildOperationalScheduleFingerprint(AchColombiaCycleOperationalSchedule schedule)
        => string.Join("|",
            schedule.Id,
            schedule.ClearingHouseId,
            schedule.ClearingHouseCycleConfigId,
            schedule.AutomaticOutboundEnabled,
            schedule.AutomaticOutboundStartTime?.ToString("c", CultureInfo.InvariantCulture) ?? "NULL",
            schedule.AutomaticOutboundLatestStartTime?.ToString("c", CultureInfo.InvariantCulture) ?? "NULL",
            (int)schedule.OutboundExecutionStrategy,
            schedule.EffectiveTimeZoneId,
            schedule.IsActive,
            schedule.ClearingHouseCycleConfig.EffectiveFrom.ToString("O", CultureInfo.InvariantCulture),
            schedule.ClearingHouseCycleConfig.EffectiveTo?.ToString("O", CultureInfo.InvariantCulture) ?? "NULL");

    private static DateTime ResolveLocalDate(DateTimeOffset currentInstant, string timeZoneId)
    {
        var zone = ResolveTimeZone(timeZoneId);
        return TimeZoneInfo.ConvertTime(currentInstant, zone).Date;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException) when (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
    }

    private OperationalCycleWindow ResolveSendingWindow(AchCycle cycle, DateTimeOffset currentInstant)
    {
        var timeZoneId = cycle.ClearingHouse?.ClearingHouseConfig?.TimeZoneId;
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new InvalidOperationException("ACHCOL_OPERATIONAL_TIMEZONE_NOT_CONFIGURED");
        }

        return windowResolver.Resolve(
            cycle.ProcessingDate,
            cycle.StartTime,
            cycle.CutoffTime,
            timeZoneId,
            currentInstant);
    }

    private static DateTimeOffset AsUtcInstant(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);
}

public sealed class AchColombiaManagedMftInboundHandler(
    AchDbContext context,
    IAchColombiaManagedMftExecutionService service,
    IOperationalTimeSnapshotProvider operationalTimeProvider,
    IOperationalCycleWindowResolver windowResolver) : ITaskHandler
{
    private const string ClearingHouseCode = "ACHCOL";

    public string Code => "AchColombiaManagedMftInbound";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var now = operationalTimeProvider.CaptureNow();
        if (!await IsInsideExplicitReleaseWindowAsync(now, cancellationToken))
        {
            return "Intercambio entrante ACH Colombia: polling=omitido, motivo=fuera_ventana_liberacion, recibidos=0, procesados=0, rechazados=0.";
        }

        var result = await service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            $"task:{Code}",
            $"task:{Code}:{now.CapturedAtUtc:yyyyMMddHHmm}",
            cancellationToken);
        return $"Intercambio entrante ACH Colombia: recibidos={result.Processed}, procesados={result.Succeeded}, rechazados={result.Failed}.";
    }

    private async Task<bool> IsInsideExplicitReleaseWindowAsync(
        Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames.OperationalTimeSnapshot now,
        CancellationToken cancellationToken)
    {
        var operationalDate = now.OperationalDate.ToDateTime(TimeOnly.MinValue);
        var cycles = await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse)
                .ThenInclude(x => x!.ClearingHouseConfig)
            .Where(x => x.ClearingHouse!.Code == ClearingHouseCode
                && x.ProcessingDate == operationalDate)
            .OrderBy(x => x.OutputReleaseStartTime)
            .ToListAsync(cancellationToken);

        var currentInstant = AsUtcInstant(now.CapturedAtUtc);
        foreach (var cycle in cycles.Where(x => x.OutputReleaseStartTime.HasValue && x.OutputReleaseEndTime.HasValue))
        {
            var timeZoneId = cycle.ClearingHouse?.ClearingHouseConfig?.TimeZoneId;
            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                throw new InvalidOperationException("ACHCOL_OPERATIONAL_TIMEZONE_NOT_CONFIGURED");
            }

            var releaseWindow = windowResolver.Resolve(
                cycle.ProcessingDate,
                cycle.OutputReleaseStartTime!.Value,
                cycle.OutputReleaseEndTime!.Value,
                timeZoneId,
                currentInstant);

            if (releaseWindow.Status == OperationalCycleWindowStatus.Inside)
            {
                return true;
            }
        }

        // FASE 7: no se infieren ventanas desde OutputReleaseTime ni se conserva
        // polling legacy cuando faltan ventanas explícitas. El archivo permanece
        // en la frontera CFA y será reclamado en una ventana válida posterior.
        return false;
    }

    private static DateTimeOffset AsUtcInstant(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);
}
