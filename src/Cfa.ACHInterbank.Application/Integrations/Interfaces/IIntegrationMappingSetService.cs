using Cfa.ACHInterbank.Application.Integrations.Dtos;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationMappingSetService
{
    Task<IReadOnlyCollection<IntegrationMappingSetDto>> GetByMethodAsync(int? methodId, CancellationToken ct = default);
    Task<IReadOnlyCollection<IntegrationMappingSetDto>> GetByContractVersionAsync(Guid contractVersionId, CancellationToken ct = default);
    Task<IntegrationMappingSetDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationMappingWorkspaceDto?> GetWorkspaceAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationMappingSetDto?> GetPublishedByMethodAsync(int methodId, CancellationToken ct = default);
    Task<IntegrationMappingSetDto?> GetPublishedByContractVersionAsync(Guid contractVersionId, CancellationToken ct = default);
    Task<IntegrationMappingSetDto> CreateDraftAsync(CreateIntegrationMappingSetRequest request, CancellationToken ct = default);
    Task<IntegrationMappingSetDto> UpdateDraftAsync(Guid id, UpdateIntegrationMappingSetRequest request, CancellationToken ct = default);
    Task<IntegrationMappingSetDto> UpsertRulesAsync(Guid id, UpsertIntegrationMappingRulesRequest request, CancellationToken ct = default);
    Task<IntegrationMappingSetDto> UpdateParameterMappingAsync(Guid id, long parameterId, UpdateIntegrationParameterMappingRequest request, CancellationToken ct = default);
    Task<IntegrationMappingValidationResultDto> ValidateAsync(Guid id, ValidateIntegrationMappingSetRequest request, CancellationToken ct = default);
    Task<IntegrationMappingPreviewResultDto> PreviewAsync(Guid id, PreviewIntegrationMappingSetRequest request, CancellationToken ct = default);
    Task<IntegrationMappingSetDto> PublishAsync(Guid id, PublishIntegrationMappingSetRequest request, CancellationToken ct = default);
    Task<IntegrationMappingSetDto> CloneAsync(Guid id, CloneIntegrationMappingSetRequest request, CancellationToken ct = default);
    Task<IntegrationMappingContractCloneResultDto> CloneToContractVersionAsync(Guid id, CloneIntegrationMappingToContractVersionRequest request, CancellationToken ct = default);
    Task<IntegrationContractMappingValidationResultDto> ValidateContractMappingAsync(Guid id, ValidateIntegrationContractMappingRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<IntegrationMappingSetHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationMappingSetComparisonResultDto> CompareAsync(CompareIntegrationMappingSetsRequest request, CancellationToken ct = default);
}
