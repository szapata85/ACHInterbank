using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;

public class ColombianHolidayStrategy : IHolidayStrategy
{
    public List<BankHoliday> GenerateHolidays(int year)
    {
        var holidays = new List<BankHoliday>
        {
            new BankHoliday { Date = new DateTime(year, 1, 1), Description  = "New Year's Day" },
            new BankHoliday { Date = new DateTime(year, 5, 1), Description  = "Labor Day" },
            new BankHoliday { Date = new DateTime(year, 7, 20), Description  = "Independence Day" },
            new BankHoliday { Date = new DateTime(year, 8, 7), Description  = "Battle of Boyacá" },
            new BankHoliday { Date = new DateTime(year, 12, 25), Description  = "Christmas" },
        };

        return holidays;
    }
}
