using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseService
{
    Task<IEnumerable<ClearingHouseDto>> GetAllAsync(CancellationToken ct = default);
    Task<ClearingHouseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PaginatedResult<ClearingHouseDto>> GetAsync(PaginationRequest request, CancellationToken ct = default);
}
