using Cfa.ACHInterbank.Application.Integrations.Dtos;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationContractArtifactAnalyzer
{
    Task<IntegrationContractArtifactAnalysisDto> AnalyzeAsync(
        AnalyzeIntegrationContractArtifactRequest request,
        CancellationToken ct = default);
}
