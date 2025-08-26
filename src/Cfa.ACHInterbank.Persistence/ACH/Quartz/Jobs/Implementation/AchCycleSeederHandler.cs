using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public class AchCycleSeederHandler : ITaskHandler
{
    private readonly IAchCycleSeeder _cycleSeeder;

    public AchCycleSeederHandler(IAchCycleSeeder cycleSeeder)
    {
        _cycleSeeder = cycleSeeder;
    }

    public string Code => "AchCycleSeeder";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var year = DateTime.Now.Year;

        // sembramos ACH Colombia
        await _cycleSeeder.SeedCyclesIfNotExistsAsync(1, year);
        // sembramos CENIT
        await _cycleSeeder.SeedCyclesIfNotExistsAsync(2, year);

        // opcional: sembrar anticipadamente para el próximo año
        await _cycleSeeder.SeedCyclesIfNotExistsAsync(1, year + 1);
        await _cycleSeeder.SeedCyclesIfNotExistsAsync(2, year + 1);

        return $"Ciclos ACH Colombia y CENIT cargados/validados para {year} y {year + 1}";
    }
}

