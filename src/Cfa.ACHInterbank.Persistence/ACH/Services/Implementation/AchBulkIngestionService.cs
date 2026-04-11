using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchBulkIngestionService : IAchBulkIngestionService
{
    private readonly IAchBulkTransactionService _bulkTransactionService;

    public AchBulkIngestionService(IAchBulkTransactionService bulkTransactionService)
    {
        _bulkTransactionService = bulkTransactionService;
    }

    public async Task<BulkIngestionResponse> SubmitAsync(BulkIngestionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProcessingMode == BulkIngestionProcessingMode.AsynchronousJob)
        {
            // Contrato preparado para evolución futura de jobs asíncronos.
            // Por ahora no se implementa despacho real de jobs.
            return new BulkIngestionResponse
            {
                ProcessingMode = BulkIngestionProcessingMode.AsynchronousJob,
                JobId = null,
                Status = "NOT_IMPLEMENTED"
            };
        }

        if (request.SourceType != BulkIngestionSourceType.InlineTransactions)
        {
            throw new NotSupportedException($"El origen '{request.SourceType}' aún no está soportado en modo síncrono.");
        }

        var transactions = request.Transactions ?? [];
        var result = await _bulkTransactionService.RegisterBulkAsync(new BulkAchTransactionRequest
        {
            BatchReference = request.BatchReference,
            ChunkSize = request.ChunkSize,
            Transactions = transactions
        }, ct);

        return new BulkIngestionResponse
        {
            ProcessingMode = BulkIngestionProcessingMode.Synchronous,
            Status = "COMPLETED",
            ImmediateResult = result
        };
    }
}
