using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyAccountShadowComparisonService
{
    Task<ThirdPartyAccountShadowComparisonSummary> CompareLegacyReadAsync(
        CustomerThirdPartyQuery query,
        IReadOnlyCollection<int> legacyThirdPartyIds,
        int legacyTotal,
        CancellationToken ct = default);
}
