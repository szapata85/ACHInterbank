using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public class NachaConfigResolutionRequest
{
    public string ClearingHouseCode { get; init; } = "ACH";
    public string FlowTypeCode { get; init; } = "ORIGINAL";
    public string DirectionCode { get; init; } = "SALIDA";
    public string? ServiceClassCode { get; init; }
    public DateTime ProcessDateUtc { get; init; }
    public IReadOnlyCollection<string> RecordCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> SelectionContext { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class NachaConfigResolutionResult
{
    public bool Success { get; init; }
    public bool UsedFallback { get; set; }
    public CfgProfile? Profile { get; init; }
    public IReadOnlyDictionary<string, CfgLayoutVariant> LayoutsByRecordCode { get; init; } = new Dictionary<string, CfgLayoutVariant>(StringComparer.OrdinalIgnoreCase);
    public List<string> Trace { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

public class NachaGenerationAuditResult
{
    public string Mode { get; set; } = "LEGACY";
    public int? ProfileId { get; set; }
    public string? ProfileCode { get; set; }
    public List<string> NewEngineRecordCodes { get; init; } = [];
    public List<string> LegacyRecordCodes { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public List<string> Trace { get; init; } = [];
    public List<string> EquivalenceDiffs { get; init; } = [];
    public int Type7TotalCandidates { get; set; }
    public int Type7GeneratedTableDriven { get; set; }
    public int Type7GeneratedLegacy { get; set; }
    public string? ClearingHouseCode { get; set; }
    public string? Type7LayoutVariantCode { get; set; }
    public Dictionary<string, int> Type7FallbackReasons { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Type7FallbackByLayout { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Type7DiffByField { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Type7AliasResolutionTrace { get; init; } = [];
    public int ShadowDiffCount { get; set; }
    public List<string> ShadowDiffDetails { get; init; } = [];
}
