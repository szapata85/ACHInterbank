using Cfa.ACHInterbank.Application.Integrations.Dtos;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationContractArtifactImportService
{
    Task<IntegrationContractArtifactPreviewDto> PreviewDraftAsync(
        int methodId,
        Guid baseContractVersionId,
        AnalyzeIntegrationContractArtifactRequest request,
        CancellationToken ct = default);

    Task<IntegrationContractArtifactImportResultDto> ImportDraftAsync(
        int methodId,
        ImportIntegrationContractArtifactRequest request,
        CancellationToken ct = default);
}
