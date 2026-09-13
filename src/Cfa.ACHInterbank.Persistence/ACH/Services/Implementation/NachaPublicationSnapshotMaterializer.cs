using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class NachaPublicationSnapshotMaterializer
{
    public static (CfgProfile Profile, List<CfgLayoutVariant> Variants) Materialize(NachaPublicationSnapshot snapshot)
    {
        var published = snapshot.Profile;
        var profile = new CfgProfile
        {
            Id = published.ProfileId,
            ProfileCode = published.ProfileCode,
            VersionMajor = published.VersionMajor,
            VersionMinor = published.VersionMinor,
            ContextPriority = published.ContextPriority,
            EffectiveFrom = published.EffectiveFrom,
            EffectiveTo = published.EffectiveTo,
            PublishedAt = published.PublishedAtUtc,
            PublishedBy = published.PublishedBy,
            ClearingHouse = new CatClearingHouse { Code = published.ClearingHouseCode },
            FlowType = new CatFlowType { Code = published.FlowTypeCode },
            Direction = new CatDirection { Code = published.DirectionCode },
            ServiceClass = published.ServiceClassCode is null ? null : new CatServiceClass { Code = published.ServiceClassCode },
            Status = new CatConfigStatus { Code = published.PublishedStatusCode },
            Tags = snapshot.GenerationCriticalTags.Select(tag => new CfgProfileTag
            {
                TagKey = tag.Key,
                TagValue = tag.Value
            }).ToList()
        };

        profile.Records = snapshot.Records.Select(record => new CfgProfileRecord
        {
            RecordCode = new CatRecordCode { Code = record.RecordCode },
            Sequence = record.Sequence,
            IsEnabled = record.IsEnabled,
            MinOccurs = record.MinOccurs,
            MaxOccurs = record.MaxOccurs,
            SourceStrategy = record.SourceStrategy,
            SemanticRuleSetId = record.SemanticRuleSetId,
            SemanticRuleSet = record.SemanticRuleSet is null ? null : new CfgRuleSet
            {
                Id = record.SemanticRuleSet.RuleSetId,
                RuleSetCode = record.SemanticRuleSet.RuleSetCode,
                Scope = record.SemanticRuleSet.Scope,
                Rules = record.SemanticRuleSet.Rules.Select(rule => new CfgRuleSetRule
                {
                    RuleType = new CatRuleType { Code = rule.RuleTypeCode },
                    RuleCode = rule.RuleCode,
                    Order = rule.Order,
                    ConditionDsl = rule.ConditionDsl,
                    RuleConfigJson = rule.RuleConfiguration?.GetRawText(),
                    ErrorCode = rule.ErrorCode,
                    ErrorMessageEs = rule.ErrorMessage
                }).ToList()
            }
        }).ToList();

        var variants = snapshot.LayoutVariants.Select(variant => new CfgLayoutVariant
        {
            Id = variant.LayoutVariantId!.Value,
            ProfileId = published.ProfileId,
            RecordCode = new CatRecordCode { Code = variant.RecordCode },
            VariantCode = variant.VariantCode,
            Priority = variant.Priority,
            IsDefaultForRecord = variant.IsDefaultForRecord,
            TotalLength = variant.TotalLength,
            EffectiveFrom = variant.EffectiveFrom,
            EffectiveTo = variant.EffectiveTo,
            Status = new CatConfigStatus { Code = variant.StatusCode },
            SelectionPredicateJson = variant.SelectionPredicate?.GetRawText(),
            Fields = variant.Fields.Where(field => field.IsEnabled).Select(field => new CfgLayoutField
            {
                Id = field.FieldDefinitionId!.Value,
                FieldCode = field.FieldCode,
                FieldNameEs = field.FieldNameEs!,
                StartPosition = field.StartPosition,
                Length = field.Length,
                SortOrder = field.SortOrder,
                Justification = field.Justification,
                PadChar = field.PadChar,
                IsEnabled = field.IsEnabled,
                IsVisibleInBackoffice = field.IsVisibleInBackoffice,
                FormatMask = field.FormatMask,
                TransformationPipelineJson = field.TransformationPipeline?.GetRawText(),
                SourceDefinition = new CfgFieldSourceDefinition
                {
                    DataSourceType = new CatDataSourceType { Code = field.Source.DataSourceTypeCode },
                    ConstantValue = field.Source.ConstantValue,
                    EntityName = field.Source.EntityName,
                    PropertyPath = field.Source.PropertyPath,
                    SqlObjectName = field.Source.SqlObjectName,
                    ExpressionDsl = field.Source.ExpressionDsl,
                    ExternalCatalogCode = field.Source.ExternalCatalogCode,
                    FallbackPolicyJson = field.Source.FallbackPolicy?.GetRawText()
                },
                Rules = field.Rules.Where(rule => rule.IsEnabled).Select(rule => new CfgFieldRule
                {
                    RuleType = new CatRuleType { Code = rule.RuleTypeCode },
                    RuleCode = rule.RuleCode,
                    ErrorCode = rule.ErrorCode,
                    ErrorMessageEs = rule.ErrorMessage,
                    Severity = rule.Severity,
                    ConditionDsl = rule.ConditionDsl,
                    RuleConfigJson = rule.RuleConfiguration?.GetRawText(),
                    Order = rule.Order,
                    IsEnabled = rule.IsEnabled
                }).ToList()
            }).ToList()
        }).ToList();

        return (profile, variants);
    }
}
