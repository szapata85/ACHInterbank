using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IHolidayStrategy
{
    List<BankHolidayModel> GenerateHolidays(int year);
}
