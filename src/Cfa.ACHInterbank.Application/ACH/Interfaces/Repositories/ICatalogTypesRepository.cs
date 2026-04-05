using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;

public interface ICatalogTypesRepository
{
    Task<IReadOnlyList<CatalogTypeItemDto>> ListAsync(CatalogTypeKey type, CancellationToken ct = default);
    Task<bool> ExistsAsync(CatalogTypeKey type, string code, CancellationToken ct = default);
    Task AddAsync(CatalogTypeKey type, string code, string name, string? description, CancellationToken ct = default);
    Task<bool> UpdateAsync(CatalogTypeKey type, string code, string name, string? description, CancellationToken ct = default);
    Task<bool> RemoveAsync(CatalogTypeKey type, string code, CancellationToken ct = default);
}
