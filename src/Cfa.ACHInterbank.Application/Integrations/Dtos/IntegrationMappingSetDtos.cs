using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Dtos;

public sealed record CreateIntegrationMappingSetRequest(
    int MethodId,
    string Name,
    string Notes,
    string CreatedBy);

public sealed record UpdateIntegrationMappingSetRequest(
    string Name,
    string Notes,
    bool IsActive,
    string UpdatedBy);

public sealed record UpsertIntegrationMappingRuleRequest(
    long? Id,
    int MethodId,
    long ParameterId,
    IntegrationSourceKindEnum SourceKind,
    long? SourceCatalogFieldId,
    string? SourceFieldPath,
    string? FixedValue,
    string? DefaultValue,
    string? TransformationCode,
    string? FormatMask,
    int Priority,
    bool? RequiredOverride,
    bool Enabled,
    string? ConditionExpression);

public sealed record UpsertIntegrationMappingRulesRequest(
    string UpdatedBy,
    IReadOnlyCollection<UpsertIntegrationMappingRuleRequest> Rules);

public sealed record CloneIntegrationMappingSetRequest(
    string NewName,
    string ClonedBy);

public sealed record PublishIntegrationMappingSetRequest(
    string PublishedBy,
    string? PublishNote = null);

public sealed record ValidateIntegrationMappingSetRequest(
    bool IncludeWarnings = true);

public sealed record PreviewIntegrationMappingSetRequest(
    int? SampleTransactionId = null,
    string? SampleCycleId = null,
    int MaxItems = 3);

public sealed record IntegrationMappingRuleDto(
    long Id,
    Guid MappingSetId,
    int MethodId,
    long ParameterId,
    IntegrationSourceKindEnum SourceKind,
    long? SourceCatalogFieldId,
    string SourceFieldPath,
    string? FixedValue,
    string? DefaultValue,
    string? TransformationCode,
    string? FormatMask,
    int Priority,
    bool? RequiredOverride,
    bool Enabled,
    string? ConditionExpression);

public sealed record IntegrationMappingSetDto(
    Guid Id,
    int MethodId,
    string MethodCode,
    string Name,
    int Version,
    IntegrationMappingSetStatusEnum Status,
    bool IsActive,
    string Notes,
    DateTime? PublishedAtUtc,
    string PublishedBy,
    IReadOnlyCollection<IntegrationMappingRuleDto> Rules);

public sealed record IntegrationMappingValidationIssueDto(
    string Severity,
    string Code,
    string Message,
    string Path);

public sealed record IntegrationMappingValidationResultDto(
    Guid MappingSetId,
    bool IsValid,
    IReadOnlyCollection<IntegrationMappingValidationIssueDto> Issues);

public sealed record IntegrationMappingPreviewItemDto(
    string ParameterPath,
    string ResolvedFrom,
    string? PreviewValue,
    int Priority,
    bool Enabled);

public sealed record IntegrationMappingPreviewResultDto(
    Guid MappingSetId,
    int MethodId,
    string MethodCode,
    IReadOnlyCollection<IntegrationMappingPreviewItemDto> Items,
    string RawPreviewJson);

public sealed record IntegrationMappingSetHistoryDto(
    Guid Id,
    Guid MappingSetId,
    int MethodId,
    int Version,
    IntegrationMappingSetStatusEnum Status,
    string Action,
    string PerformedBy,
    DateTime PerformedAtUtc,
    string SnapshotHash);
