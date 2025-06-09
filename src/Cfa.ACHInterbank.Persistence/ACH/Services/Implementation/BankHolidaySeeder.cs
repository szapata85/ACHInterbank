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

        var strategy = _strategyFactory.GetStrategyForClearingHouse(0); // Default or general
        var holidays = strategy.GenerateHolidays(year);
        await _context.BankHolidays.AddRangeAsync(holidays);
        await _context.SaveChangesAsync();
    }

    public async Task<List<BankHoliday>> GetHolidaysForClearingHouseAsync(int clearingHouseId, int year)
    {
        return await _context.BankHolidays
            .Where(h => h.Date.Year == year)
            .ToListAsync();
    }
}
