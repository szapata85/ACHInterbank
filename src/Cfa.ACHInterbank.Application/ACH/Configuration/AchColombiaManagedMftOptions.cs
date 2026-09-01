namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public sealed class AchColombiaManagedMftOptions
{
    public const string SectionName = "AchColombiaManagedMft";
    public bool Enabled { get; set; }
    public string OutboundPath { get; set; } = string.Empty;
    public string InboundPath { get; set; } = string.Empty;
    public string ProcessingPath { get; set; } = string.Empty;
    public string ArchivePath { get; set; } = string.Empty;
    public long MaximumFileBytes { get; set; } = 10 * 1024 * 1024;
}
