using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICustomerThirdPartyAppService
{
    Task<PagedResponse<CustomerThirdPartyListDto>> GetAsync(CustomerThirdPartyQuery query, CancellationToken ct = default);
    Task<CustomerThirdPartyListDto> UpdateStatusAsync(
        int id,
        CustomerThirdPartyStatusEnum status,
        string? validationMessage,
        CancellationToken ct = default);
}
