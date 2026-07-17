using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Entities.Integrations;

public enum IntegrationMappingSetStatusEnum
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public enum IntegrationSourceKindEnum
{
    Transaction = 1,
    Addenda = 2,
    Batch = 3,
    Cycle = 4,
    ClearingHouse = 5,
    Constant = 6,
    Expression = 7,
    NachaHeader = 8,
    BatchHeader = 9,
    EntryDetail = 10,
    AddendaRecord = 11,
    BatchControl = 12,
    FileControl = 13,
    Prenotification = 14,
    DifferentialResponse = 15
}

public enum IntegrationParameterCardinalityEnum
{
    Scalar = 1,
    Object = 2,
    Collection = 3
}

public enum IntegrationParameterDirectionEnum
{
    Input = 1,
    Output = 2
}

public class IntegrationMethod : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SoapClientCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<IntegrationMethodParameter> Parameters { get; set; } = new List<IntegrationMethodParameter>();
    public ICollection<IntegrationSourceCatalogField> SourceCatalogFields { get; set; } = new List<IntegrationSourceCatalogField>();
    public ICollection<IntegrationMappingSet> MappingSets { get; set; } = new List<IntegrationMappingSet>();
    public ICollection<IntegrationResponseCode> ResponseCodes { get; set; } = new List<IntegrationResponseCode>();
}

public class IntegrationMethodParameter : AuditableEntity
{
    public long Id { get; set; }
    public int MethodId { get; set; }
    public IntegrationMethod Method { get; set; } = null!;

    public string ParameterPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DescriptionEs { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ExampleValue { get; set; } = string.Empty;
    public string UiHelpText { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public IntegrationParameterDirectionEnum Direction { get; set; } = IntegrationParameterDirectionEnum.Input;
    public IntegrationParameterCardinalityEnum Cardinality { get; set; } = IntegrationParameterCardinalityEnum.Scalar;
    public bool Required { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<IntegrationMappingRule> MappingRules { get; set; } = new List<IntegrationMappingRule>();
}

public class IntegrationSourceCatalogField : AuditableEntity
{
    public long Id { get; set; }
    public int? MethodId { get; set; }
    public IntegrationMethod? Method { get; set; }

    public IntegrationSourceKindEnum SourceKind { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public IntegrationParameterCardinalityEnum Cardinality { get; set; } = IntegrationParameterCardinalityEnum.Scalar;
    public bool Nullable { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<IntegrationMappingRule> MappingRules { get; set; } = new List<IntegrationMappingRule>();
}

public class IntegrationMappingSet : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int MethodId { get; set; }
    public IntegrationMethod Method { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public IntegrationMappingSetStatusEnum Status { get; set; } = IntegrationMappingSetStatusEnum.Draft;
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;

    public DateTime? PublishedAtUtc { get; set; }
    public string PublishedBy { get; set; } = string.Empty;

    public string ValidationSummaryJson { get; set; } = string.Empty;

    public ICollection<IntegrationMappingRule> Rules { get; set; } = new List<IntegrationMappingRule>();
    public ICollection<IntegrationMappingSetHistory> History { get; set; } = new List<IntegrationMappingSetHistory>();
}

public class IntegrationMappingRule : AuditableEntity
{
    public long Id { get; set; }
    public Guid MappingSetId { get; set; }
    public IntegrationMappingSet MappingSet { get; set; } = null!;

    public int MethodId { get; set; }
    public long ParameterId { get; set; }
    public IntegrationMethodParameter Parameter { get; set; } = null!;

    public IntegrationSourceKindEnum SourceKind { get; set; }
    public long? SourceCatalogFieldId { get; set; }
    public IntegrationSourceCatalogField? SourceCatalogField { get; set; }
    public string SourceFieldPath { get; set; } = string.Empty;

    public string? FixedValue { get; set; }
    public string? DefaultValue { get; set; }
    public string? TransformationCode { get; set; }
    public string? FormatMask { get; set; }
    public int Priority { get; set; } = 1;
    public bool? RequiredOverride { get; set; }
    public bool Enabled { get; set; } = true;
    public string? ConditionExpression { get; set; }
}

public class IntegrationMappingSetHistory : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MappingSetId { get; set; }
    public IntegrationMappingSet MappingSet { get; set; } = null!;

    public int MethodId { get; set; }
    public int Version { get; set; }
    public IntegrationMappingSetStatusEnum Status { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;

    public string SnapshotJson { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
}

public class IntegrationMappingTrace : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IntegrationKey { get; set; } = string.Empty;
    public string OperationKey { get; set; } = string.Empty;
    public string MappingPurpose { get; set; } = string.Empty;
    public string MappingDirection { get; set; } = string.Empty;
    public int? TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid? MappingSetId { get; set; }
    public int? MappingVersion { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public bool DryRun { get; set; }
    public bool ExternalTransmission { get; set; }
    public bool MonetaryMovementCreated { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<IntegrationMappingTraceEntry> Entries { get; set; } = new List<IntegrationMappingTraceEntry>();
}

public class IntegrationMappingTraceEntry : AuditableEntity
{
    public long Id { get; set; }
    public Guid TraceId { get; set; }
    public IntegrationMappingTrace Trace { get; set; } = null!;
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string SourceValueSanitized { get; set; } = string.Empty;
    public string MappedValueSanitized { get; set; } = string.Empty;
    public long? MappingRuleId { get; set; }
    public string TransformationApplied { get; set; } = string.Empty;
    public bool DefaultValueApplied { get; set; }
    public bool Required { get; set; }
    public bool UsedFallback { get; set; }
    public bool Missing { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
