using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchBulkFileIngestionService
{
    Task<BulkFileUploadResponse> UploadAndParseAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        BulkFileUploadRequest request,
        CancellationToken ct = default);
}
