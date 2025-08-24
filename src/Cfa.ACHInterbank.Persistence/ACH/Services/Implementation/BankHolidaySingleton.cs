using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class BankHolidaySingleton : IBankHolidaySingleton
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BankHolidaySingleton(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task SeedHolidaysIfNotExistsAsync(int year)
    {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            AchDbContext _context = scope.ServiceProvider.GetRequiredService<AchDbContext>();

            if (await _context.BankHolidays.AnyAsync(h => h.Date.Year == year))
                return;

            var clearingHouseIds = await _context.ClearingHouses.Select(ch => ch.Id).ToListAsync();

            IHolidayStrategyFactory _strategyFactory = scope.ServiceProvider.GetRequiredService<IHolidayStrategyFactory>();

            foreach (var chId in clearingHouseIds)
            {
                var strategy = _strategyFactory.GetStrategyForClearingHouse(chId);
                var holidays = strategy.GenerateHolidays(year);

                var existingDates = await _context.BankHolidays
                    .Where(h => h.Date.Year == year)
                    .Select(h => h.Date)
                    .ToListAsync();

                var newHolidays = holidays
                    .Where(h => !existingDates.Contains(h.Date))
                    .ToList();

                await _context.BankHolidays.AddRangeAsync(newHolidays);
            }

            await _context.SaveChangesAsync();
        }
    }


    public async Task<List<BankHoliday>> GetHolidaysForClearingHouseAsync(int clearingHouseId, int year)
    {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            AchDbContext _context = scope.ServiceProvider.GetRequiredService<AchDbContext>();

            return await _context.BankHolidays
            .Where(h => h.Date.Year == year)
            .ToListAsync();
        }
    }

    public List<BankHoliday> GetHolidays(int year)
    {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            AchDbContext _context = scope.ServiceProvider.GetRequiredService<AchDbContext>();

            return _context.BankHolidays
            .Where(h => h.Date.Year == year)
            .ToList();
        }
    }



    public bool IsHoliday(DateOnly d, string cc)
    {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            AchDbContext _context = scope.ServiceProvider.GetRequiredService<AchDbContext>();

            return _context.BankHolidays.Any(h => h.CountryCode == cc && h.Date == d);
        }
    }

    public bool IsBusinessDay(DateOnly d, string cc) =>
        d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday && !IsHoliday(d, cc);

    public DateOnly NextBusinessDay(DateOnly d, string cc)
    {
        var cur = d;
        while (!IsBusinessDay(cur, cc)) cur = cur.AddDays(1);
        return cur;
    }
}
