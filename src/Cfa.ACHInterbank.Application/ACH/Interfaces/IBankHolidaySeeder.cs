using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBankHolidaySeeder
{
    Task SeedHolidaysIfNotExistsAsync(int year);
    Task<List<BankHoliday>> GetHolidaysForClearingHouseAsync(int clearingHouseId, int year);
}
