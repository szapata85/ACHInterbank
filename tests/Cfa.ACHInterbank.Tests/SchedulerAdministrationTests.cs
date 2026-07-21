using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Application.Scheduler.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Scheduler;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public sealed class SchedulerAdministrationTests
{
    [Fact]
    public async Task ManualExecution_ShouldRequireReasonAndRequestId()
    {
        var service = new Mock<ISchedulerAdminService>();
        var controller = BuildController(service.Object);

        var result = await controller.Execute("ACH_CYCLE_SCHEDULER", new ExecuteSchedulerTaskRequest(" ", Guid.Empty), CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ValidationProblemDetails>();
        controller.ModelState.ErrorCount.Should().Be(2);
        service.Verify(x => x.ExecuteNowAsync(It.IsAny<ExecuteSchedulerTaskCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ManualExecution_ShouldBeIdempotentForDuplicateRequestId()
    {
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        scheduler.Setup(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<JobDataMap>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await using var fixture = await SchedulerFixture.CreateAsync(scheduler);
        var requestId = Guid.NewGuid();
        var command = new ExecuteSchedulerTaskCommand("ACH_CYCLE_SCHEDULER", "Reproceso autorizado por Operaciones", requestId, "user-1", "operador", "corr-1");

        var first = await fixture.Service.ExecuteNowAsync(command);
        var second = await fixture.Service.ExecuteNowAsync(command);

        first.Outcome.Should().Be(ManualExecutionOutcome.Accepted);
        second.Outcome.Should().Be(ManualExecutionOutcome.Duplicate);
        second.ExecutionId.Should().Be(first.ExecutionId);
        scheduler.Verify(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<JobDataMap>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ManualExecution_ShouldRejectConcurrentNonParallelTask()
    {
        var scheduler = new Mock<IScheduler>();
        await using var fixture = await SchedulerFixture.CreateAsync(scheduler);
        var activeExecution = Guid.NewGuid();
        fixture.Db.TaskExecutionLogs.Add(new TaskExecutionLog
        {
            TaskDefinitionId = 1,
            ExecutionId = activeExecution,
            ExecutionKey = activeExecution.ToString("N"),
            TaskCode = "ACH_CYCLE_SCHEDULER",
            JobName = "job:1",
            JobGroup = "db-tasks",
            TriggerName = "existing",
            TriggerType = "Programada",
            IdempotencyKey = "existing",
            CorrelationId = "existing",
            Status = SchedulerExecutionStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            ScheduledAt = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.Service.ExecuteNowAsync(new ExecuteSchedulerTaskCommand(
            "ACH_CYCLE_SCHEDULER", "Reproceso autorizado por Operaciones", Guid.NewGuid(), "user-1", "operador", "corr-2"));

        result.Outcome.Should().Be(ManualExecutionOutcome.Conflict);
        result.ActiveExecutionId.Should().Be(activeExecution);
        scheduler.Verify(x => x.TriggerJob(It.IsAny<JobKey>(), It.IsAny<JobDataMap>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SchedulePreview_ShouldValidateCronTimeZoneAndMisfire()
    {
        var scheduler = new Mock<IScheduler>();
        await using var fixture = await SchedulerFixture.CreateAsync(scheduler);
        var valid = new SchedulerScheduleUpdateRequest(6, null, null, null, null, null,
            "0 30 6 ? * MON-FRI", "America/Bogota", SchedulerMisfirePolicy.DoNothing, true, null, null);

        var preview = await fixture.Service.PreviewScheduleAsync(valid);
        preview.NextExecutionsUtc.Should().HaveCount(5);

        var invalidCron = valid with { CronExpression = "not-a-cron" };
        var invalidZone = valid with { TimeZoneId = "Invalid/Zone" };
        var invalidMisfire = valid with { MisfirePolicy = (SchedulerMisfirePolicy)99 };
        await FluentActions.Awaiting(() => fixture.Service.PreviewScheduleAsync(invalidCron)).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => fixture.Service.PreviewScheduleAsync(invalidZone)).Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => fixture.Service.PreviewScheduleAsync(invalidMisfire)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void SchedulerEndpoints_ShouldUseSeparateFineGrainedPolicies()
    {
        PolicyOf(nameof(SchedulerController.GetTasks)).Should().Be(P1Policies.SchedulerView);
        PolicyOf(nameof(SchedulerController.GetHistory)).Should().Be(P1Policies.SchedulerHistoryView);
        PolicyOf(nameof(SchedulerController.Execute)).Should().Be(P1Policies.SchedulerExecute);
        PolicyOf(nameof(SchedulerController.UpdateSchedule)).Should().Be(P1Policies.SchedulerManageSchedule);
        PolicyOf(nameof(SchedulerController.Pause)).Should().Be(P1Policies.SchedulerPauseResume);
        PolicyOf(nameof(SchedulerController.GetInstances)).Should().Be(P1Policies.SchedulerViewInstances);
    }

    [Fact]
    public async Task AdministrativeOperations_ShouldAuditPauseResumeAndScheduleWithCorrelation()
    {
        var scheduler = new Mock<IScheduler>();
        scheduler.Setup(x => x.ScheduleJob(It.IsAny<IJobDetail>(), It.IsAny<IReadOnlyCollection<ITrigger>>(), true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        scheduler.Setup(x => x.GetJobDetail(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).ReturnsAsync((IJobDetail?)null);
        scheduler.Setup(x => x.PauseJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        scheduler.Setup(x => x.ResumeJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await using var fixture = await SchedulerFixture.CreateAsync(scheduler);

        fixture.SetAuditScope("Scheduler.Pause", "pause-correlation");
        (await fixture.Service.PauseAsync("ACH_CYCLE_SCHEDULER", "admin-id", "administrador")).Should().BeTrue();

        fixture.SetAuditScope("Scheduler.Resume", "resume-correlation");
        (await fixture.Service.ResumeAsync("ACH_CYCLE_SCHEDULER", "admin-id", "administrador")).Should().BeTrue();

        fixture.SetAuditScope("Scheduler.UpdateSchedule", "schedule-correlation");
        await fixture.Service.UpdateScheduleAsync(new SchedulerScheduleUpdateCommand(
            "ACH_CYCLE_SCHEDULER",
            new SchedulerScheduleUpdateRequest(6, null, null, null, null, null,
                "0 45 7 ? * MON-FRI", "America/Bogota", SchedulerMisfirePolicy.FireAndProceed, true, null, null),
            "admin-id", "administrador"));

        var audits = await fixture.Db.AuditLogs.Where(x => x.EntityName == nameof(TaskDefinition)).ToListAsync();
        audits.Should().Contain(x => x.Action == "Scheduler.Pause" && x.ChangedBy == "admin-id" && x.CorrelationId == "pause-correlation" && x.AfterJson!.Contains("Paused"));
        audits.Should().Contain(x => x.Action == "Scheduler.Resume" && x.ChangedBy == "admin-id" && x.CorrelationId == "resume-correlation" && x.AfterJson!.Contains("Paused"));
        audits.Should().Contain(x => x.Action == "Scheduler.UpdateSchedule" && x.ChangedBy == "admin-id" && x.CorrelationId == "schedule-correlation" && x.AfterJson!.Contains("CronExpression"));
    }

    private static string? PolicyOf(string methodName)
        => typeof(SchedulerController).GetMethod(methodName)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single().Policy;

    private static SchedulerController BuildController(ISchedulerAdminService service)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "operador")], "test");
        return new SchedulerController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } }
        };
    }

    private sealed class SchedulerFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly HttpContext _httpContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AchDbContext Db { get; }
        public SchedulerAdminService Service { get; }
        public SchedulerSyncService Sync { get; }

        private SchedulerFixture(ServiceProvider provider, AchDbContext db, SchedulerAdminService service, SchedulerSyncService sync, HttpContext httpContext, IHttpContextAccessor httpContextAccessor)
        {
            _provider = provider;
            Db = db;
            Service = service;
            Sync = sync;
            _httpContext = httpContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetAuditScope(string action, string correlationId)
        {
            _httpContextAccessor.HttpContext = _httpContext;
            _httpContext.Items[AchDbContext.AuditActionItemKey] = action;
            _httpContext.Items[AchDbContext.AuditCorrelationItemKey] = correlationId;
        }

        public static async Task<SchedulerFixture> CreateAsync(Mock<IScheduler> scheduler)
        {
            var factory = new Mock<ISchedulerFactory>();
            factory.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler.Object);
            var services = new ServiceCollection()
                .AddHttpContextAccessor()
                .AddDbContext<AchDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()))
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                    new Claim(ClaimTypes.Name, "administrador")], "test"))
            };
            var httpContextAccessor = services.GetRequiredService<IHttpContextAccessor>();
            httpContextAccessor.HttpContext = httpContext;
            var db = services.GetRequiredService<AchDbContext>();
            db.TaskDefinitions.Add(new TaskDefinition
            {
                Id = 1,
                Code = "AchCycleScheduler",
                Name = "Programador de ciclos",
                Description = "Tarea segura de prueba",
                Status = TaskStatusEnum.Enabled,
                ManualExecutionEnabled = true,
                ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
                PeriodicityType = PeriodicityTypeEnum.Cron,
                CronExpression = "0 30 6 ? * MON-FRI",
                TimeZoneId = "America/Bogota"
            });
            await db.SaveChangesAsync();
            var sync = new SchedulerSyncService(services, factory.Object, NullLogger<SchedulerSyncService>.Instance, new QuartzTaskCalendarEvaluator());
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Quartz:SchedulerName"] = "ACHInterbankScheduler",
                ["Quartz:InstanceName"] = "test-instance",
                ["Quartz:JobStore:Mode"] = "RAM"
            }).Build();
            var service = new SchedulerAdminService(db, factory.Object, sync, configuration);
            return new SchedulerFixture(services, db, service, sync, httpContext, httpContextAccessor);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _provider.DisposeAsync();
        }
    }
}
