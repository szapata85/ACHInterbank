using System.Reflection;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz;

public sealed class SchedulerRuntimeService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly SchedulerMisfireListener _misfireListener;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SchedulerRuntimeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly QuartzJobStoreOptions _options;
    private string? _instanceId;

    public SchedulerRuntimeService(
        IServiceProvider services,
        ISchedulerFactory schedulerFactory,
        SchedulerMisfireListener misfireListener,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<SchedulerRuntimeService> logger)
    {
        _services = services;
        _schedulerFactory = schedulerFactory;
        _misfireListener = misfireListener;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
        _options = QuartzJobStoreOptionsFactory.Create(configuration);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);
        _instanceId = scheduler.SchedulerInstanceId;
        scheduler.ListenerManager.AddTriggerListener(_misfireListener);

        await EnsureProbeTaskAsync(stoppingToken);
        await EnsureReprocessDispatcherTaskAsync(stoppingToken);

        _logger.LogInformation(
            "Scheduler iniciado; SchedulerName={SchedulerName}; SchedulerInstanceId={SchedulerInstanceId}; InstanceName={InstanceName}; Host={Host}; Persistent={Persistent}; Clustered={Clustered}",
            scheduler.SchedulerName,
            scheduler.SchedulerInstanceId,
            _options.InstanceName,
            Environment.MachineName,
            _options.IsPersistentMode(),
            _options.Clustered);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WriteHeartbeatAsync(scheduler, "En línea", stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el heartbeat funcional de la instancia Quartz {SchedulerInstanceId}.", scheduler.SchedulerInstanceId);
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, _options.InstanceHeartbeatSeconds)), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await WriteHeartbeatAsync(scheduler, "Deteniéndose", cancellationToken, stopped: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo persistir el estado de detención de Quartz {SchedulerInstanceId}.", _instanceId);
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task WriteHeartbeatAsync(IScheduler scheduler, string status, CancellationToken cancellationToken, bool stopped = false)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
        var now = DateTimeOffset.UtcNow;
        var state = await db.SchedulerInstanceStates
            .SingleOrDefaultAsync(
                x => x.SchedulerName == scheduler.SchedulerName && x.InstanceId == scheduler.SchedulerInstanceId,
                cancellationToken);

        if (state is null)
        {
            state = new SchedulerInstanceState
            {
                SchedulerName = scheduler.SchedulerName,
                InstanceId = scheduler.SchedulerInstanceId,
                StartedAtUtc = now
            };
            db.SchedulerInstanceStates.Add(state);
        }

        state.InstanceName = _options.InstanceName;
        state.HostName = Environment.MachineName;
        state.LastHeartbeatUtc = now;
        state.StoppedAtUtc = stopped ? now : null;
        state.Status = status;
        state.CurrentlyExecutingJobs = (await scheduler.GetCurrentlyExecutingJobs(cancellationToken)).Count;
        state.Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (state.Id == 0)
        {
            db.Entry(state).State = EntityState.Detached;
        }
    }

    private async Task EnsureProbeTaskAsync(CancellationToken cancellationToken)
    {
        var enabled = _configuration.GetValue<bool>("Scheduler:Probe:Enabled");
        if (!enabled || _environment.IsProduction())
        {
            return;
        }

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
        if (await db.TaskDefinitions.AnyAsync(x => x.Code == "SCHEDULER_CLUSTER_PROBE", cancellationToken))
        {
            return;
        }

        var task = new TaskDefinition
        {
            Code = "SCHEDULER_CLUSTER_PROBE",
            Name = "Sonda técnica de clúster",
            Description = "Prueba controlada de adquisición, idempotencia y recuperación.",
            Status = TaskStatusEnum.Enabled,
            CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar,
            ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
            RetryOnFailure = false,
            PeriodicityType = PeriodicityTypeEnum.Cron,
            CronExpression = "0 0 0 1 1 ? 2099",
            TimeZoneId = "America/Bogota",
            MisfirePolicy = SchedulerMisfirePolicy.FireAndProceed,
            RequestsRecovery = true,
            ManualExecutionEnabled = true,
            Parameters =
            [
                new TaskParameter { Key = "DurationSeconds", Value = "15" },
                new TaskParameter { Key = "RecoveryDurationSeconds", Value = "2" }
            ]
        };
        db.TaskDefinitions.Add(task);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(task).State = EntityState.Detached;
        }
    }

    private async Task EnsureReprocessDispatcherTaskAsync(CancellationToken cancellationToken)
    {
        const string taskCode = "ach-response-reprocess-dispatcher";
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
        if (await db.TaskDefinitions.AnyAsync(x => x.Code == taskCode, cancellationToken))
        {
            return;
        }

        var task = new TaskDefinition
        {
            Code = taskCode,
            Name = "Dispatcher de reprocesos de respuestas ACH",
            Description = "Adquiere y ejecuta reprocesos pendientes con ownership y lease persistidos.",
            Status = TaskStatusEnum.Enabled,
            CalendarPolicy = CalendarPolicyEnum.IgnoreCalendar,
            ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
            RetryOnFailure = false,
            PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
            N = 1,
            TimeZoneId = "America/Bogota",
            MisfirePolicy = SchedulerMisfirePolicy.FireAndProceed,
            RequestsRecovery = true,
            ManualExecutionEnabled = true,
            StartAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Parameters =
            [
                new TaskParameter { Key = "BatchSize", Value = "50" },
                new TaskParameter { Key = "LeaseSeconds", Value = "120" }
            ]
        };
        db.TaskDefinitions.Add(task);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(task).State = EntityState.Detached;
        }
    }
}
