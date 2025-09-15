using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IFinancialInstitutionService
{
    Task<IEnumerable<FinancialInstitutionDto>> GetAllAsync();
    Task<FinancialInstitutionDto?> GetByIdAsync(int id);
    Task<FinancialInstitutionDto> CreateAsync(FinancialInstitutionDto dto);
    Task UpdateAsync(int id, FinancialInstitutionDto dto);
    Task DeleteAsync(int id);
}

