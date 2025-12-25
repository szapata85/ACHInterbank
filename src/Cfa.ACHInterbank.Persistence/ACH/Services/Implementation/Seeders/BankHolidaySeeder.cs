using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class BankHolidaySeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public BankHolidaySeeder(AchDbContext context)
    {
        _context = context;
    }

    int IDbSeeder.Order => 3;

    public async Task SeedAsync()
    {
        if (!_context.BankHolidays.Any())
        {
            var strategy = new ColombianHolidayStrategy();
            var year = DateTime.Now.Year;
            var holidays = new List<BankHolidayModel>();

            holidays.AddRange(strategy.GenerateHolidays(year));
            holidays.AddRange(strategy.GenerateHolidays(year + 1));

            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _context.BankHolidays.AddRange(holidays);

            await _context.SaveChangesAsync();

            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
