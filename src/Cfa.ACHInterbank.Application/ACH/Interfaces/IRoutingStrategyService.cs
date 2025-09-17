namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IRoutingStrategyService
{
    Task<int> ResolveClearingHouseForTransactionAsync(
        int destinationInstitutionId,
        DateTime now,
        CancellationToken ct);
}

