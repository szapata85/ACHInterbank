namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public class NachaGenerationOptions
{
    public const string SectionName = "NachaGeneration";
    public string Mode { get; set; } = "TABLE_DRIVEN";
    public string ExecutionScope { get; set; } = "LIVE";
    public bool AllowNonHomologatedCenitDevelopment { get; set; }
    public bool AchColExternalNamingHomologated { get; set; }
    public bool EnableType7TableDriven { get; set; }
    public bool FailOnResolverAmbiguity { get; set; }
    public bool EnableRecord6MappingEngine { get; set; }
    public bool Record6MappingDiagnostics { get; set; }
    public bool EnableRecord1MappingEngine { get; set; }
    public bool EnableRecord5MappingEngine { get; set; }
    public bool EnableRecord8MappingEngine { get; set; }
    public bool EnableRecord9MappingEngine { get; set; }
    public bool EnableType7CommonMappingEngine { get; set; }
    public List<string> Type7DisableLegacyFallbackForLayouts { get; set; } = [];
    public List<string> Type7EnableTableDrivenForClearingHouses { get; set; } = [];
    public bool Type7RolloutPolicyEnabled { get; set; } = true;
    public bool Type7RequireShadowBeforeDisableFallback { get; set; } = true;
    public int Type7MinQualifiedRuns { get; set; } = 10;
    public decimal Type7MinEquivalencePercent { get; set; } = 99.5m;
    public List<string> Type7CriticalFieldCodes { get; set; } = [];
    public List<string> Type7DisableFallbackEnvironments { get; set; } = [];
}
