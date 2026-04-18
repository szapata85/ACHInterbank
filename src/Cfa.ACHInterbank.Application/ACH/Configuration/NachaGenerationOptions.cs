namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public class NachaGenerationOptions
{
    public const string SectionName = "NachaGeneration";
    public string Mode { get; set; } = "LEGACY";
    public bool EnableType7TableDriven { get; set; }
    public bool FailOnResolverAmbiguity { get; set; }
}
