using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchCycleScheduler
{
    Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId);
    Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId, DateTime processingDate);
    Task<List<AchCycle>> GetScheduledCyclesAsync(int clearingHouseId, DateTime date);

    DateTime GetNextValidProcessingDate(DateTime baseDate);
}
