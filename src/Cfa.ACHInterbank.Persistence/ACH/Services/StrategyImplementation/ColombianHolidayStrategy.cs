using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;

public class ColombianHolidayStrategy : IHolidayStrategy
{
    public List<BankHolidayModel> GenerateHolidays(int year)
    {
        var holidays = new List<BankHolidayModel>
        {
            new BankHolidayModel { Date = new DateOnly(year, 1, 1), Description  = "New Year's Day" },
            new BankHolidayModel { Date = new DateOnly(year, 5, 1), Description  = "Labor Day" },
            new BankHolidayModel { Date = new DateOnly(year, 7, 20), Description  = "Independence Day" },
            new BankHolidayModel { Date = new DateOnly(year, 8, 7), Description  = "Battle of Boyacá" },
            new BankHolidayModel { Date = new DateOnly(year, 12, 25), Description  = "Christmas" },
        };

        return holidays;
    }
}
