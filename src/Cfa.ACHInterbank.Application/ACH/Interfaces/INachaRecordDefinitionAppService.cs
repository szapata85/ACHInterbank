using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaRecordDefinitionAppService
{
    Task<IReadOnlyList<NachaRecordDefinitionDto>> GetAllAsync(CancellationToken ct = default);
    Task<NachaRecordDefinitionDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<NachaRecordDefinitionDto> CreateAsync(NachaRecordDefinitionDto request, CancellationToken ct = default);
    Task<NachaRecordDefinitionDto?> UpdateAsync(int id, NachaRecordDefinitionDto request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
