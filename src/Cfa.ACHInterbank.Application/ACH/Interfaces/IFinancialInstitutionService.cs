using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IFinancialInstitutionService
{
    Task<IEnumerable<FinancialInstitutionDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<FinancialInstitutionDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<FinancialInstitutionDto> CreateAsync(FinancialInstitutionDto dto, CancellationToken ct = default);

    Task<FinancialInstitutionDto> UpdateAsync(FinancialInstitutionDto dto, CancellationToken ct = default);

    Task SetStatusAsync(int id, FinancialInstitutionStatus newStatus, CancellationToken ct = default);
}
