using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Contrato extensible para ingestión masiva ACH desde múltiples orígenes
/// (JSON inline, archivo JSON, CSV, Excel) y con estrategias de procesamiento
/// síncrono o asíncrono por job.
/// </summary>
public interface IAchBulkIngestionService
{
    Task<BulkIngestionResponse> SubmitAsync(BulkIngestionRequest request, CancellationToken ct = default);
}
