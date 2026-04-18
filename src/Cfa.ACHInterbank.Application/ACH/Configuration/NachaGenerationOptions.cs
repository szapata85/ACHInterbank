namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public class NachaGenerationOptions
{
    public const string SectionName = "NachaGeneration";
    public string Mode { get; set; } = "LEGACY";
    public bool EnableType7TableDriven { get; set; }
    public bool FailOnResolverAmbiguity { get; set; }
    public List<string> Type7DisableLegacyFallbackForLayouts { get; set; } = [];
    public List<string> Type7EnableTableDrivenForClearingHouses { get; set; } = [];
    public bool Type7RolloutPolicyEnabled { get; set; } = true;
    public bool Type7RequireShadowBeforeDisableFallback { get; set; } = true;
    public int Type7MinQualifiedRuns { get; set; } = 10;
    public decimal Type7MinEquivalencePercent { get; set; } = 99.5m;
    public List<string> Type7CriticalFieldCodes { get; set; } = [];
    public List<string> Type7DisableFallbackEnvironments { get; set; } = [];
}
