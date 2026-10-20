using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseCycleConfigService
{
    Task<IReadOnlyList<ClearingHouseCycleConfigDto>> GetByClearingHouseAsync(int clearingHouseId, DateTime? effectiveAt, CancellationToken ct = default);
    Task<IReadOnlyList<ClearingHouseCycleConfigDto>> GetCurrentByClearingHouseAsync(int clearingHouseId, DateTime? effectiveAt, CancellationToken ct = default);
    Task<ClearingHouseCycleConfigDto> CreateVersionAsync(UpsertClearingHouseCycleConfigDto dto, CancellationToken ct = default);
    Task<ClearingHouseCycleConfigDto> InactivateAsync(int id, DateTime effectiveTo, CancellationToken ct = default);
    Task<ClearingHouseCycleConfigDto> ChangeStatusAsync(int id, bool isActive, DateTime? effectiveTo, CancellationToken ct = default);
}
