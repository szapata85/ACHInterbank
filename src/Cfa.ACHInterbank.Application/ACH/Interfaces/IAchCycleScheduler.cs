using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchCycleScheduler
{
    Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId);
    Task<List<AchCycle>> GetScheduledCyclesAsync(int clearingHouseId, DateTime date);

    DateTime GetNextValidProcessingDate(DateTime baseDate);
}
