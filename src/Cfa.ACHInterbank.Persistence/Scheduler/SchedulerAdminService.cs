using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Application.Scheduler.Models;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.Scheduler;

[Scoped]
public sealed class SchedulerAdminService : ISchedulerAdminService
{
    private const string DynamicGroup = "db-tasks";
    private readonly AchDbContext _db;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly SchedulerSyncService _syncService;
    private readonly QuartzJobStoreOptions _options;

    public SchedulerAdminService(
        AchDbContext db,
        ISchedulerFactory schedulerFactory,
        SchedulerSyncService syncService,
        IConfiguration configuration)
    {
        _db = db;
        _schedulerFactory = schedulerFactory;
        _syncService = syncService;
        _options = QuartzJobStoreOptionsFactory.Create(configuration);
    }

    public async Task<SchedulerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var offlineLimit = now.AddSeconds(-Math.Max(_options.OfflineThresholdSeconds, 10));
        var instances = await _db.SchedulerInstanceStates.AsNoTracking()
            .Where(x => x.SchedulerName == _options.SchedulerName)
            .ToListAsync(cancellationToken);
        var recentLimit = now.AddHours(-24);
        var executions = _db.TaskExecutionLogs.AsNoTracking().Where(x => x.StartedAt >= recentLimit);
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        return new SchedulerOverviewDto(
            instances.Count,
            instances.Count(x => x.StoppedAtUtc == null && x.LastHeartbeatUtc >= offlineLimit),
            instances.Count(x => x.StoppedAtUtc != null || x.LastHeartbeatUtc < offlineLimit),
            await executions.CountAsync(x => x.Status == SchedulerExecutionStatus.Running, cancellationToken),
            (await GetTasksAsync(cancellationToken)).Count(x => x.NextExecutionUtc.HasValue),
            await executions.CountAsync(x => x.Status == SchedulerExecutionStatus.Failed, cancellationToken),
            await executions.CountAsync(x => x.Status == SchedulerExecutionStatus.Misfired || x.MisfireDetected, cancellationToken),
            scheduler.SchedulerName,
            _options.IsPersistentMode(),
            _options.Clustered);
    }

    public async Task<IReadOnlyList<SchedulerTaskDto>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _db.TaskDefinitions.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var result = new List<SchedulerTaskDto>();

        foreach (var task in tasks)
        {
            var catalog = SchedulerTaskCatalog.ByHandlerCode(task.Code);
            if (catalog is null)
            {
                continue;
            }

            var last = await _db.TaskExecutionLogs.AsNoTracking()
                .Where(x => x.TaskDefinitionId == task.Id)
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);
            var trigger = await scheduler.GetTrigger(new TriggerKey($"trg:{task.Id}", DynamicGroup), cancellationToken);
            var running = await _db.TaskExecutionLogs.AsNoTracking()
                .AnyAsync(x => x.TaskDefinitionId == task.Id && x.Status == SchedulerExecutionStatus.Running && x.FinishedAt == null, cancellationToken);
            result.Add(ToTaskDto(task, catalog, last, trigger?.GetNextFireTimeUtc(), running));
        }

        return result;
    }

    public async Task<SchedulerTaskDto?> GetTaskAsync(string taskCode, CancellationToken cancellationToken = default)
    {
        var entry = SchedulerTaskCatalog.ByTaskCode(taskCode);
        if (entry is null)
        {
            return null;
        }

        var task = await _db.TaskDefinitions.AsNoTracking().SingleOrDefaultAsync(x => x.Code == entry.HandlerCode, cancellationToken);
        if (task is null)
        {
            return null;
        }

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var trigger = await scheduler.GetTrigger(new TriggerKey($"trg:{task.Id}", DynamicGroup), cancellationToken);
        var last = await _db.TaskExecutionLogs.AsNoTracking().Where(x => x.TaskDefinitionId == task.Id)
            .OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(cancellationToken);
        var running = await _db.TaskExecutionLogs.AsNoTracking()
            .AnyAsync(x => x.TaskDefinitionId == task.Id && x.Status == SchedulerExecutionStatus.Running && x.FinishedAt == null, cancellationToken);
        return ToTaskDto(task, entry, last, trigger?.GetNextFireTimeUtc(), running);
    }

    public async Task<SchedulerPagedResult<SchedulerExecutionDto>> GetHistoryAsync(
        string? taskCode,
        SchedulerHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var source = _db.TaskExecutionLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(taskCode))
        {
            var entry = SchedulerTaskCatalog.ByTaskCode(taskCode);
            if (entry is null)
            {
                return new SchedulerPagedResult<SchedulerExecutionDto>([], page, pageSize, 0);
            }

            source = source.Where(x => x.TaskDefinition.Code == entry.HandlerCode);
        }

        if (query.Status.HasValue) source = source.Where(x => x.Status == query.Status);
        if (query.FromUtc.HasValue) source = source.Where(x => x.StartedAt >= query.FromUtc);
        if (query.ToUtc.HasValue) source = source.Where(x => x.StartedAt <= query.ToUtc);
        if (!string.IsNullOrWhiteSpace(query.TriggerType)) source = source.Where(x => x.TriggerType == query.TriggerType);
        if (!string.IsNullOrWhiteSpace(query.InstanceId)) source = source.Where(x => x.SchedulerInstanceId == query.InstanceId);
        if (!string.IsNullOrWhiteSpace(query.UserName)) source = source.Where(x => x.RequestedByUserName == query.UserName);
        if (!string.IsNullOrWhiteSpace(query.CorrelationId)) source = source.Where(x => x.CorrelationId == query.CorrelationId);

        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(x => x.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new SchedulerPagedResult<SchedulerExecutionDto>(items.Select(ToExecutionDto).ToList(), page, pageSize, total);
    }

    public async Task<SchedulerExecutionDto?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _db.TaskExecutionLogs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ExecutionId == executionId, cancellationToken);
        return execution is null ? null : ToExecutionDto(execution);
    }

    public async Task<IReadOnlyList<SchedulerInstanceDto>> GetInstancesAsync(CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var degradedLimit = now.AddSeconds(-Math.Max(_options.InstanceHeartbeatSeconds * 2, 5));
        var offlineLimit = now.AddSeconds(-Math.Max(_options.OfflineThresholdSeconds, 10));
        var items = await _db.SchedulerInstanceStates.AsNoTracking()
            .Where(x => x.SchedulerName == scheduler.SchedulerName)
            .OrderBy(x => x.InstanceName)
            .ToListAsync(cancellationToken);

        return items.Select(x => new SchedulerInstanceDto(
            x.InstanceId,
            x.InstanceName,
            x.HostName,
            x.StartedAtUtc,
            x.LastHeartbeatUtc,
            ResolveInstanceStatus(x, degradedLimit, offlineLimit),
            x.InstanceId == scheduler.SchedulerInstanceId,
            x.CurrentlyExecutingJobs,
            x.Version)).ToList();
    }

    public async Task<ManualExecutionResult> ExecuteNowAsync(ExecuteSchedulerTaskCommand command, CancellationToken cancellationToken = default)
    {
        var catalog = SchedulerTaskCatalog.ByTaskCode(command.TaskCode);
        if (catalog is null)
        {
            return new ManualExecutionResult(ManualExecutionOutcome.NotFound, null, "La tarea solicitada no está autorizada.");
        }

        var task = await _db.TaskDefinitions.SingleOrDefaultAsync(x => x.Code == catalog.HandlerCode, cancellationToken);
        if (task is null)
        {
            return new ManualExecutionResult(ManualExecutionOutcome.NotFound, null, "La tarea no existe.");
        }

        var requestId = command.RequestId.ToString("D");
        var duplicate = await _db.TaskExecutionLogs.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
        if (duplicate is not null)
        {
            return new ManualExecutionResult(ManualExecutionOutcome.Duplicate, duplicate.ExecutionId, "La solicitud ya fue registrada.");
        }

        if (!catalog.ManualAllowed || !task.ManualExecutionEnabled)
        {
            return await RecordRejectedAsync(task, catalog.TaskCode, command, "La ejecución manual no está habilitada para esta tarea.", cancellationToken);
        }

        if (task.Paused || task.Status != TaskStatusEnum.Enabled)
        {
            return await RecordRejectedAsync(task, catalog.TaskCode, command, "La tarea está pausada o deshabilitada.", cancellationToken);
        }

        var active = await _db.TaskExecutionLogs.AsNoTracking()
            .Where(x => x.TaskDefinitionId == task.Id && x.FinishedAt == null
                && (x.Status == SchedulerExecutionStatus.Pending || x.Status == SchedulerExecutionStatus.Running))
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (active is not null && task.ConcurrencyPolicy != ConcurrencyPolicyEnum.AllowParallel)
        {
            return new ManualExecutionResult(ManualExecutionOutcome.Conflict, null, "La tarea ya tiene una ejecución activa.", active.ExecutionId);
        }

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"job:{task.Id}", DynamicGroup);
        if (!await scheduler.CheckExists(jobKey, cancellationToken))
        {
            await _syncService.SynchronizeTaskAsync(task.Id, cancellationToken);
        }

        if (!await scheduler.CheckExists(jobKey, cancellationToken))
        {
            return await RecordRejectedAsync(task, catalog.TaskCode, command, "La tarea no está disponible en Quartz.", cancellationToken);
        }

        var executionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var log = new TaskExecutionLog
        {
            TaskDefinitionId = task.Id,
            ExecutionId = executionId,
            ExecutionKey = executionId.ToString("N"),
            TaskCode = catalog.TaskCode,
            JobName = jobKey.Name,
            JobGroup = jobKey.Group,
            TriggerName = $"manual:{executionId:N}",
            TriggerType = "Manual",
            FireInstanceId = string.Empty,
            SchedulerInstanceId = string.Empty,
            SchedulerInstanceName = string.Empty,
            RequestedByUserId = command.UserId,
            RequestedByUserName = command.UserName,
            RequestReason = command.Reason.Trim(),
            RequestId = requestId,
            IdempotencyKey = requestId,
            CorrelationId = command.CorrelationId,
            ScheduledAt = now,
            StartedAt = now,
            Status = SchedulerExecutionStatus.Pending,
            ManualConcurrencyKey = task.ConcurrencyPolicy == ConcurrencyPolicyEnum.AllowParallel ? null : catalog.TaskCode,
            CreatedAtUtc = now
        };
        _db.TaskExecutionLogs.Add(log);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            duplicate = await _db.TaskExecutionLogs.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == requestId, cancellationToken);
            if (duplicate is not null)
            {
                return new ManualExecutionResult(ManualExecutionOutcome.Duplicate, duplicate.ExecutionId, "La solicitud ya fue registrada.");
            }

            active = await _db.TaskExecutionLogs.AsNoTracking().SingleOrDefaultAsync(x => x.ManualConcurrencyKey == catalog.TaskCode, cancellationToken);
            return new ManualExecutionResult(ManualExecutionOutcome.Conflict, null, "La tarea ya tiene una solicitud manual activa.", active?.ExecutionId);
        }

        try
        {
            await scheduler.TriggerJob(jobKey, new JobDataMap
            {
                ["ExecutionId"] = executionId.ToString("D"),
                ["TriggerType"] = "Manual",
                ["RequestedByUserId"] = command.UserId ?? string.Empty,
                ["RequestedByUserName"] = command.UserName,
                ["RequestReason"] = command.Reason.Trim(),
                ["RequestId"] = requestId,
                ["IdempotencyKey"] = requestId,
                ["CorrelationId"] = command.CorrelationId
            }, cancellationToken);
        }
        catch
        {
            log.Status = SchedulerExecutionStatus.Rejected;
            log.ErrorCode = "QUARTZ_TRIGGER_REJECTED";
            log.Error = "Quartz no aceptó el disparo manual.";
            log.FinishedAt = DateTimeOffset.UtcNow;
            log.ManualConcurrencyKey = null;
            await _db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return new ManualExecutionResult(ManualExecutionOutcome.Accepted, executionId, "La ejecución fue aceptada por el clúster.");
    }

    public async Task<bool> PauseAsync(string taskCode, string? userId, string userName, CancellationToken cancellationToken = default)
    {
        var task = await FindTaskAsync(taskCode, cancellationToken);
        if (task is null) return false;
        task.Paused = true;
        await _db.SaveChangesAsync(cancellationToken);
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.PauseJob(new JobKey($"job:{task.Id}", DynamicGroup), cancellationToken);
        return true;
    }

    public async Task<bool> ResumeAsync(string taskCode, string? userId, string userName, CancellationToken cancellationToken = default)
    {
        var task = await FindTaskAsync(taskCode, cancellationToken);
        if (task is null) return false;
        task.Paused = false;
        await _db.SaveChangesAsync(cancellationToken);
        await _syncService.SynchronizeTaskAsync(task.Id, cancellationToken);
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.ResumeJob(new JobKey($"job:{task.Id}", DynamicGroup), cancellationToken);
        return true;
    }

    public async Task<SchedulerTaskDto?> UpdateScheduleAsync(SchedulerScheduleUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ValidateSchedule(command.Schedule);
        var task = await FindTaskAsync(command.TaskCode, cancellationToken);
        if (task is null) return null;
        ApplySchedule(task, command.Schedule);
        await _db.SaveChangesAsync(cancellationToken);
        await _syncService.SynchronizeTaskAsync(task.Id, cancellationToken);
        return await GetTaskAsync(command.TaskCode, cancellationToken);
    }

    public Task<SchedulerSchedulePreviewDto> PreviewScheduleAsync(SchedulerScheduleUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ValidateSchedule(request);
        var description = DescribeSchedule(request.PeriodicityType, request.N, request.Minute, request.TimeOfDay, request.WeeklyDay, request.MonthDay, request.CronExpression);
        var next = CalculateNextExecutions(request, 5);
        return Task.FromResult(new SchedulerSchedulePreviewDto(description, next));
    }

    private async Task<TaskDefinition?> FindTaskAsync(string taskCode, CancellationToken cancellationToken)
    {
        var entry = SchedulerTaskCatalog.ByTaskCode(taskCode);
        return entry is null
            ? null
            : await _db.TaskDefinitions.SingleOrDefaultAsync(x => x.Code == entry.HandlerCode, cancellationToken);
    }

    private async Task<ManualExecutionResult> RecordRejectedAsync(
        TaskDefinition task,
        string taskCode,
        ExecuteSchedulerTaskCommand command,
        string message,
        CancellationToken cancellationToken)
    {
        var executionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        _db.TaskExecutionLogs.Add(new TaskExecutionLog
        {
            TaskDefinitionId = task.Id,
            ExecutionId = executionId,
            ExecutionKey = executionId.ToString("N"),
            TaskCode = taskCode,
            JobName = $"job:{task.Id}",
            JobGroup = DynamicGroup,
            TriggerName = $"manual-rejected:{executionId:N}",
            TriggerType = "Manual",
            RequestedByUserId = command.UserId,
            RequestedByUserName = command.UserName,
            RequestReason = command.Reason.Trim(),
            RequestId = command.RequestId.ToString("D"),
            IdempotencyKey = command.RequestId.ToString("D"),
            CorrelationId = command.CorrelationId,
            ScheduledAt = now,
            StartedAt = now,
            FinishedAt = now,
            Status = SchedulerExecutionStatus.Rejected,
            ErrorCode = "MANUAL_EXECUTION_REJECTED",
            Error = message,
            CreatedAtUtc = now
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new ManualExecutionResult(ManualExecutionOutcome.Rejected, executionId, message);
    }

    private static SchedulerTaskDto ToTaskDto(
        TaskDefinition task,
        SchedulerTaskCatalogEntry catalog,
        TaskExecutionLog? last,
        DateTimeOffset? next,
        bool running)
        => new(
            catalog.TaskCode,
            task.Name,
            task.Description ?? catalog.Description,
            task.Paused ? "Pausada" : task.Status == TaskStatusEnum.Enabled ? "Activa" : "Deshabilitada",
            catalog.ClearingHouse,
            DescribeSchedule((int)task.PeriodicityType, task.N, task.Minute, task.TimeOfDay?.ToString("HH:mm"), task.WeeklyDay, task.MonthDay, task.CronExpression),
            task.CronExpression,
            task.TimeZoneId ?? "America/Bogota",
            task.MisfirePolicy,
            task.MisfirePolicy == SchedulerMisfirePolicy.FireAndProceed
                ? "Ejecutar una vez al recuperarse y continuar normalmente."
                : "Omitir la ejecución perdida y continuar con la siguiente.",
            last?.StartedAt,
            next,
            last?.Status.ToString(),
            last?.DurationMilliseconds,
            last?.SchedulerInstanceName,
            running ? "En ejecución" : task.Paused ? "Pausada" : "En espera",
            catalog.ManualAllowed && task.ManualExecutionEnabled,
            task.RequestsRecovery,
            task.ConcurrencyPolicy == ConcurrencyPolicyEnum.AllowParallel,
            (int)task.PeriodicityType,
            task.N,
            task.Minute,
            task.TimeOfDay?.ToString("HH:mm"),
            task.WeeklyDay,
            task.MonthDay,
            task.CalendarPolicy == CalendarPolicyEnum.OnlyBusinessDays,
            task.StartAt,
            task.EndAt);

    private static SchedulerExecutionDto ToExecutionDto(TaskExecutionLog item)
        => new(
            item.ExecutionId ?? Guid.Empty,
            item.TaskCode,
            item.JobName,
            item.JobGroup,
            item.TriggerName,
            item.TriggerType,
            item.FireInstanceId,
            item.SchedulerInstanceId,
            item.SchedulerInstanceName,
            item.RequestedByUserId,
            item.RequestedByUserName,
            item.RequestReason,
            item.RequestId,
            item.CorrelationId,
            item.ScheduledAt,
            item.ActualFireTimeUtc,
            item.StartedAt,
            item.FinishedAt,
            item.DurationMilliseconds,
            item.Status,
            item.IsRecovery,
            item.RefireCount,
            item.MisfireDetected,
            item.Output,
            item.ErrorCode,
            item.Error,
            item.OriginalFireInstanceId,
            item.RecoveredByInstanceId,
            item.RecoveryStartedAtUtc,
            item.RecoveryResult);

    private static string ResolveInstanceStatus(SchedulerInstanceState state, DateTimeOffset degradedLimit, DateTimeOffset offlineLimit)
    {
        if (state.StoppedAtUtc.HasValue || state.LastHeartbeatUtc < offlineLimit) return "Desconectada";
        if (state.LastHeartbeatUtc < degradedLimit) return "Degradada";
        return state.Status;
    }

    private static void ValidateSchedule(SchedulerScheduleUpdateRequest request)
    {
        if (!Enum.IsDefined(typeof(PeriodicityTypeEnum), request.PeriodicityType))
            throw new ArgumentException("La frecuencia no es válida.");
        if (!Enum.IsDefined(request.MisfirePolicy))
            throw new ArgumentException("La política de misfire no es válida.");
        if (request.StartAt.HasValue && request.EndAt.HasValue && request.StartAt >= request.EndAt)
            throw new ArgumentException("La fecha de inicio debe ser anterior a la fecha de fin.");

        try { _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId); }
        catch (TimeZoneNotFoundException) { throw new ArgumentException("La zona horaria no es válida."); }
        catch (InvalidTimeZoneException) { throw new ArgumentException("La zona horaria no es válida."); }

        var periodicity = (PeriodicityTypeEnum)request.PeriodicityType;
        if (periodicity == PeriodicityTypeEnum.Cron
            && (string.IsNullOrWhiteSpace(request.CronExpression) || !CronExpression.IsValidExpression(request.CronExpression)))
            throw new ArgumentException("La expresión cron no es válida.");
        if (periodicity == PeriodicityTypeEnum.EveryNMinutes && request.N is not (> 0 and <= 1440))
            throw new ArgumentException("El intervalo debe estar entre 1 y 1440 minutos.");
        if (periodicity == PeriodicityTypeEnum.HourlyAtMinute && request.Minute is not (>= 0 and <= 59))
            throw new ArgumentException("El minuto debe estar entre 0 y 59.");
        if (periodicity is PeriodicityTypeEnum.DailyAtTime or PeriodicityTypeEnum.Weekly or PeriodicityTypeEnum.Monthly
            && !TimeOnly.TryParse(request.TimeOfDay, out _))
            throw new ArgumentException("La hora no es válida.");
    }

    private static void ApplySchedule(TaskDefinition task, SchedulerScheduleUpdateRequest request)
    {
        task.PeriodicityType = (PeriodicityTypeEnum)request.PeriodicityType;
        task.N = request.N;
        task.Minute = request.Minute;
        task.TimeOfDay = TimeOnly.TryParse(request.TimeOfDay, out var time) ? time : null;
        task.WeeklyDay = request.WeeklyDay;
        task.MonthDay = request.MonthDay;
        task.CronExpression = string.IsNullOrWhiteSpace(request.CronExpression) ? null : request.CronExpression.Trim();
        task.TimeZoneId = request.TimeZoneId.Trim();
        task.MisfirePolicy = request.MisfirePolicy;
        task.CalendarPolicy = request.OnlyBusinessDays ? CalendarPolicyEnum.OnlyBusinessDays : CalendarPolicyEnum.IgnoreCalendar;
        task.StartAt = request.StartAt;
        task.EndAt = request.EndAt;
    }

    private static string DescribeSchedule(int periodicityValue, int? n, int? minute, string? time, DayOfWeek? weeklyDay, int? monthDay, string? cron)
    {
        var periodicity = (PeriodicityTypeEnum)periodicityValue;
        return periodicity switch
        {
            PeriodicityTypeEnum.Once => "Una sola vez",
            PeriodicityTypeEnum.EveryNMinutes => $"Cada {n ?? 1} minutos",
            PeriodicityTypeEnum.HourlyAtMinute => $"Cada hora, al minuto {minute ?? 0:00}",
            PeriodicityTypeEnum.DailyAtTime => $"Todos los días a las {time ?? "00:00"}",
            PeriodicityTypeEnum.Weekly => $"Cada {DayName(weeklyDay)} a las {time ?? "00:00"}",
            PeriodicityTypeEnum.Monthly => $"El día {monthDay ?? 1} de cada mes a las {time ?? "00:00"}",
            PeriodicityTypeEnum.Cron => $"Programación cron: {cron}",
            _ => "Sin programación"
        };
    }

    private static IReadOnlyList<DateTimeOffset> CalculateNextExecutions(SchedulerScheduleUpdateRequest request, int count)
    {
        var start = request.StartAt ?? DateTimeOffset.UtcNow;
        if ((PeriodicityTypeEnum)request.PeriodicityType == PeriodicityTypeEnum.EveryNMinutes)
            return Enumerable.Range(1, count).Select(i => start.AddMinutes((request.N ?? 1) * i)).ToList();
        if ((PeriodicityTypeEnum)request.PeriodicityType == PeriodicityTypeEnum.Once)
            return [start];

        var expressionText = (PeriodicityTypeEnum)request.PeriodicityType switch
        {
            PeriodicityTypeEnum.HourlyAtMinute => $"0 {request.Minute ?? 0} * * * ?",
            PeriodicityTypeEnum.DailyAtTime => BuildCronForTime(request.TimeOfDay, "*", "?"),
            PeriodicityTypeEnum.Weekly => BuildCronForTime(request.TimeOfDay, "?", request.WeeklyDay?.ToString()[..3].ToUpperInvariant() ?? "MON"),
            PeriodicityTypeEnum.Monthly => BuildCronForTime(request.TimeOfDay, (request.MonthDay ?? 1).ToString(), "?"),
            _ => request.CronExpression!
        };
        var expression = new CronExpression(expressionText) { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId) };
        var next = new List<DateTimeOffset>();
        var cursor = start;
        while (next.Count < count)
        {
            var value = expression.GetNextValidTimeAfter(cursor);
            if (!value.HasValue || request.EndAt.HasValue && value > request.EndAt) break;
            next.Add(value.Value);
            cursor = value.Value;
        }
        return next;
    }

    private static string BuildCronForTime(string? value, string dayOfMonth, string dayOfWeek)
    {
        var time = TimeOnly.TryParse(value, out var parsed) ? parsed : new TimeOnly(0, 0);
        return $"0 {time.Minute} {time.Hour} {dayOfMonth} * {dayOfWeek}";
    }

    private static string DayName(DayOfWeek? day) => day switch
    {
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sábado",
        DayOfWeek.Sunday => "domingo",
        _ => "semana"
    };
}
