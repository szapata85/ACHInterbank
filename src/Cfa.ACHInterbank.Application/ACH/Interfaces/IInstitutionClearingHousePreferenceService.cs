using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IInstitutionClearingHousePreferenceService
{
    Task<IEnumerable<InstitutionClearingHousePreferenceDto>> GetAllAsync(CancellationToken ct = default);

    Task<InstitutionClearingHousePreferenceDto> UpdateAsync(InstitutionClearingHousePreferenceDto dto, CancellationToken ct = default);

    Task<InstitutionClearingHousePreferenceDto> CreateAsync(InstitutionClearingHousePreferenceDto dto, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
