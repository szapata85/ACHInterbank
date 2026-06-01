using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaIncomingFileProcessor
{
    Task<NachaIncomingFileProcessingResult> ProcessAsync(NachaIncomingFileRequest request, CancellationToken ct = default);
}
