using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Application.Scheduler.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;
using System.Globalization;

namespace Cfa.ACHInterbank.Persistence.Scheduler;

[Scoped]
public sealed class SchedulerAdminService : ISchedulerAdminService
{
    private const string DynamicGroup = "db-tasks";
    private readonly AchDbContext _db;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly SchedulerSyncService _syncService;
    private readonly QuartzJobStoreOptions _options;
    private readonly IOperationalCalendarService _operationalCalendar;
    private readonly IOperationalCycleWindowResolver _windowResolver;
    private readonly TimeProvider _timeProvider;

    public SchedulerAdminService(
        AchDbContext db,
        ISchedulerFactory schedulerFactory,
        SchedulerSyncService syncService,
        IConfiguration configuration,
        IOperationalCalendarService? operationalCalendar = null,
        IOperationalCycleWindowResolver? windowResolver = null,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _schedulerFactory = schedulerFactory;
        _syncService = syncService;
        _options = QuartzJobStoreOptionsFactory.Create(configuration);
        _operationalCalendar = operationalCalendar ?? new OperationalCalendarService(db);
        _windowResolver = windowResolver ?? new OperationalCycleWindowResolver();
        _timeProvider = timeProvider ?? TimeProvider.System;
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
            _options.Clustered,
            await _db.TaskDefinitions.CountAsync(x => x.SchedulerSynchronizationStatus != "Synchronized", cancellationToken));
    }

    public async Task<IReadOnlyList<SchedulerTaskDto>> GetTasksAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _db.TaskDefinitions.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var result = new List<SchedulerTaskDto>();
        var cycleContexts = await BuildOperationalContextsAsync(cancellationToken);

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
            var running = await _db.TaskExecutionLogs.AsNoTracking()
                .AnyAsync(x => x.TaskDefinitionId == task.Id && x.Status == SchedulerExecutionStatus.Running && x.FinishedAt == null, cancellationToken);
            var next = await GetNextFireTimeAsync(scheduler, task.Id, cancellationToken);
            result.Add(ToTaskDto(task, catalog, last, next, running,
                catalog.UsesCycleSchedule ? cycleContexts : []));
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
        var last = await _db.TaskExecutionLogs.AsNoTracking().Where(x => x.TaskDefinitionId == task.Id)
            .OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(cancellationToken);
        var running = await _db.TaskExecutionLogs.AsNoTracking()
            .AnyAsync(x => x.TaskDefinitionId == task.Id && x.Status == SchedulerExecutionStatus.Running && x.FinishedAt == null, cancellationToken);
        var next = await GetNextFireTimeAsync(scheduler, task.Id, cancellationToken);
        var contexts = entry.UsesCycleSchedule ? await BuildOperationalContextsAsync(cancellationToken) : [];
        return ToTaskDto(task, entry, last, next, running, contexts);
    }

    public async Task<SchedulerTechnicalInfoDto?> GetTechnicalInfoAsync(
        string taskCode,
        CancellationToken cancellationToken = default)
    {
        var entry = SchedulerTaskCatalog.ByTaskCode(taskCode);
        if (entry is null) return null;

        var task = await _db.TaskDefinitions.AsNoTracking()
            .Include(x => x.Parameters)
            .SingleOrDefaultAsync(x => x.Code == entry.HandlerCode, cancellationToken);
        if (task is null) return null;

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey($"job:{task.Id}", DynamicGroup);
        var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken) ?? [];
        return new SchedulerTechnicalInfoDto(
            entry.TaskCode,
            entry.HandlerCode,
            entry.SoapService,
            jobKey.Name,
            jobKey.Group,
            task.CronExpression,
            task.TimeZoneId ?? "America/Bogota",
            task.MisfirePolicy,
            task.RequestsRecovery,
            task.ConcurrencyPolicy == ConcurrencyPolicyEnum.AllowParallel,
            task.Parameters.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase),
            triggers.Select(x => x.Key.ToString()).OrderBy(x => x).ToArray());
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
            return new ManualExecutionResult(ManualExecutionOutcome.Conflict, null, "La tarea ya está en ejecución y no puede iniciarse nuevamente hasta que finalice.", active.ExecutionId);
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
            return new ManualExecutionResult(ManualExecutionOutcome.Conflict, null, "La tarea ya está en ejecución y no puede iniciarse nuevamente hasta que finalice.", active?.ExecutionId);
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

        return new ManualExecutionResult(ManualExecutionOutcome.Accepted, executionId, "La ejecución fue solicitada correctamente. Puedes consultar su progreso en el historial.");
    }

    public async Task<bool> PauseAsync(string taskCode, string? userId, string userName, CancellationToken cancellationToken = default)
    {
        var task = await FindTaskAsync(taskCode, cancellationToken);
        if (task is null) return false;
        task.Paused = true;
        MarkSynchronizationPending(task);
        await _db.SaveChangesAsync(cancellationToken);
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.PauseJob(new JobKey($"job:{task.Id}", DynamicGroup), cancellationToken);
            await MarkSynchronizationSucceededAsync(task, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            await MarkSynchronizationFailedAsync(task, ex, cancellationToken);
            throw;
        }
    }

    public async Task<bool> ResumeAsync(string taskCode, string? userId, string userName, CancellationToken cancellationToken = default)
    {
        var task = await FindTaskAsync(taskCode, cancellationToken);
        if (task is null) return false;
        task.Paused = false;
        MarkSynchronizationPending(task);
        await _db.SaveChangesAsync(cancellationToken);
        try
        {
            await _syncService.SynchronizeTaskAsync(task.Id, cancellationToken);
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.ResumeJob(new JobKey($"job:{task.Id}", DynamicGroup), cancellationToken);
            await MarkSynchronizationSucceededAsync(task, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            await MarkSynchronizationFailedAsync(task, ex, cancellationToken);
            throw;
        }
    }

    public async Task<SchedulerTaskDto?> UpdateScheduleAsync(SchedulerScheduleUpdateCommand command, CancellationToken cancellationToken = default)
    {
        var catalog = SchedulerTaskCatalog.ByTaskCode(command.TaskCode);
        if (catalog?.UsesCycleSchedule == true)
        {
            throw new ArgumentException("La programación de esta tarea depende del ciclo de compensación. Para modificar el horario, actualiza la configuración del ciclo correspondiente.");
        }

        ValidateSchedule(command.Schedule);
        if (catalog is not null
            && (PeriodicityTypeEnum)command.Schedule.PeriodicityType == PeriodicityTypeEnum.EveryNMinutes
            && command.Schedule.N < catalog.MinimumIntervalMinutes)
        {
            throw new ArgumentException($"El intervalo mínimo permitido para esta tarea es de {catalog.MinimumIntervalMinutes} minutos.");
        }
        var task = await FindTaskAsync(command.TaskCode, cancellationToken);
        if (task is null) return null;
        ApplySchedule(task, command.Schedule);
        MarkSynchronizationPending(task);
        await _db.SaveChangesAsync(cancellationToken);
        try
        {
            await _syncService.SynchronizeTaskAsync(task.Id, cancellationToken);
            await MarkSynchronizationSucceededAsync(task, cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkSynchronizationFailedAsync(task, ex, cancellationToken);
            throw;
        }
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
        bool running,
        IReadOnlyList<SchedulerOperationalContextDto> operationalContexts)
        => new(
            catalog.TaskCode,
            catalog.Name,
            catalog.Description,
            task.Paused ? "Pausada" : task.Status == TaskStatusEnum.Enabled ? "Activa" : "Deshabilitada",
            catalog.UsesCycleSchedule
                ? string.Join(" y ", operationalContexts.Select(x => x.ClearingHouseName).Distinct())
                : null,
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
            task.EndAt,
            task.SchedulerSynchronizationStatus,
            task.LastSchedulerSynchronizationError,
            catalog.Category,
            catalog.ProcessType,
            catalog.SoapService,
            catalog.UsesCycleSchedule,
            !catalog.UsesCycleSchedule,
            operationalContexts);

    private async Task<IReadOnlyList<SchedulerOperationalContextDto>> BuildOperationalContextsAsync(
        CancellationToken cancellationToken)
    {
        var configurations = await _db.ClearingHouseCycleConfigs.AsNoTracking()
            .Include(x => x.ClearingHouse)
                .ThenInclude(x => x.ClearingHouseConfig)
            .OrderBy(x => x.ClearingHouse.Name)
            .ThenBy(x => x.CycleName)
            .ToListAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow();
        var contexts = new List<SchedulerOperationalContextDto>(configurations.Count);

        foreach (var config in configurations)
        {
            var next = await FindNextWindowAsync(config, now, cancellationToken);
            var status = !config.ClearingHouse.IsActive
                ? "Cámara inactiva"
                : !config.IsActive
                    ? "Ciclo inactivo"
                    : next is { Window.IsInside: true }
                        ? "Ventana abierta"
                        : next is null
                            ? "Sin próxima ventana vigente"
                            : "Programada";
            contexts.Add(new SchedulerOperationalContextDto(
                config.Id,
                config.ClearingHouse.Code,
                config.ClearingHouse.Name,
                config.CycleName,
                $"{FormatTime(config.StartTime)} a {FormatTime(config.EndTime)}",
                FormatTime(config.CutoffTime),
                next?.Window.StartInstant,
                next?.Window.EndInstant,
                status));
        }

        return contexts;
    }

    private async Task<WindowCandidate?> FindNextWindowAsync(
        ClearingHouseCycleConfig config,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!config.IsActive || !config.ClearingHouse.IsActive) return null;
        var timeZoneId = config.ClearingHouse.ClearingHouseConfig.TimeZoneId;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);

        for (var offset = 0; offset <= 370; offset++)
        {
            var date = localDate.AddDays(offset);
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            if (dateTime.Date < config.EffectiveFrom.Date
                || config.EffectiveTo.HasValue && dateTime.Date > config.EffectiveTo.Value.Date
                || !await _operationalCalendar.IsBusinessDayAsync(date, config.ClearingHouseId, cancellationToken))
            {
                continue;
            }

            var window = _windowResolver.Resolve(dateTime, config.StartTime, config.EndTime, timeZoneId, now);
            if (window.Status != OperationalCycleWindowStatus.After)
            {
                return new WindowCandidate(window);
            }
        }

        return null;
    }

    private static string FormatTime(TimeSpan value)
        => $"{(int)value.TotalHours:00}:{value.Minutes:00}";

    private static async Task<DateTimeOffset?> GetNextFireTimeAsync(
        IScheduler scheduler,
        int taskId,
        CancellationToken cancellationToken)
    {
        var triggers = await scheduler.GetTriggersOfJob(new JobKey($"job:{taskId}", DynamicGroup), cancellationToken) ?? [];
        return triggers.Select(x => x.GetNextFireTimeUtc()).Where(x => x.HasValue).Min();
    }

    private sealed record WindowCandidate(OperationalCycleWindow Window);

    private static void MarkSynchronizationPending(TaskDefinition task)
    {
        task.SchedulerSynchronizationStatus = "Pending";
        task.LastSchedulerSynchronizationAttemptUtc = DateTimeOffset.UtcNow;
        task.LastSchedulerSynchronizationError = null;
    }

    private async Task MarkSynchronizationSucceededAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        if (task.SchedulerSynchronizationStatus == "Synchronized" && task.LastSchedulerSynchronizationError is null)
        {
            return;
        }

        task.SchedulerSynchronizationStatus = "Synchronized";
        task.LastSchedulerSynchronizationAttemptUtc = DateTimeOffset.UtcNow;
        task.LastSchedulerSynchronizationError = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkSynchronizationFailedAsync(TaskDefinition task, Exception exception, CancellationToken cancellationToken)
    {
        task.SchedulerSynchronizationStatus = "Failed";
        task.LastSchedulerSynchronizationAttemptUtc = DateTimeOffset.UtcNow;
        task.LastSchedulerSynchronizationError = Truncate(exception.Message, 2000);
        await _db.SaveChangesAsync(CancellationToken.None);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

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
        if (periodicity == PeriodicityTypeEnum.Weekly && !request.WeeklyDay.HasValue)
            throw new ArgumentException("Selecciona un día de la semana.");
        if (periodicity == PeriodicityTypeEnum.Monthly && request.MonthDay is not (>= 1 and <= 31))
            throw new ArgumentException("El día del mes debe estar entre 1 y 31.");
        if (periodicity == PeriodicityTypeEnum.Once && !request.StartAt.HasValue)
            throw new ArgumentException("Selecciona la fecha y hora de la única ejecución.");
        if (periodicity == PeriodicityTypeEnum.Yearly && !request.StartAt.HasValue)
            throw new ArgumentException("Selecciona la fecha y hora de la ejecución anual.");
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
            PeriodicityTypeEnum.Cron => DescribeAdvancedSchedule(cron),
            PeriodicityTypeEnum.Yearly => "Una vez al año en la fecha y hora seleccionadas",
            _ => "Sin programación"
        };
    }

    private static string DescribeAdvancedSchedule(string? cron)
    {
        if (!string.IsNullOrWhiteSpace(cron))
        {
            var parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length is 6 or 7
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minute)
                && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var hour)
                && int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var day)
                && int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
                && parts[5] == "?"
                && day is >= 1 and <= 31
                && month is >= 1 and <= 12
                && hour is >= 0 and <= 23
                && minute is >= 0 and <= 59)
            {
                var monthName = new DateTime(2000, month, 1).ToString("MMMM", CultureInfo.GetCultureInfo("es-CO"));
                return $"Una vez al año, el {day} de {monthName} a las {hour:00}:{minute:00}";
            }
        }

        return "Configuración avanzada administrada por el sistema";
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
            PeriodicityTypeEnum.Yearly => BuildYearlyCron(request.StartAt!.Value, request.TimeZoneId),
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

    private static string BuildYearlyCron(DateTimeOffset value, string timeZoneId)
    {
        var local = TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        return $"{local.Second} {local.Minute} {local.Hour} {local.Day} {local.Month} ?";
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
