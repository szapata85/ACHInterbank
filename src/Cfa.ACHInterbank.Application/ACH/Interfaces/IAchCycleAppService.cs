using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchCycleAppService
{
    Task<IEnumerable<AchCycleDto>> GetAsync(int? clearingHouseId = null, DateTime? processingDate = null, CancellationToken ct = default);
    Task<AchCycleDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AchCycleDto> CreateAsync(AchCycleRequest request, CancellationToken ct = default);
    Task<AchCycleDto> UpdateAsync(int id, AchCycleRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
