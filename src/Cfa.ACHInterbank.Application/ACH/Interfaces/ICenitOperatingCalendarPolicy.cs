using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitOperatingCalendarPolicy
{
    Task ValidateCycleConsistencyAsync(int clearingHouseId, DateTime processingDate, CancellationToken ct);
    Task<AchCycle> ResolveTargetCycleAsync(int clearingHouseId, DateTime receivedAtUtc, CancellationToken ct);
}
