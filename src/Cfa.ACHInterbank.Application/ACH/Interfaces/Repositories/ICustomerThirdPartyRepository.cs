using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;

public interface ICustomerThirdPartyRepository
{
    Task<CustomerThirdParty?> FindAsync(int customerId, int destinationInstitutionId, string destinationAccountNumber, string recipientIdNumber, CancellationToken ct = default);
    Task AddAsync(CustomerThirdParty entity, CancellationToken ct = default);
}
