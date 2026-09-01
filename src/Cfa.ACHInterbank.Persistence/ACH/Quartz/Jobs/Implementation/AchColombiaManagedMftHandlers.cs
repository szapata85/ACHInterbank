using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public sealed class AchColombiaManagedMftOutboundHandler(
    AchDbContext context,
    IAchColombiaManagedFileExchangeService service) : ITaskHandler
{
    public string Code => "AchColombiaManagedMftOutbound";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var cycles = await context.AchCycles.AsNoTracking()
            .Where(x => x.ClearingHouse!.Code == "ACHCOL" && x.ProcessingDate >= DateTime.UtcNow.Date.AddDays(-1))
            .OrderBy(x => x.ProcessingDate).ThenBy(x => x.CutoffTime).Select(x => x.Id).Take(20).ToListAsync(cancellationToken);
        var succeeded = 0;
        var failed = 0;
        foreach (var cycleId in cycles)
        {
            var result = await service.ExecuteOutboundAsync(cycleId, AchManagedFileExecutionOrigin.Automatic,
                $"task:{Code}", $"task:{Code}:{cycleId}", cancellationToken);
            succeeded += result.Succeeded;
            failed += result.Failed;
        }
        return $"Intercambio saliente ACH Colombia: ciclos={cycles.Count}, enviados={succeeded}, fallidos={failed}.";
    }
}

public sealed class AchColombiaManagedMftInboundHandler(IAchColombiaManagedFileExchangeService service) : ITaskHandler
{
    public string Code => "AchColombiaManagedMftInbound";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var result = await service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Automatic,
            $"task:{Code}", $"task:{Code}:{DateTime.UtcNow:yyyyMMddHHmm}", cancellationToken);
        return $"Intercambio entrante ACH Colombia: recibidos={result.Processed}, procesados={result.Succeeded}, rechazados={result.Failed}.";
    }
}
