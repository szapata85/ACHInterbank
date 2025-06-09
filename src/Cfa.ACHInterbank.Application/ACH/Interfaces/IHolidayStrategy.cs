using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IHolidayStrategy
{
    List<BankHoliday> GenerateHolidays(int year);
}
