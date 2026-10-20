using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseSpecialDateService
{
    Task<IReadOnlyList<ClearingHouseSpecialDateDto>> GetAllAsync(int? year, int? clearingHouseId, CancellationToken ct = default);
    Task<ClearingHouseSpecialDateDto> CreateAsync(ClearingHouseSpecialDateDto dto, CancellationToken ct = default);
    Task<ClearingHouseSpecialDateDto> UpdateAsync(ClearingHouseSpecialDateDto dto, CancellationToken ct = default);
    Task<ClearingHouseSpecialDateDto> ChangeStatusAsync(int id, bool isActive, CancellationToken ct = default);
}
