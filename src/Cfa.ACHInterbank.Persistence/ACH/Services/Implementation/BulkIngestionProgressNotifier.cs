using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Implementación no-op para mantener desacoplo del canal de notificación.
/// </summary>
[Scoped]
public sealed class BulkIngestionProgressNotifier : IBulkIngestionProgressNotifier
{
    public Task NotifyBatchProgressAsync(Guid batchId, decimal progressPercent, string? message = null, CancellationToken ct = default)
        => Task.CompletedTask;
}
