namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed record GeneratedReportFile
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}

