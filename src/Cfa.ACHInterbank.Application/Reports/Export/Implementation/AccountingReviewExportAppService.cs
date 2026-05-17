using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Export.Implementation;

public sealed class AccountingReviewExportAppService(
    IAccountingReviewReportBuilder reportBuilder,
    IAccountingReviewReportExporter reportExporter) : IAccountingReviewExportAppService
{
    public Task<AccountingReviewExportResult> ExportAsync(AccountingReviewExportApiRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var format = ParseFormat(request.Format);
        var reportRequest = new AccountingReviewReportRequest
        {
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            ClearingHouseId = request.ClearingHouseId,
            ClearingHouseCode = request.ClearingHouseCode,
            CycleId = request.CycleId,
            CycleName = request.CycleName,
            FileId = request.FileId,
            FileName = request.FileName,
            TransactionId = request.TransactionId,
            Status = request.Status,
            CauseCode = request.CauseCode,
            IncludeOutbound = request.IncludeOutbound,
            IncludeIncoming = request.IncludeIncoming,
            IncludeReturns = request.IncludeReturns,
            IncludeReturnOfReturn = request.IncludeReturnOfReturn,
            IncludeOrphans = request.IncludeOrphans,
            IncludeManualAuditOnly = request.IncludeManualAuditOnly,
            IncludeNetting = request.IncludeNetting,
            IncludeLiquidity = request.IncludeLiquidity,
            IncludeCudEvidence = request.IncludeCudEvidence,
            RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "sistema" : request.RequestedBy,
            CorrelationId = request.CorrelationId
        };

        var report = reportBuilder.Build(
            reportRequest,
            rows: [],
            differences: [],
            evidence: []);

        var warnings = report.Warnings.ToList();
        warnings.Add("Reporte de revisión contable de terceros generado con alcance solicitado; integración de datos conciliatorios especializados pendiente.");
        warnings.Add("El reporte no constituye contabilización ni asiento contable.");

        report = new AccountingReviewReportResult
        {
            ReportId = report.ReportId,
            GeneratedAt = report.GeneratedAt,
            GeneratedBy = report.GeneratedBy,
            Scope = report.Scope,
            Summary = report.Summary,
            Rows = report.Rows,
            Differences = report.Differences,
            Evidence = report.Evidence,
            ExportMetadata = report.ExportMetadata,
            BoundaryFlags = report.BoundaryFlags,
            Warnings = warnings
        };

        var exportRequest = new AccountingReviewExportRequest
        {
            Format = format,
            RequestedBy = reportRequest.RequestedBy,
            CsvDelimiter = request.CsvDelimiter,
            IncludeRows = request.IncludeRows,
            IncludeDifferences = request.IncludeDifferences,
            IncludeEvidence = request.IncludeEvidence,
            IncludeBoundaryFlags = request.IncludeBoundaryFlags,
            IncludeWarnings = request.IncludeWarnings,
            IncludeSummary = request.IncludeSummary,
            IncludeScope = request.IncludeScope
        };

        return Task.FromResult(reportExporter.Export(report, exportRequest));
    }

    private static AccountingReviewExportFormat ParseFormat(string? raw)
    {
        var format = raw?.Trim().ToLowerInvariant();
        return format switch
        {
            "pdf" => AccountingReviewExportFormat.Pdf,
            "csv" => AccountingReviewExportFormat.Csv,
            "excel" or "xlsx" => AccountingReviewExportFormat.Excel,
            _ => throw new ArgumentException("Formato inválido. Use: pdf, csv, excel o xlsx.", nameof(raw))
        };
    }
}
