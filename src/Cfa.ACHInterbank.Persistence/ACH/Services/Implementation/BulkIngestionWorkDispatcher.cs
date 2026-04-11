using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Adaptador actual basado en Quartz. Permite migración futura a colas distribuidas
/// sin impactar servicios de dominio.
/// </summary>
[Scoped]
public sealed class BulkIngestionWorkDispatcher : IBulkIngestionWorkDispatcher
{
    private readonly IAchBulkJobScheduler _jobScheduler;

    public BulkIngestionWorkDispatcher(IAchBulkJobScheduler jobScheduler)
    {
        _jobScheduler = jobScheduler;
    }

    public Task<string> DispatchProcessingAsync(Guid batchId, long? attemptId = null, CancellationToken ct = default)
        => _jobScheduler.EnqueueBatchAsync(batchId, attemptId, ct);
}
