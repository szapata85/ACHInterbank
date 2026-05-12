using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz;

public class SchedulerSyncService : BackgroundService
{
    private const string DynamicGroup = "db-tasks";
    private const int FullReconciliationEveryCycles = 10;

    private readonly IServiceProvider _sp;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<SchedulerSyncService> _logger;
    private readonly QuartzTaskCalendarEvaluator _calendarEvaluator;

    private DateTimeOffset _lastSync = DateTimeOffset.MinValue;
    private bool _hasCompletedFirstSync;
    private int _cyclesSinceFullReconciliation;

    public SchedulerSyncService(IServiceProvider sp, ISchedulerFactory schedulerFactory, ILogger<SchedulerSyncService> logger, QuartzTaskCalendarEvaluator calendarEvaluator)
    {
        _sp = sp;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
        _calendarEvaluator = calendarEvaluator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncOnceAsync(scheduler, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resincronizando tareas.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task SyncOnceAsync(IScheduler scheduler, CancellationToken cancellationToken)
    {
        var syncStartedAt = DateTimeOffset.UtcNow;
        var shouldRunFullReconciliation = !_hasCompletedFirstSync || _cyclesSinceFullReconciliation >= FullReconciliationEveryCycles;

        if (shouldRunFullReconciliation)
        {
            await ReconcileAllTasksAsync(scheduler, cancellationToken);
            _hasCompletedFirstSync = true;
            _cyclesSinceFullReconciliation = 0;
        }
        else
        {
            await SyncChangedTasksAsync(scheduler, cancellationToken);
            _cyclesSinceFullReconciliation++;
        }

        _lastSync = syncStartedAt;
    }

    private async Task SyncChangedTasksAsync(IScheduler scheduler, CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();

        var changedTasks = await db.TaskDefinitions
            .Include(t => t.Parameters)
            .Where(t => t.UpdatedAt > _lastSync)
            .ToListAsync(cancellationToken);

        foreach (var task in changedTasks)
        {
            await ProcessTaskSafelyAsync(scheduler, task, cancellationToken);
        }
    }

    private async Task ReconcileAllTasksAsync(IScheduler scheduler, CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
        var now = DateTimeOffset.UtcNow;

        var allTasks = await db.TaskDefinitions
            .Include(t => t.Parameters)
            .ToListAsync(cancellationToken);

        var activeTasks = allTasks.Where(t => IsTaskSchedulable(t, now)).ToList();
        foreach (var task in activeTasks)
        {
            await ProcessTaskSafelyAsync(scheduler, task, cancellationToken);
        }

        var activeTaskIds = activeTasks.Select(t => t.Id).ToHashSet();
        await DeleteOrphanDynamicJobsAsync(scheduler, activeTaskIds, cancellationToken);
    }

    private async Task ProcessTaskSafelyAsync(IScheduler scheduler, TaskDefinition task, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsTaskSchedulable(task, DateTimeOffset.UtcNow))
            {
                await DeleteTaskJobAsync(scheduler, task.Id, cancellationToken);
                return;
            }

            await ScheduleOrUpdateTaskAsync(scheduler, task, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando Task {Id}/{Code}. Se continuará con la siguiente.", task.Id, task.Code);
        }
    }

    private async Task ScheduleOrUpdateTaskAsync(IScheduler scheduler, TaskDefinition task, CancellationToken cancellationToken)
    {
        var jobKey = GetJobKey(task.Id);
        var triggerKey = GetTriggerKey(task.Id);

        var jobType = GetJobTypeForConcurrencyPolicy(task.ConcurrencyPolicy);
        var existing = await scheduler.GetJobDetail(jobKey, cancellationToken);
        if (existing is not null && existing.JobType != jobType)
        {
            await scheduler.DeleteJob(jobKey, cancellationToken);
        }

        var jobBuilder = JobBuilder.Create(jobType)
            .WithIdentity(jobKey)
            .UsingJobData("TaskId", task.Id);

        if (task.ConcurrencyPolicy == ConcurrencyPolicyEnum.SkipIfRunning)
        {
            jobBuilder.StoreDurably();
        }

        var job = jobBuilder.Build();
        var trigger = BuildTrigger(task, triggerKey);

        if (trigger is null)
        {
            await DeleteTaskJobAsync(scheduler, task.Id, cancellationToken);
            return;
        }

        await scheduler.ScheduleJob(job, new[] { trigger }, true, cancellationToken);
        _logger.LogInformation("Task {Id}/{Code} sincronizada con éxito.", task.Id, task.Code);
    }

    private async Task DeleteTaskJobAsync(IScheduler scheduler, int taskId, CancellationToken cancellationToken)
    {
        var jobKey = GetJobKey(taskId);
        await scheduler.DeleteJob(jobKey, cancellationToken);
        _logger.LogInformation("Task {Id} eliminada del scheduler por estado no programable.", taskId);
    }

    private async Task DeleteOrphanDynamicJobsAsync(IScheduler scheduler, IReadOnlySet<int> activeTaskIds, CancellationToken cancellationToken)
    {
        var keys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(DynamicGroup), cancellationToken);
        foreach (var key in keys)
        {
            var taskId = ParseTaskIdFromJobKey(key);
            if (!taskId.HasValue || !activeTaskIds.Contains(taskId.Value))
            {
                await scheduler.DeleteJob(key, cancellationToken);
                _logger.LogInformation("Job huérfano {JobKey} eliminado durante reconciliación.", key);
            }
        }
    }

    private static JobKey GetJobKey(int taskId) => new($"job:{taskId}", DynamicGroup);
    private static TriggerKey GetTriggerKey(int taskId) => new($"trg:{taskId}", DynamicGroup);

    private static int? ParseTaskIdFromJobKey(JobKey key)
    {
        const string prefix = "job:";
        if (!key.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(key.Name[prefix.Length..], out var id) ? id : null;
    }

    private static bool IsTaskSchedulable(TaskDefinition task, DateTimeOffset now)
    {
        if (task.Status == TaskStatusEnum.Disabled)
        {
            return false;
        }

        if (task.EndAt.HasValue && task.EndAt.Value.ToUniversalTime() <= now)
        {
            return false;
        }

        return true;
    }


    private static Type GetJobTypeForConcurrencyPolicy(ConcurrencyPolicyEnum policy)
        => policy switch
        {
            ConcurrencyPolicyEnum.AllowParallel => typeof(DynamicJob),
            ConcurrencyPolicyEnum.SkipIfRunning => typeof(NonConcurrentDynamicJob),
            ConcurrencyPolicyEnum.Queue => typeof(NonConcurrentDynamicJob),
            _ => typeof(NonConcurrentDynamicJob)
        };

    private ITrigger? BuildTrigger(TaskDefinition task, TriggerKey triggerKey)
    {
        if (task.StartAt.HasValue && task.EndAt.HasValue && task.StartAt >= task.EndAt)
        {
            _logger.LogWarning("La tarea {Id} tiene StartAt >= EndAt, no se programará", task.Id);
            return null!;
        }

        if (task.EndAt.HasValue && task.EndAt.Value <= DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("La tarea {Code} ({Id}) no se programará porque EndAt ya venció ({EndAt})", task.Code, task.Id, task.EndAt);
            return null;
        }

        var tz = _calendarEvaluator.ResolveTimeZone(task.TimeZoneId, _logger);
        var tb = TriggerBuilder.Create().WithIdentity(triggerKey);

        if (task.StartAt.HasValue)
            tb.StartAt(task.StartAt.Value);
        else
            tb.StartNow();

        if (task.EndAt.HasValue)
            tb.EndAt(task.EndAt.Value);

        switch (task.PeriodicityType)
        {
            case PeriodicityTypeEnum.Once:
                return tb.WithSimpleSchedule(s => s.WithRepeatCount(0)).Build();
            case PeriodicityTypeEnum.EveryNMinutes:
                return tb.WithSimpleSchedule(s => s.WithIntervalInMinutes(task.N ?? 1).RepeatForever()).Build();
            case PeriodicityTypeEnum.HourlyAtMinute:
                return tb.WithSchedule(CronScheduleBuilder.CronSchedule($"0 {task.Minute ?? 0} * * * ?").InTimeZone(tz)).Build();
            case PeriodicityTypeEnum.DailyAtTime:
                return tb.WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(task.TimeOfDay!.Value.Hour, task.TimeOfDay.Value.Minute).InTimeZone(tz)).Build();
            case PeriodicityTypeEnum.Weekly:
                return tb.WithSchedule(CronScheduleBuilder.WeeklyOnDayAndHourAndMinute(task.WeeklyDay!.Value, task.TimeOfDay!.Value.Hour, task.TimeOfDay.Value.Minute).InTimeZone(tz)).Build();
            case PeriodicityTypeEnum.Monthly:
                return tb.WithSchedule(CronScheduleBuilder.MonthlyOnDayAndHourAndMinute(task.MonthDay!.Value, task.TimeOfDay!.Value.Hour, task.TimeOfDay.Value.Minute).InTimeZone(tz)).Build();
            case PeriodicityTypeEnum.Cron:
                return tb.WithSchedule(CronScheduleBuilder.CronSchedule(task.CronExpression!).InTimeZone(tz)).Build();
            default:
                return null;
        }
    }
}
