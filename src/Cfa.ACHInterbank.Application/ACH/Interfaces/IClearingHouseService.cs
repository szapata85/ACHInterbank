using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseService
{
    Task<IEnumerable<ClearingHouseDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ClearingHouseDto>> GetOperationalAsync(CancellationToken ct = default);
    Task<ClearingHouseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PaginatedResult<ClearingHouseDto>> GetAsync(ClearingHouseAdminQuery request, CancellationToken ct = default);
    Task<ClearingHouseDto> CreateAsync(CreateClearingHouseRequest request, CancellationToken ct = default);
    Task<ClearingHouseDto> UpdateAsync(int id, UpdateClearingHouseRequest request, CancellationToken ct = default);
    Task<ClearingHouseDto> ChangeStatusAsync(int id, bool isActive, CancellationToken ct = default);
    Task<ClearingHouseReadinessDto> GetReadinessAsync(int id, CancellationToken ct = default);
    IReadOnlyList<ClearingHousePaymentRailOptionDto> GetPaymentRailOptions();
    Task<IReadOnlyList<ClearingHouseNachaProfileOptionDto>> GetNachaProfilesAsync(string? clearingHouseCode, CancellationToken ct = default);
}
