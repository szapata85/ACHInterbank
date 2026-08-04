using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IOperationalCalendarService
{
    Task<IReadOnlyList<BankHolidayModel>> GetNationalHolidaysAsync(int year, CancellationToken ct = default);
    Task<IReadOnlyList<ClearingHouseSpecialDate>> GetSpecialDatesAsync(int clearingHouseId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<bool> IsNationalHolidayAsync(DateOnly date, CancellationToken ct = default);
    Task<bool> IsBusinessDayAsync(DateOnly date, int clearingHouseId, CancellationToken ct = default);
    Task<DateOnly> GetNextBusinessDayAsync(DateOnly date, int clearingHouseId, CancellationToken ct = default);
    Task<DateOnly> GetPreviousBusinessDayAsync(DateOnly date, int clearingHouseId, CancellationToken ct = default);
    Task<DateOnly> ShiftBusinessDaysAsync(DateOnly date, int amount, int clearingHouseId, CancellationToken ct = default);
    Task<OperationalDayExplanation> ExplainDayAsync(DateOnly date, int clearingHouseId, CancellationToken ct = default);
}
