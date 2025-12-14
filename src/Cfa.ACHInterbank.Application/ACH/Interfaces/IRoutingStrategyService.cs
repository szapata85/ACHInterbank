namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IRoutingStrategyService
{
    Task<string> ResolveClearingHouseForTransactionAsync(
        int destinationInstitutionId,
        DateTime now,
        CancellationToken ct);
}

