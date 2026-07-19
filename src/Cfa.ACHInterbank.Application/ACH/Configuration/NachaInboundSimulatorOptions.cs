namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public sealed class NachaInboundSimulatorOptions
{
    public const string SectionName = "NachaInboundSimulator";

    public bool Enabled { get; set; }
    public string Mode { get; set; } = "Disabled";
    public bool AllowExternalTransmission { get; set; }
    public bool RequireSyntheticData { get; set; } = true;
    public bool AllowAutoImport { get; set; }
    public bool DifferentialResponsesEnabled { get; set; }
    public bool RequirePublishedDifferentialProfile { get; set; } = true;
    public string OutputDirectory { get; set; } = "docs/uat/evidencias/nacha-m-inbound-simulator/generated";
    public int MaxEntriesPerSimulation { get; set; } = 10;
    public string[] AllowedClearingHouses { get; set; } = ["ACHCOL", "CENIT"];

    public bool IsUatLike()
        => Enabled
           && !AllowExternalTransmission
           && !AllowAutoImport
           && (string.Equals(Mode, "UAT", StringComparison.OrdinalIgnoreCase)
               || string.Equals(Mode, "Local", StringComparison.OrdinalIgnoreCase));
}
