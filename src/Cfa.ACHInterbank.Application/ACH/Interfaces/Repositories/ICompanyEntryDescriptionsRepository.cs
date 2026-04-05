using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;

public interface ICompanyEntryDescriptionsRepository
{
    Task<IReadOnlyList<CompanyEntryDescriptionAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByTermAsync(string term, int? excludingId = null, CancellationToken ct = default);
    Task<CompanyEntryDescriptionCatalog?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(CompanyEntryDescriptionCatalog entity, CancellationToken ct = default);
    Task RemoveAsync(CompanyEntryDescriptionCatalog entity, CancellationToken ct = default);
}
