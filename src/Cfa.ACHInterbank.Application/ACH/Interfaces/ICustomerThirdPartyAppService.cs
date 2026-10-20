using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICustomerThirdPartyAppService
{
    Task<PagedResponse<CustomerThirdPartyListDto>> GetAsync(CustomerThirdPartyQuery query, CancellationToken ct = default);
}
