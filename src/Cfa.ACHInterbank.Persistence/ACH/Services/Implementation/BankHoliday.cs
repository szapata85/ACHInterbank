using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Singleton]
public class BankHoliday : IBankHoliday
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BankHoliday(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task SeedHolidaysIfNotExistsAsync(int year)
    {
        using var scope = _scopeFactory.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<IBankHolidayProvisioningService>();
        await provisioning.EnsureYearsAsync([year]);
    }


    public async Task<List<Domain.Models.ACH.BankHolidayModel>> GetHolidaysForClearingHouseAsync(int clearingHouseId, int year)
    {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            AchDbContext _context = scope.ServiceProvider.GetRequiredService<AchDbContext>();

            return await _context.BankHolidays.AsNoTracking()
            .Where(h => h.Date.Year == year)
            .OrderBy(h => h.Date)
            .ToListAsync();
        }
    }

    public List<BankHolidayModel> GetHolidays(int year)
    {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            AchDbContext _context = scope.ServiceProvider.GetRequiredService<AchDbContext>();

            return _context.BankHolidays.AsNoTracking()
            .Where(h => h.Date.Year == year)
            .OrderBy(h => h.Date)
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
