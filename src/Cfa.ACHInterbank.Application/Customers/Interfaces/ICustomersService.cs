using Cfa.ACHInterbank.Application.Customers.Dtos;

namespace Cfa.ACHInterbank.Application.Customers.Interfaces;

public interface ICustomersService
{
    Task<IEnumerable<CustomerSummaryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CustomerDetailDto> CreateAsync(SaveCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDetailDto?> UpdateAsync(int id, SaveCustomerRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
