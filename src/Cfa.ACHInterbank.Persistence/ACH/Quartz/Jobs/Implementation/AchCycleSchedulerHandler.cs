using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public class AchCycleSchedulerHandler : ITaskHandler
{
    private readonly IAchCycleScheduler _cycleScheduler;

    public AchCycleSchedulerHandler(IAchCycleScheduler cycleScheduler)
    {
        _cycleScheduler = cycleScheduler;
    }

    public string Code => "AchCycleScheduler";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        // Ejecuta para cada clearing house
        await _cycleScheduler.ScheduleCyclesForClearingHouseAsync(1); // ACH Colombia
        await _cycleScheduler.ScheduleCyclesForClearingHouseAsync(2); // CENIT

        return "Ciclos diarios generados para todas las cámaras de compensación";
    }
}

