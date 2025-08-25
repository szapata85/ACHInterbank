using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBankHoliday
{
    Task SeedHolidaysIfNotExistsAsync(int year);
    Task<List<BankHolidayModel>> GetHolidaysForClearingHouseAsync(int clearingHouseId, int year);
    List<BankHolidayModel> GetHolidays(int year);

    bool IsHoliday(DateOnly date, string countryCode);
    bool IsBusinessDay(DateOnly date, string countryCode);
    DateOnly NextBusinessDay(DateOnly date, string countryCode);
}
