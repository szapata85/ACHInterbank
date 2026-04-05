using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICompanyEntryDescriptionsService
{
    Task<IReadOnlyList<CompanyEntryDescriptionAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<CompanyEntryDescriptionAdminDto> CreateAsync(CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default);
    Task<CompanyEntryDescriptionAdminDto> UpdateAsync(int id, CompanyEntryDescriptionUpsertRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
