namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public sealed class AchOutboundReturnTransportOptions
{
    public const string SectionName = "AchOutboundReturnTransport";

    public bool Enabled { get; set; }
    public string Mode { get; set; } = "CfaManagedHandoff";
    public string HandoffDirectory { get; set; } = string.Empty;
    public long MaxFileBytes { get; set; } = 25 * 1024 * 1024;
}
