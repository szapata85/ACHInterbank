using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBankHolidayAdminService
{
    Task<IReadOnlyList<BankHolidayDto>> GetAllAsync(int? year, CancellationToken ct = default);
    Task<BankHolidayDto> CreateAsync(BankHolidayDto dto, CancellationToken ct = default);
    Task<BankHolidayDto> UpdateAsync(BankHolidayDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
