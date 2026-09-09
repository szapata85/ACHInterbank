using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public static class NachaPublicationSnapshotSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HashSet<string> GenerationCriticalTagKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        NachaSettlementPolicyMetadata.TagKey,
        "NormativeSource",
        "NormativeVersion",
        "IsHomologated",
        "IsPlaceholder"
    };

    private static readonly HashSet<string> SupportedSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "CONSTANTE",
        "ENTIDAD",
        "EXPRESION"
    };

    public static NachaPublicationSnapshot Build(
        CfgProfile profile,
        int versionMajor,
        int versionMinor,
        DateTime publishedAtUtc,
        string publishedBy)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var semanticMetadata = NachaSemanticContractMetadata.Resolve(profile.Records);
        if (semanticMetadata.Status != NachaSemanticContractMetadataStatus.Resolved)
        {
            throw new InvalidOperationException(
                $"SNAPSHOT_SEMANTIC_CONTRACT_INVALID: {semanticMetadata.ErrorCode}: {semanticMetadata.Error}");
        }

        var tags = profile.Tags
            .Where(tag => GenerationCriticalTagKeys.Contains(tag.TagKey)
                          || tag.TagKey.StartsWith(NachaOutboundPolicyMetadata.Prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(tag => tag.TagKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(tag => tag.TagValue, StringComparer.Ordinal)
            .Select(tag => new NachaPublicationSnapshotTag(tag.TagKey, tag.TagValue))
            .ToArray();

        var records = profile.Records
            .OrderBy(record => record.Sequence)
            .ThenBy(record => record.RecordCode.Code, StringComparer.OrdinalIgnoreCase)
            .Select(record => new NachaPublicationSnapshotRecord(
                record.RecordCode.Code,
                record.Sequence,
                record.IsEnabled,
                record.MinOccurs,
                record.MaxOccurs,
                record.SourceStrategy,
                record.LayoutVariant?.VariantCode,
                record.SemanticRuleSetId,
                BuildSemanticRuleSet(record, semanticMetadata.Contract!)))
            .ToArray();

        var variants = profile.LayoutVariants
            .OrderBy(variant => variant.RecordCode.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(variant => variant.Priority)
            .ThenBy(variant => variant.VariantCode, StringComparer.OrdinalIgnoreCase)
            .Select(BuildLayoutVariant)
            .ToArray();

        return new NachaPublicationSnapshot(
            NachaPublicationSnapshotFormat.CurrentVersion,
            new NachaPublicationSnapshotProfile(
                profile.Id,
                profile.ProfileCode,
                versionMajor,
                versionMinor,
                RequiredCode(profile.ClearingHouse?.Code, "clearing house"),
                RequiredCode(profile.FlowType?.Code, "flow type"),
                RequiredCode(profile.Direction?.Code, "direction"),
                profile.ServiceClass?.Code,
                profile.ContextPriority,
                profile.EffectiveFrom,
                profile.EffectiveTo,
                "PUBLICADO",
                publishedAtUtc,
                publishedBy),
            tags,
            records,
            variants);
    }

    public static string Serialize(NachaPublicationSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot, SerializerOptions);

    public static NachaPublicationSnapshotReadResult Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return LegacyOrIncomplete("SnapshotJson está vacío.");
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("snapshotFormatVersion", out var versionElement)
                || versionElement.ValueKind != JsonValueKind.Number
                || !versionElement.TryGetInt32(out var formatVersion))
            {
                return LegacyOrIncomplete("El snapshot no declara snapshotFormatVersion.");
            }

            if (formatVersion != NachaPublicationSnapshotFormat.CurrentVersion)
            {
                return new NachaPublicationSnapshotReadResult(
                    NachaPublicationSnapshotReadStatus.UnsupportedVersion,
                    Error: $"Snapshot format version {formatVersion} no está soportada.");
            }

            var snapshot = JsonSerializer.Deserialize<NachaPublicationSnapshot>(json, SerializerOptions);
            var completenessError = ValidateComplete(snapshot);
            return completenessError is null
                ? new NachaPublicationSnapshotReadResult(NachaPublicationSnapshotReadStatus.Supported, snapshot)
                : LegacyOrIncomplete(completenessError);
        }
        catch (JsonException exception)
        {
            return new NachaPublicationSnapshotReadResult(
                NachaPublicationSnapshotReadStatus.Malformed,
                Error: exception.Message);
        }
        catch (NotSupportedException exception)
        {
            return new NachaPublicationSnapshotReadResult(
                NachaPublicationSnapshotReadStatus.Malformed,
                Error: exception.Message);
        }
    }

    private static NachaPublicationSnapshotSemanticRuleSet? BuildSemanticRuleSet(
        CfgProfileRecord record,
        NachaServiceClassSemanticContract resolvedContract)
    {
        if (!record.SemanticRuleSetId.HasValue)
        {
            return null;
        }

        var ruleSet = record.SemanticRuleSet
            ?? throw new InvalidOperationException("SNAPSHOT_SEMANTIC_RULE_SET_MISSING: la referencia semántica no fue cargada.");
        var rules = ruleSet.Rules
            .OrderBy(rule => rule.Order)
            .ThenBy(rule => rule.RuleCode, StringComparer.OrdinalIgnoreCase)
            .Select(rule => new NachaPublicationSnapshotSemanticRule(
                RequiredCode(rule.RuleType?.Code, $"semantic rule type {rule.RuleCode}"),
                RequiredCode(rule.RuleCode, "semantic rule code"),
                rule.Order,
                rule.ConditionDsl,
                ParseOptionalJson(rule.RuleConfigJson, $"semantic rule {rule.RuleCode}"),
                rule.ErrorCode,
                rule.ErrorMessageEs))
            .ToArray();
        var declarations = resolvedContract.Rules
            .OrderBy(rule => rule.ServiceClassCode, StringComparer.Ordinal)
            .Select(rule => new NachaPublicationSnapshotSemanticDeclaration(
                rule.ServiceClassCode,
                rule.AllowsCredit,
                rule.AllowsDebit))
            .ToArray();

        return new NachaPublicationSnapshotSemanticRuleSet(
            ruleSet.Id,
            RequiredCode(ruleSet.RuleSetCode, "semantic rule-set code"),
            RequiredCode(ruleSet.Scope, "semantic rule-set scope"),
            rules,
            declarations);
    }

    private static NachaPublicationSnapshotLayoutVariant BuildLayoutVariant(CfgLayoutVariant variant)
        => new(
            RequiredCode(variant.VariantCode, "layout variant code"),
            RequiredCode(variant.RecordCode?.Code, $"record code for layout {variant.VariantCode}"),
            variant.Priority,
            variant.IsDefaultForRecord,
            variant.TotalLength,
            variant.EffectiveFrom,
            variant.EffectiveTo,
            RequiredCode(variant.Status?.Code, $"status for layout {variant.VariantCode}"),
            ParseSelectionPredicate(variant.SelectionPredicateJson, variant.VariantCode),
            variant.Fields
                .OrderBy(field => field.SortOrder)
                .ThenBy(field => field.StartPosition)
                .ThenBy(field => field.FieldCode, StringComparer.OrdinalIgnoreCase)
                .Select(BuildField)
                .ToArray());

    private static NachaPublicationSnapshotField BuildField(CfgLayoutField field)
    {
        var source = field.SourceDefinition
            ?? throw new InvalidOperationException($"SNAPSHOT_FIELD_SOURCE_MISSING: {field.FieldCode}.");
        var sourceTypeCode = RequiredCode(source.DataSourceType?.Code, $"source type for field {field.FieldCode}");
        if (!SupportedSourceTypes.Contains(sourceTypeCode))
        {
            throw new InvalidOperationException($"SNAPSHOT_SOURCE_TYPE_UNSUPPORTED: {field.FieldCode} usa {sourceTypeCode}.");
        }

        return new NachaPublicationSnapshotField(
            RequiredCode(field.FieldCode, "field code"),
            field.StartPosition,
            field.Length,
            field.SortOrder,
            field.Justification,
            field.PadChar,
            field.IsEnabled,
            field.IsVisibleInBackoffice,
            field.FormatMask,
            new NachaPublicationSnapshotFieldSource(
                sourceTypeCode,
                source.ConstantValue,
                source.EntityName,
                source.PropertyPath,
                source.SqlObjectName,
                source.ExpressionDsl,
                source.ExternalCatalogCode,
                ParseOptionalJson(source.FallbackPolicyJson, $"fallback policy for field {field.FieldCode}")),
            ParseOptionalJson(field.TransformationPipelineJson, $"transformation pipeline for field {field.FieldCode}"),
            field.Rules
                .OrderBy(rule => rule.Order)
                .ThenBy(rule => rule.RuleCode, StringComparer.OrdinalIgnoreCase)
                .Select(rule => new NachaPublicationSnapshotFieldRule(
                    RequiredCode(rule.RuleType?.Code, $"rule type for {rule.RuleCode}"),
                    RequiredCode(rule.RuleCode, $"rule code for field {field.FieldCode}"),
                    rule.ErrorCode,
                    rule.ErrorMessageEs,
                    rule.Severity,
                    rule.ConditionDsl,
                    ParseOptionalJson(rule.RuleConfigJson, $"rule configuration {rule.RuleCode}"),
                    rule.Order,
                    rule.IsEnabled))
                .ToArray());
    }

    private static JsonElement? ParseSelectionPredicate(string? json, string variantCode)
    {
        var element = ParseOptionalJson(json, $"selection predicate for layout {variantCode}");
        if (element.HasValue && element.Value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"SNAPSHOT_SELECTION_PREDICATE_INVALID: {variantCode} debe declarar un objeto JSON.");
        }

        if (element.HasValue && element.Value.EnumerateObject().Any(property => property.Value.ValueKind != JsonValueKind.String))
        {
            throw new InvalidOperationException($"SNAPSHOT_SELECTION_PREDICATE_INVALID: {variantCode} sólo admite valores string.");
        }

        return element;
    }

    private static JsonElement? ParseOptionalJson(string? json, string description)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"SNAPSHOT_JSON_INVALID: {description} contiene JSON inválido.", exception);
        }
    }

    private static string? ValidateComplete(NachaPublicationSnapshot? snapshot)
    {
        if (snapshot?.Profile is null
            || string.IsNullOrWhiteSpace(snapshot.Profile.ProfileCode)
            || string.IsNullOrWhiteSpace(snapshot.Profile.ClearingHouseCode)
            || string.IsNullOrWhiteSpace(snapshot.Profile.FlowTypeCode)
            || string.IsNullOrWhiteSpace(snapshot.Profile.DirectionCode)
            || snapshot.Profile.VersionMajor <= 0
            || snapshot.Profile.VersionMinor < 0
            || !string.Equals(snapshot.Profile.PublishedStatusCode, "PUBLICADO", StringComparison.OrdinalIgnoreCase))
        {
            return "El envelope de publicación está incompleto.";
        }

        if (snapshot.GenerationCriticalTags is null
            || snapshot.GenerationCriticalTags.Count == 0
            || snapshot.GenerationCriticalTags.Any(tag => string.IsNullOrWhiteSpace(tag.Key) || string.IsNullOrWhiteSpace(tag.Value))
            || snapshot.GenerationCriticalTags.GroupBy(tag => tag.Key, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() != 1)
            || snapshot.GenerationCriticalTags.Count(tag => string.Equals(tag.Key, NachaSettlementPolicyMetadata.TagKey, StringComparison.OrdinalIgnoreCase)) != 1)
        {
            return "La metadata crítica de generación está incompleta o es ambigua.";
        }

        if (snapshot.Records is null || snapshot.Records.Count == 0
            || snapshot.Records.Any(record => string.IsNullOrWhiteSpace(record.RecordCode) || string.IsNullOrWhiteSpace(record.SourceStrategy))
            || snapshot.Records.GroupBy(record => record.Sequence).Any(group => group.Count() != 1))
        {
            return "Los records del perfil están incompletos o son ambiguos.";
        }

        if (snapshot.LayoutVariants is null || snapshot.LayoutVariants.Count == 0
            || snapshot.LayoutVariants.Any(variant => string.IsNullOrWhiteSpace(variant.VariantCode)
                                                      || string.IsNullOrWhiteSpace(variant.RecordCode)
                                                      || string.IsNullOrWhiteSpace(variant.StatusCode)
                                                      || variant.TotalLength <= 0
                                                      || variant.Fields is null
                                                      || variant.Fields.Count == 0))
        {
            return "Las variantes de layout están incompletas.";
        }

        foreach (var variant in snapshot.LayoutVariants)
        {
            if (variant.Fields.Any(field => string.IsNullOrWhiteSpace(field.FieldCode)
                                            || field.StartPosition <= 0
                                            || field.Length <= 0
                                            || field.StartPosition + field.Length - 1 > variant.TotalLength
                                            || field.Source is null
                                            || !SupportedSourceTypes.Contains(field.Source.DataSourceTypeCode)
                                            || field.Rules is null))
            {
                return $"La variante {variant.VariantCode} contiene fields incompletos.";
            }

            var enabled = variant.Fields.Where(field => field.IsEnabled).OrderBy(field => field.StartPosition).ToArray();
            if (enabled.Length == 0 || enabled.Zip(enabled.Skip(1), (left, right) => right.StartPosition < left.StartPosition + left.Length).Any(overlap => overlap))
            {
                return $"La variante {variant.VariantCode} no tiene fields habilitados válidos o contiene solapamientos.";
            }
        }

        var batchRecord = snapshot.Records.SingleOrDefault(record => record.IsEnabled && record.RecordCode == "5");
        if (batchRecord?.SemanticRuleSet is null
            || batchRecord.SemanticRuleSet.Rules is null
            || batchRecord.SemanticRuleSet.Rules.Count == 0
            || batchRecord.SemanticRuleSet.ResolvedDeclarations is null
            || batchRecord.SemanticRuleSet.ResolvedDeclarations.Select(rule => rule.ServiceClassCode).OrderBy(code => code).SequenceEqual(["200", "220", "225"]) == false)
        {
            return "El contrato semántico T5 por valor está incompleto.";
        }

        return null;
    }

    private static string RequiredCode(string? value, string description)
        => !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"SNAPSHOT_STABLE_CODE_MISSING: {description}.");

    private static NachaPublicationSnapshotReadResult LegacyOrIncomplete(string error)
        => new(NachaPublicationSnapshotReadStatus.LegacyOrIncomplete, Error: error);
}
