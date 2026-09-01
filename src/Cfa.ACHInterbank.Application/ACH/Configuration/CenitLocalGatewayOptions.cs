namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public sealed class CenitLocalGatewayOptions
{
    public const string SectionName = "CenitLocalGateway";
    public bool Enabled { get; set; } = false;
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string ArchivePath { get; set; } = string.Empty;
    public int PollIntervalMilliseconds { get; set; } = 500;
}
