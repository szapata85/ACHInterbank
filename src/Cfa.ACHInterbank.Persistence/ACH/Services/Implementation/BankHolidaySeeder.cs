using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class BankHolidaySeeder : IBankHolidaySeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IHolidayStrategyFactory _strategyFactory;

    public BankHolidaySeeder(ApplicationDbContext context, IHolidayStrategyFactory strategyFactory)
    {
        _context = context;
        _strategyFactory = strategyFactory;
    }

    public async Task SeedHolidaysIfNotExistsAsync(int year)
    {
        if (await _context.BankHolidays.AnyAsync(h => h.Date.Year == year))
            return;

        var clearingHouseIds = await _context.ClearingHouses.Select(ch => ch.Id).ToListAsync();

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


    public async Task<List<BankHoliday>> GetHolidaysForClearingHouseAsync(int clearingHouseId, int year)
    {
        return await _context.BankHolidays
            .Where(h => h.Date.Year == year)
            .ToListAsync();
    }
}
