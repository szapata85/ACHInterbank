namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaConfigProfilesDashboardReadModel
{
    public string ProductiveStatus { get; init; } = "NO-GO";
    public bool IsOfficialModel { get; init; } = true;
    public bool LegacyDeprecated { get; init; } = true;
    public int ProfileCount { get; init; }
    public int PublishedProfileCount { get; init; }
    public int CurrentProfileCount { get; init; }
    public int LayoutVariantCount { get; init; }
    public int FieldCount { get; init; }
    public IReadOnlyList<string> ClearingHouses { get; init; } = [];
    public IReadOnlyList<string> RecordTypes { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public class NachaConfigProfileReadModel
{
    public int ProfileId { get; init; }
    public string ProfileCode { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string FlowType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public bool IsCurrent { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public int LayoutVariantCount { get; init; }
    public int FieldCount { get; init; }
    public IReadOnlyList<string> RecordTypes { get; init; } = [];
    public bool IsOfficialModel { get; init; } = true;
    public bool LegacyDeprecated { get; init; } = true;
}

public sealed class NachaConfigProfileDetailReadModel : NachaConfigProfileReadModel
{
    public IReadOnlyList<NachaConfigProfileVariantReadModel> Variants { get; init; } = [];
    public IReadOnlyList<NachaConfigProfileFieldReadModel> Fields { get; init; } = [];
}

public sealed class NachaConfigProfileVariantReadModel
{
    public int VariantId { get; init; }
    public string VariantCode { get; init; } = string.Empty;
    public string RecordType { get; init; } = string.Empty;
    public int RecordLength { get; init; }
    public int BlockingFactor { get; init; }
    public bool IsActive { get; init; }
    public int FieldCount { get; init; }
}

public sealed class NachaConfigProfileFieldReadModel
{
    public int FieldId { get; init; }
    public string RecordType { get; init; } = string.Empty;
    public string FieldName { get; init; } = string.Empty;
    public int StartPosition { get; init; }
    public int Length { get; init; }
    public int EndPosition { get; init; }
    public string DataType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public string? SourceFieldPath { get; init; }
    public string PaddingDirection { get; init; } = string.Empty;
    public string PaddingChar { get; init; } = string.Empty;
    public string? Format { get; init; }
    public bool IsComputed { get; init; }
    public bool IsControlTotalField { get; init; }
}
