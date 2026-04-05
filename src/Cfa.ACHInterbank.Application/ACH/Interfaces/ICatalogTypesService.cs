using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICatalogTypesService
{
    Task<IReadOnlyList<CatalogTypeItemDto>> GetAllAsync(string catalogType, CancellationToken ct = default);
    Task<CatalogTypeItemDto> CreateAsync(string catalogType, CatalogTypeUpsertRequest request, CancellationToken ct = default);
    Task<CatalogTypeItemDto> UpdateAsync(string catalogType, string code, CatalogTypeUpsertRequest request, CancellationToken ct = default);
    Task DeleteAsync(string catalogType, string code, CancellationToken ct = default);
}
