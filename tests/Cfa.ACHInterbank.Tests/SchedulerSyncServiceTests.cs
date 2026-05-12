using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using System.Reflection;

public class SchedulerSyncServiceTests
{
    [Fact]
    public void SchedulerSyncService_ShouldNotThrow_WhenTimeZoneInvalid()
    {
        var service = new SchedulerSyncService(new ServiceCollection().BuildServiceProvider(), Mock.Of<ISchedulerFactory>(), NullLogger<SchedulerSyncService>.Instance, new QuartzTaskCalendarEvaluator());

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
}
