using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;

public interface IAchCustomerRepository
{
    Task<string> ResolveDocumentTypeCodeAsync(string preferredCode, CancellationToken ct = default);
    Task<string> ResolvePersonTypeCodeAsync(string preferredCode, CancellationToken ct = default);
    Task<Customer?> GetByDocumentAsync(string documentType, string documentNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetByDocumentNumberAsync(string documentNumber, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task<Customer?> FindBySourceAccountNumberAsync(string sourceAccountNumber, CancellationToken ct = default);
}
