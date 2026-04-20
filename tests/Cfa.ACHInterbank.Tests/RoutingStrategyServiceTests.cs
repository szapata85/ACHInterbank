using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class RoutingStrategyServiceTests
{
    [Fact]
    public async Task ResolveClearingHouseForTransactionAsync_AfterCutoff_UsesNextAvailableCycle()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);

        var today = DateTime.Today;
        SeedRoutingData(context, today);

        var holidayService = new Mock<IBankHoliday>();
        holidayService.Setup(h => h.GetHolidays(It.IsAny<int>())).Returns(new List<BankHolidayModel>());

        var scheduler = new Mock<IAchCycleScheduler>();
        scheduler
            .Setup(s => s.ScheduleCyclesForClearingHouseAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var service = new RoutingStrategyService(context, holidayService.Object, scheduler.Object);

        var resolved = await service.ResolveClearingHouseForTransactionAsync(2, today.AddHours(18).AddMinutes(5), CancellationToken.None);

        var expected = context.AchCycles
            .Single(c => c.ProcessingDate == today.AddDays(1) && c.CycleName == "CICLO-1")
            .Id;

        Assert.Equal(expected, resolved);
    }

    [Fact]
    public async Task ResolveClearingHouseForTransactionAsync_WithinDayWindow_UsesCurrentDayCycle()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);

        var today = DateTime.Today;
        SeedRoutingData(context, today);

        var holidayService = new Mock<IBankHoliday>();
        holidayService.Setup(h => h.GetHolidays(It.IsAny<int>())).Returns(new List<BankHolidayModel>());

        var scheduler = new Mock<IAchCycleScheduler>();
        scheduler
            .Setup(s => s.ScheduleCyclesForClearingHouseAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
            .Returns(Task.CompletedTask);

        var service = new RoutingStrategyService(context, holidayService.Object, scheduler.Object);

        var resolved = await service.ResolveClearingHouseForTransactionAsync(2, today.AddHours(15), CancellationToken.None);

        var expected = context.AchCycles
            .Single(c => c.ProcessingDate == today && c.CycleName == "CICLO-4")
            .Id;

        Assert.Equal(expected, resolved);
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedRoutingData(AchDbContext context, DateTime baseDate)
    {
        var config = new ClearingHouseConfig
        {
            Id = 1,
                                                        };

        var clearingHouse = new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "00112233",
            ClearingHouseId = 1,
            ClearingHouseConfig = config
        };

        var fi = new FinancialInstitution
        {
            Id = 2,
            Name = "Banco Destino",
            RoutingNumber = "7654321",
            TransitCode = "0",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = false
        };
        fi.CalculateCheckDigit();

        fi.ClearingHousePreferences.Add(new InstitutionClearingHousePreference
        {
            ClearingHouseId = 1,
            IsActive = true,
            IsDefault = true,
            Priority = 1
        });

        context.ClearingHouseConfigs.Add(config);
        context.ClearingHouses.Add(clearingHouse);
        context.FinancialInstitutions.Add(fi);

        foreach (var (name, start, end, cutoff) in BuildCycleDefinitions())
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = $"{baseDate:yyyyMMdd}-T-{name}",
                CycleName = name,
                ProcessingDate = baseDate,
                StartTime = start,
                EndTime = end,
                CutoffTime = cutoff,
                RescheduleOnHoliday = false,
                ClearingHouseId = 1
            });

            context.AchCycles.Add(new AchCycle
            {
                Id = $"{baseDate.AddDays(1):yyyyMMdd}-T-{name}",
                CycleName = name,
                ProcessingDate = baseDate.AddDays(1),
                StartTime = start,
                EndTime = end,
                CutoffTime = cutoff,
                RescheduleOnHoliday = false,
                ClearingHouseId = 1
            });
        }

        context.SaveChanges();
    }

    private static IEnumerable<(string Name, TimeSpan Start, TimeSpan End, TimeSpan Cutoff)> BuildCycleDefinitions()
    {
        yield return ("CICLO-1", new TimeSpan(19, 1, 0), new TimeSpan(8, 30, 0), new TimeSpan(8, 30, 0));
        yield return ("CICLO-2", new TimeSpan(8, 31, 0), new TimeSpan(11, 0, 0), new TimeSpan(11, 0, 0));
        yield return ("CICLO-3", new TimeSpan(11, 1, 0), new TimeSpan(14, 0, 0), new TimeSpan(14, 0, 0));
        yield return ("CICLO-4", new TimeSpan(14, 1, 0), new TimeSpan(16, 0, 0), new TimeSpan(16, 0, 0));
        yield return ("CICLO-5", new TimeSpan(16, 1, 0), new TimeSpan(18, 0, 0), new TimeSpan(18, 0, 0));
    }
}
