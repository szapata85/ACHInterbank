using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH.Config;

public class CatClearingHouse : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<CatServiceClass> ServiceClasses { get; set; } = new List<CatServiceClass>();
    public ICollection<CfgProfile> Profiles { get; set; } = new List<CfgProfile>();
}

public class CatFlowType : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public int? DirectionDefaultId { get; set; }
    public CatDirection? DirectionDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CfgProfile> Profiles { get; set; } = new List<CfgProfile>();
}

public class CatDirection : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<CatFlowType> FlowTypesDefault { get; set; } = new List<CatFlowType>();
    public ICollection<CfgProfile> Profiles { get; set; } = new List<CfgProfile>();
}

public class CatServiceClass : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public int? ClearingHouseId { get; set; }
    public CatClearingHouse? ClearingHouse { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CfgProfile> Profiles { get; set; } = new List<CfgProfile>();
}

public class CatRecordCode : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;
    public bool IsMandatoryBase { get; set; }

    public ICollection<CfgProfileRecord> ProfileRecords { get; set; } = new List<CfgProfileRecord>();
    public ICollection<CfgLayoutVariant> LayoutVariants { get; set; } = new List<CfgLayoutVariant>();
}

public class CatConfigStatus : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsEditable { get; set; }
    public bool IsPublishable { get; set; }

    public ICollection<CfgProfile> Profiles { get; set; } = new List<CfgProfile>();
    public ICollection<CfgLayoutVariant> LayoutVariants { get; set; } = new List<CfgLayoutVariant>();
}

public class CatDataSourceType : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;

    public ICollection<CfgFieldSourceDefinition> FieldSources { get; set; } = new List<CfgFieldSourceDefinition>();
}

public class CatRuleType : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEs { get; set; } = string.Empty;

    public ICollection<CfgFieldRule> FieldRules { get; set; } = new List<CfgFieldRule>();
    public ICollection<CfgRuleSetRule> RuleSetRules { get; set; } = new List<CfgRuleSetRule>();
}
