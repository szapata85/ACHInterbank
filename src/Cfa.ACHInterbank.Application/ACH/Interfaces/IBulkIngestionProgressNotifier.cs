namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Contrato de notificación de avance desacoplado del canal de entrega
/// (SignalR, WebSocket, cola de eventos, webhook, etc.).
/// </summary>
public interface IBulkIngestionProgressNotifier
{
    Task NotifyBatchProgressAsync(Guid batchId, decimal progressPercent, string? message = null, CancellationToken ct = default);
}
