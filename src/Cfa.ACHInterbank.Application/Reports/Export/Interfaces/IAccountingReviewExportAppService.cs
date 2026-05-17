using Cfa.ACHInterbank.Application.Reports.Export.Models;

namespace Cfa.ACHInterbank.Application.Reports.Export.Interfaces;

public interface IAccountingReviewExportAppService
{
    Task<AccountingReviewExportResult> ExportAsync(AccountingReviewExportApiRequest request, CancellationToken cancellationToken);
}

public sealed class AccountingReviewExportApiRequest
{
    public string Format { get; init; } = "pdf";
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? ClearingHouseCode { get; init; }
    public string? CycleId { get; init; }
    public string? CycleName { get; init; }
    public string? FileId { get; init; }
    public string? FileName { get; init; }
    public string? FileHash { get; init; }
    public int? TransactionId { get; init; }
    public string? Status { get; init; }
    public string? CauseCode { get; init; }
    public bool IncludeOutbound { get; init; } = true;
    public bool IncludeIncoming { get; init; } = true;
    public bool IncludeReturns { get; init; } = true;
    public bool IncludeReturnOfReturn { get; init; } = true;
    public bool IncludeOrphans { get; init; } = true;
    public bool IncludeManualAuditOnly { get; init; } = true;
    public bool IncludeNetting { get; init; } = true;
    public bool IncludeLiquidity { get; init; } = true;
    public bool IncludeCudEvidence { get; init; } = true;
    public bool IncludeRows { get; init; } = true;
    public bool IncludeDifferences { get; init; } = true;
    public bool IncludeEvidence { get; init; } = true;
    public bool IncludeBoundaryFlags { get; init; } = true;
    public bool IncludeWarnings { get; init; } = true;
    public bool IncludeSummary { get; init; } = true;
    public bool IncludeScope { get; init; } = true;
    public string RequestedBy { get; init; } = "sistema";
    public string? CorrelationId { get; init; }
    public string CsvDelimiter { get; init; } = ";";
}
