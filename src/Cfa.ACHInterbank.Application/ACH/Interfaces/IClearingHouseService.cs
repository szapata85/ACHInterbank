using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseService
{
    Task<IEnumerable<ClearingHouseDto>> GetAllAsync(CancellationToken ct = default);
    Task<ClearingHouseDto?> GetByIdAsync(int id, CancellationToken ct = default);
}
