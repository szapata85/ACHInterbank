using System;
using System.IO;
using System.Reflection;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

public class SchedulerSyncServiceTests
{
    [Fact]
    public void SchedulerSyncService_ShouldNotThrow_WhenTimeZoneInvalid()
    {
        var service = new SchedulerSyncService(
            new ServiceCollection().BuildServiceProvider(),
            Mock.Of<ISchedulerFactory>(),
            NullLogger<SchedulerSyncService>.Instance,
            new QuartzTaskCalendarEvaluator());

        var task = new TaskDefinition
        {
            Id = 1,
            Code = "Test",
            TimeZoneId = "Invalid/TZ",
            PeriodicityType = PeriodicityTypeEnum.Cron,
            CronExpression = "0 0 12 * * ?"
        };

        var method = typeof(SchedulerSyncService).GetMethod("BuildTrigger", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var action = () => method.Invoke(service, new object[] { task, new TriggerKey("trg:1", "db-tasks") });

        action.Should().NotThrow();
    }

    [Fact]
    public void DynamicJob_ShouldNotUseRescheduleJob_ForShiftToNextBusinessDay()
    {
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        while (probe is not null && !Directory.Exists(Path.Combine(probe.FullName, "src")))
        {
            probe = probe.Parent;
        }

        probe.Should().NotBeNull("debe resolverse la raíz del repositorio para validar el guardrail");
        var dynamicJobPath = Path.Combine(probe!.FullName, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Quartz", "Jobs", "DynamicJob.cs");

        File.Exists(dynamicJobPath).Should().BeTrue($"debe existir {dynamicJobPath}");
        var content = File.ReadAllText(dynamicJobPath);

        content.Should().NotContain("RescheduleJob");
        content.Should().NotContain("shifted:");
        content.Should().Contain("ShiftToNextBusinessDay");
    }
}
