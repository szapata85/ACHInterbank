using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Export.Models;

public enum AccountingReviewExportFormat { Pdf = 1, Csv = 2, Excel = 3 }

public sealed class AccountingReviewExportRequest
{
    public AccountingReviewExportFormat Format { get; init; }
    public string ReportTitle { get; init; } = "Accounting Review Reconciliation";
    public string RequestedBy { get; init; } = string.Empty;
    public string CultureName { get; init; } = "es-CO";
    public string CsvDelimiter { get; init; } = ";";
    public bool IncludeRows { get; init; } = true;
    public bool IncludeDifferences { get; init; } = true;
    public bool IncludeEvidence { get; init; } = true;
    public bool IncludeBoundaryFlags { get; init; } = true;
    public bool IncludeWarnings { get; init; } = true;
    public bool IncludeSummary { get; init; } = true;
    public bool IncludeScope { get; init; } = true;
    public bool IncludeGeneratedMetadata { get; init; } = true;
}

public sealed class AccountingReviewExportResult
{
    public AccountingReviewExportFormat Format { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public byte[] Content { get; init; } = [];
    public DateTimeOffset GeneratedAt { get; init; }
    public string GeneratedBy { get; init; } = string.Empty;
    public AccountingReviewBoundaryFlags BoundaryFlags { get; init; } = AccountingReviewBoundaryFlags.Default;
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
