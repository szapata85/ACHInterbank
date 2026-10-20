using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.Scheduler;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class SchedulerTaskDefinitionSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent_AndPersistFunctionalMetadataAndSafeManualEvaluation()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(nameof(SeedAsync_ShouldBeIdempotent_AndPersistFunctionalMetadataAndSafeManualEvaluation))
            .Options;
        await using var context = new AchDbContext(options);
        var seeder = new TaskDefinitionSeeder(context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var tasks = await context.TaskDefinitions.AsNoTracking().ToListAsync();
        tasks.Should().HaveCount(9);
        tasks.Select(x => x.Code).Should().OnlyHaveUniqueItems();
        tasks.Single(x => x.Code == "SeedBankHolidays").Name.Should().Be("Actualizar días festivos");
        tasks.Single(x => x.Code == "AchCycleSeeder").Name.Should().Be("Actualizar ciclos de compensación");
        var cycleTask = tasks.Single(x => x.Code == "AchCycleScheduler");
        cycleTask.CalendarPolicy.Should().Be(CalendarPolicyEnum.IgnoreCalendar);
        cycleTask.MisfirePolicy.Should().Be(SchedulerMisfirePolicy.FireAndProceed);
        cycleTask.TimeOfDay.Should().Be(new TimeOnly(2, 0));
        var evaluator = new QuartzTaskCalendarEvaluator();
        foreach (var instant in new[]
                 {
                     new DateTimeOffset(2026, 10, 17, 7, 0, 0, TimeSpan.Zero), // sábado 02:00 Bogotá
                     new DateTimeOffset(2026, 10, 18, 7, 0, 0, TimeSpan.Zero), // domingo 02:00 Bogotá
                     new DateTimeOffset(2026, 11, 16, 7, 0, 0, TimeSpan.Zero)  // lunes festivo Colombia
                 })
        {
            evaluator.Evaluate(cycleTask, context, instant).ShouldRun.Should().BeTrue();
        }
        var contrapartidas = tasks.Single(x => x.Code == "AchContrapartidasByCycle");
        contrapartidas.ManualExecutionEnabled.Should().BeTrue();
        contrapartidas.RetryOnFailure.Should().BeFalse();
        var transacciones = tasks.Single(x => x.Code == "IncomingNachaPostProcessing");
        transacciones.ManualExecutionEnabled.Should().BeTrue();
        transacciones.RetryOnFailure.Should().BeFalse();
        var outboundMft = tasks.Single(x => x.Code == "AchColombiaManagedMftOutbound");
        outboundMft.ManualExecutionEnabled.Should().BeFalse();
        outboundMft.ConcurrencyPolicy.Should().Be(ConcurrencyPolicyEnum.SkipIfRunning);
        outboundMft.PeriodicityType.Should().Be(PeriodicityTypeEnum.EveryNMinutes);
        outboundMft.N.Should().Be(5); // metadata legacy conservada; SchedulerSyncService no la usa como trigger primario ACHCOL outbound.
        SchedulerTaskCatalog.ByHandlerCode("AchColombiaManagedMftOutbound")!.UsesCycleSchedule.Should().BeFalse();
        var inboundMft = tasks.Single(x => x.Code == "AchColombiaManagedMftInbound");
        inboundMft.ManualExecutionEnabled.Should().BeFalse();
        inboundMft.ConcurrencyPolicy.Should().Be(ConcurrencyPolicyEnum.SkipIfRunning);
        inboundMft.PeriodicityType.Should().Be(PeriodicityTypeEnum.EveryNMinutes);
        inboundMft.N.Should().Be(3);
        SchedulerTaskCatalog.ByHandlerCode("AchColombiaManagedMftInbound")!.UsesCycleSchedule.Should().BeFalse();
    }
    [Fact]
    public async Task SeedAsync_ExistingCycleScheduler_RepairsWeekendCalendarAndRecoveryWithoutDuplicatingTask()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(nameof(SeedAsync_ExistingCycleScheduler_RepairsWeekendCalendarAndRecoveryWithoutDuplicatingTask))
            .Options;
        await using var context = new AchDbContext(options);
        context.TaskDefinitions.Add(new TaskDefinition
        {
            Code = "AchCycleScheduler",
            Name = "Legacy calendar",
            CalendarPolicy = CalendarPolicyEnum.OnlyBusinessDays,
            MisfirePolicy = SchedulerMisfirePolicy.DoNothing,
            PeriodicityType = PeriodicityTypeEnum.DailyAtTime,
            TimeOfDayTicks = new TimeOnly(2, 0).Ticks
        });
        await context.SaveChangesAsync();
        var seeder = new TaskDefinitionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var tasks = await context.TaskDefinitions
            .Where(task => task.Code == "AchCycleScheduler")
            .ToListAsync();
        var task = Assert.Single(tasks);
        Assert.Equal(CalendarPolicyEnum.IgnoreCalendar, task.CalendarPolicy);
        Assert.Equal(SchedulerMisfirePolicy.FireAndProceed, task.MisfirePolicy);
    }
}
