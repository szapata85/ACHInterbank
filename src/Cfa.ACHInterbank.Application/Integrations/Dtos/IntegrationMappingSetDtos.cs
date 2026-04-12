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
    int MaxItems = 3,
    bool UseControlledSample = false);

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
    string Path,
    string Category);

public sealed record IntegrationMappingParameterValidationDto(
    long ParameterId,
    string ParameterPath,
    bool Required,
    string Status,
    string ResolutionKind,
    IReadOnlyCollection<string> Hints);

public sealed record IntegrationMappingCoverageSummaryDto(
    int TotalParameters,
    int ValidParameters,
    int IncompleteParameters,
    int InvalidParameters,
    int InactiveParameters,
    int CoveredByDefaultOrFixed,
    int CoveredBySourceField);

public sealed record IntegrationMappingValidationResultDto(
    Guid MappingSetId,
    bool IsValid,
    IReadOnlyCollection<IntegrationMappingValidationIssueDto> Issues,
    IntegrationMappingCoverageSummaryDto Coverage,
    IReadOnlyCollection<IntegrationMappingParameterValidationDto> Parameters);

public sealed record IntegrationMappingPreviewItemDto(
    long ParameterId,
    string ParameterPath,
    string ResolvedFrom,
    string? PreviewValue,
    string SourceSection,
    string ResolutionKind,
    string? AppliedTransformation,
    int Priority,
    bool Enabled);

public sealed record IntegrationMappingPreviewResultDto(
    Guid MappingSetId,
    int MethodId,
    string MethodCode,
    string ContextMode,
    IReadOnlyCollection<IntegrationMappingPreviewItemDto> Items,
    string PayloadPreviewJson,
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

public sealed record CompareIntegrationMappingSetsRequest(
    Guid LeftMappingSetId,
    Guid RightMappingSetId);

public sealed record IntegrationMappingSetComparisonMetadataDto(
    Guid MappingSetId,
    string Name,
    int Version,
    IntegrationMappingSetStatusEnum Status,
    DateTime? PublishedAtUtc,
    string PublishedBy,
    string Notes);

public sealed record IntegrationMappingSetRuleComparisonDto(
    long? LeftRuleId,
    long? RightRuleId,
    long ParameterId,
    string ParameterPath,
    string ParameterGroup,
    string ChangeType,
    IReadOnlyCollection<string> ChangedFields,
    string PotentialImpact,
    IntegrationMappingRuleDto? Left,
    IntegrationMappingRuleDto? Right);

public sealed record IntegrationMappingSetComparisonResultDto(
    IntegrationMappingSetComparisonMetadataDto Left,
    IntegrationMappingSetComparisonMetadataDto Right,
    IReadOnlyCollection<IntegrationMappingSetRuleComparisonDto> Rules);
