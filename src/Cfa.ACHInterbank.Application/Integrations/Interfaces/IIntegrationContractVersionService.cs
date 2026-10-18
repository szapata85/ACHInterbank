using Cfa.ACHInterbank.Application.Integrations.Dtos;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationContractVersionService
{
    Task<IReadOnlyCollection<IntegrationContractVersionDto>> GetByMethodAsync(int methodId, CancellationToken ct = default);
    Task<IntegrationContractVersionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationContractVersionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationContractVersionDto?> ResolveAsync(int methodId, Guid? contractVersionId = null, CancellationToken ct = default);
    Task<IReadOnlyCollection<IntegrationMethodParameterDto>> GetParametersAsync(Guid contractVersionId, CancellationToken ct = default);
    Task<IntegrationContractVersionDto> CreateDraftAsync(int methodId, CreateIntegrationContractVersionRequest request, CancellationToken ct = default);
    Task<IntegrationContractVersionDetailDto> CloneAsync(int methodId, CloneIntegrationContractVersionRequest request, CancellationToken ct = default);
    Task<IntegrationContractParameterPresentationResultDto> UpdateParameterPresentationAsync(
        Guid contractVersionId,
        long parameterId,
        UpdateIntegrationContractParameterPresentationRequest request,
        CancellationToken ct = default);
    Task<IntegrationContractDiffDto> CompareAsync(Guid leftId, Guid rightId, CancellationToken ct = default);
    Task<IntegrationContractDiffDto> CompareArtifactAsync(
        Guid baseContractVersionId,
        int methodId,
        IntegrationContractArtifactAnalysisDto analysis,
        CancellationToken ct = default);
    Task<IntegrationContractVersionDto> MarkValidatedAsync(Guid id, long expectedRevision, CancellationToken ct = default);
    Task<IntegrationContractMappingTransitionResultDto> ActivateAsync(
        Guid id,
        ActivateIntegrationContractVersionRequest request,
        CancellationToken ct = default);
    Task<IntegrationContractMappingTransitionResultDto> RollbackAsync(
        Guid targetContractVersionId,
        RollbackIntegrationContractVersionRequest request,
        CancellationToken ct = default);
    Task<IntegrationContractVersionDto> ActivateAsync(Guid id, long expectedRevision, string activatedBy, CancellationToken ct = default);
    Task<IntegrationContractVersionDto> RetireAsync(Guid id, long expectedRevision, CancellationToken ct = default);
}
