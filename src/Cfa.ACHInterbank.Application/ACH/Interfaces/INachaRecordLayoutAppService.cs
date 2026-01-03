using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaRecordLayoutAppService
{
    Task<IReadOnlyList<NachaRecordLayoutDto>> GetAllAsync(CancellationToken ct = default);
    Task<NachaRecordLayoutDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<NachaRecordLayoutDto> CreateAsync(NachaRecordLayoutDto request, CancellationToken ct = default);
    Task<NachaRecordLayoutDto?> UpdateAsync(int id, NachaRecordLayoutDto request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
