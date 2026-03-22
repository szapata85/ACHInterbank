using Cfa.ACHInterbank.Domain.Entities.ACH.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IReturnReasonService
{
    Task<IEnumerable<ReturnReasonDto>> GetAllAsync(bool onlyForReturn = false, CancellationToken ct = default);
}
