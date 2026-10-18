using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IThirdPartyAccountBackfillService
{
    Task<ThirdPartyAccountBackfillSummary> RunAsync(
        ThirdPartyAccountBackfillRequest request,
        CancellationToken ct = default);
}
