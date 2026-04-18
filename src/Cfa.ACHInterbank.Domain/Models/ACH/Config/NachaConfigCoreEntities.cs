using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH.Config;

public class CfgProfile : AuditableEntity
{
    public int Id { get; set; }
    public string ProfileCode { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int ClearingHouseId { get; set; }
    public CatClearingHouse ClearingHouse { get; set; } = null!;

    public int FlowTypeId { get; set; }
    public CatFlowType FlowType { get; set; } = null!;

    public int DirectionId { get; set; }
    public CatDirection Direction { get; set; } = null!;

    public int? ServiceClassId { get; set; }
    public CatServiceClass? ServiceClass { get; set; }

    public int ContextPriority { get; set; } = 100;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public int StatusId { get; set; }
    public CatConfigStatus Status { get; set; } = null!;

    public int VersionMajor { get; set; } = 1;
    public int VersionMinor { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }

    public int? SupersedesProfileId { get; set; }
    public CfgProfile? SupersedesProfile { get; set; }
    public ICollection<CfgProfile> SupersededByProfiles { get; set; } = new List<CfgProfile>();

    public byte[] RowVersion { get; set; } = [];

    public ICollection<CfgProfileTag> Tags { get; set; } = new List<CfgProfileTag>();
    public ICollection<CfgProfileRecord> Records { get; set; } = new List<CfgProfileRecord>();
    public ICollection<CfgLayoutVariant> LayoutVariants { get; set; } = new List<CfgLayoutVariant>();
    public ICollection<HistConfigSnapshot> Snapshots { get; set; } = new List<HistConfigSnapshot>();
    public ICollection<HistConfigChange> Changes { get; set; } = new List<HistConfigChange>();
    public ICollection<CfgPublishRequest> PublishRequests { get; set; } = new List<CfgPublishRequest>();
}

public class CfgProfileTag : AuditableEntity
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public CfgProfile Profile { get; set; } = null!;
    public string TagKey { get; set; } = string.Empty;
    public string TagValue { get; set; } = string.Empty;
}

public class CfgProfileRecord : AuditableEntity
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public CfgProfile Profile { get; set; } = null!;

    public int RecordCodeId { get; set; }
    public CatRecordCode RecordCode { get; set; } = null!;

    public int Sequence { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int MinOccurs { get; set; } = 1;
    public int? MaxOccurs { get; set; }
    public string SourceStrategy { get; set; } = "TABLE_DRIVEN";

    public int? LayoutVariantId { get; set; }
    public CfgLayoutVariant? LayoutVariant { get; set; }

    public int? SemanticRuleSetId { get; set; }
    public CfgRuleSet? SemanticRuleSet { get; set; }
}

public class CfgLayoutVariant : AuditableEntity
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public CfgProfile Profile { get; set; } = null!;

    public int RecordCodeId { get; set; }
    public CatRecordCode RecordCode { get; set; } = null!;

    public string VariantCode { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int Priority { get; set; } = 100;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public int StatusId { get; set; }
    public CatConfigStatus Status { get; set; } = null!;

    public int TotalLength { get; set; } = 106;
    public string? SelectionPredicateJson { get; set; }
    public bool IsDefaultForRecord { get; set; }

    public ICollection<CfgLayoutField> Fields { get; set; } = new List<CfgLayoutField>();
    public ICollection<CfgProfileRecord> ProfileRecords { get; set; } = new List<CfgProfileRecord>();
}

public class CfgLayoutField : AuditableEntity
{
    public int Id { get; set; }
    public int LayoutVariantId { get; set; }
    public CfgLayoutVariant LayoutVariant { get; set; } = null!;

    public string FieldCode { get; set; } = string.Empty;
    public string FieldNameEs { get; set; } = string.Empty;
    public int StartPosition { get; set; }
    public int Length { get; set; }
    public char PadChar { get; set; } = ' ';
    public char Justification { get; set; } = 'L';
    public string? FormatMask { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisibleInBackoffice { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    public int SourceDefinitionId { get; set; }
    public CfgFieldSourceDefinition SourceDefinition { get; set; } = null!;

    public string? TransformationPipelineJson { get; set; }

    public ICollection<CfgFieldRule> Rules { get; set; } = new List<CfgFieldRule>();
}

public class CfgFieldSourceDefinition : AuditableEntity
{
    public int Id { get; set; }
    public int DataSourceTypeId { get; set; }
    public CatDataSourceType DataSourceType { get; set; } = null!;

    public string? ConstantValue { get; set; }
    public string? EntityName { get; set; }
    public string? PropertyPath { get; set; }
    public string? SqlObjectName { get; set; }
    public string? ExpressionDsl { get; set; }
    public string? ExternalCatalogCode { get; set; }
    public string? FallbackPolicyJson { get; set; }

    public ICollection<CfgLayoutField> Fields { get; set; } = new List<CfgLayoutField>();
}

public class CfgFieldRule : AuditableEntity
{
    public int Id { get; set; }

    public int LayoutFieldId { get; set; }
    public CfgLayoutField LayoutField { get; set; } = null!;

    public int RuleTypeId { get; set; }
    public CatRuleType RuleType { get; set; } = null!;

    public string RuleCode { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessageEs { get; set; } = string.Empty;
    public string Severity { get; set; } = "ERROR";
    public string? ConditionDsl { get; set; }
    public string? RuleConfigJson { get; set; }
    public int Order { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class CfgRuleSet : AuditableEntity
{
    public int Id { get; set; }
    public string RuleSetCode { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Scope { get; set; } = "FILE";

    public ICollection<CfgRuleSetRule> Rules { get; set; } = new List<CfgRuleSetRule>();
    public ICollection<CfgProfileRecord> ProfileRecords { get; set; } = new List<CfgProfileRecord>();
}

public class CfgRuleSetRule : AuditableEntity
{
    public int Id { get; set; }

    public int RuleSetId { get; set; }
    public CfgRuleSet RuleSet { get; set; } = null!;

    public int RuleTypeId { get; set; }
    public CatRuleType RuleType { get; set; } = null!;

    public string RuleCode { get; set; } = string.Empty;
    public string? ConditionDsl { get; set; }
    public string? RuleConfigJson { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessageEs { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class CfgPublishRequest : AuditableEntity
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public CfgProfile Profile { get; set; } = null!;

    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? ValidationReportJson { get; set; }
}

public class HistConfigSnapshot : AuditableEntity
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public CfgProfile Profile { get; set; } = null!;

    public int VersionMajor { get; set; }
    public int VersionMinor { get; set; }
    public string SnapshotType { get; set; } = "DRAFT_SAVE";
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class HistConfigChange : AuditableEntity
{
    public int Id { get; set; }
    public int ProfileId { get; set; }
    public CfgProfile Profile { get; set; } = null!;

    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}
