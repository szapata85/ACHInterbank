using System.Text.Json;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public static class NachaPublicationSnapshotFormat
{
    public const int CurrentVersion = 1;
}

public sealed record NachaPublicationSnapshot(
    int SnapshotFormatVersion,
    NachaPublicationSnapshotProfile Profile,
    IReadOnlyList<NachaPublicationSnapshotTag> GenerationCriticalTags,
    IReadOnlyList<NachaPublicationSnapshotRecord> Records,
    IReadOnlyList<NachaPublicationSnapshotLayoutVariant> LayoutVariants);

public sealed record NachaPublicationSnapshotProfile(
    int ProfileId,
    string ProfileCode,
    int VersionMajor,
    int VersionMinor,
    string ClearingHouseCode,
    string FlowTypeCode,
    string DirectionCode,
    string? ServiceClassCode,
    int ContextPriority,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string PublishedStatusCode,
    DateTime PublishedAtUtc,
    string PublishedBy);

public sealed record NachaPublicationSnapshotTag(string Key, string Value);

public sealed record NachaPublicationSnapshotRecord(
    string RecordCode,
    int Sequence,
    bool IsEnabled,
    int MinOccurs,
    int? MaxOccurs,
    string SourceStrategy,
    string? LayoutVariantCode,
    int? SemanticRuleSetId,
    NachaPublicationSnapshotSemanticRuleSet? SemanticRuleSet);

public sealed record NachaPublicationSnapshotSemanticRuleSet(
    int RuleSetId,
    string RuleSetCode,
    string Scope,
    IReadOnlyList<NachaPublicationSnapshotSemanticRule> Rules,
    IReadOnlyList<NachaPublicationSnapshotSemanticDeclaration> ResolvedDeclarations);

public sealed record NachaPublicationSnapshotSemanticRule(
    string RuleTypeCode,
    string RuleCode,
    int Order,
    string? ConditionDsl,
    JsonElement? RuleConfiguration,
    string ErrorCode,
    string ErrorMessage);

public sealed record NachaPublicationSnapshotSemanticDeclaration(
    string ServiceClassCode,
    bool AllowsCredit,
    bool AllowsDebit);

public sealed record NachaPublicationSnapshotLayoutVariant(
    string VariantCode,
    string RecordCode,
    int Priority,
    bool IsDefaultForRecord,
    int TotalLength,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string StatusCode,
    JsonElement? SelectionPredicate,
    IReadOnlyList<NachaPublicationSnapshotField> Fields);

public sealed record NachaPublicationSnapshotField(
    string FieldCode,
    int StartPosition,
    int Length,
    int SortOrder,
    char Justification,
    char PadChar,
    bool IsEnabled,
    bool IsVisibleInBackoffice,
    string? FormatMask,
    NachaPublicationSnapshotFieldSource Source,
    JsonElement? TransformationPipeline,
    IReadOnlyList<NachaPublicationSnapshotFieldRule> Rules);

public sealed record NachaPublicationSnapshotFieldSource(
    string DataSourceTypeCode,
    string? ConstantValue,
    string? EntityName,
    string? PropertyPath,
    string? SqlObjectName,
    string? ExpressionDsl,
    string? ExternalCatalogCode,
    JsonElement? FallbackPolicy);

public sealed record NachaPublicationSnapshotFieldRule(
    string RuleTypeCode,
    string RuleCode,
    string ErrorCode,
    string ErrorMessage,
    string Severity,
    string? ConditionDsl,
    JsonElement? RuleConfiguration,
    int Order,
    bool IsEnabled);

public enum NachaPublicationSnapshotReadStatus
{
    Supported = 1,
    Malformed = 2,
    UnsupportedVersion = 3,
    LegacyOrIncomplete = 4
}

public sealed record NachaPublicationSnapshotReadResult(
    NachaPublicationSnapshotReadStatus Status,
    NachaPublicationSnapshot? Snapshot = null,
    string? Error = null)
{
    public bool IsSupported => Status == NachaPublicationSnapshotReadStatus.Supported;
}
