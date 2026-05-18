using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Export.Implementation;

public sealed class AccountingReviewExportAppService(
    IAccountingReviewReportBuilder reportBuilder,
    IAccountingReviewReportExporter reportExporter,
    IAchTransactionReportService transactionReportService,
    IAchReturnRejectionReportService returnRejectionReportService,
    IAchNachaCycleReportService nachaCycleReportService,
    IAchReconciliationReportService reconciliationReportService,
    IAchAuditHistoryReportService auditHistoryReportService) : IAccountingReviewExportAppService
{
    public async Task<AccountingReviewExportResult> ExportAsync(AccountingReviewExportApiRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var format = ParseFormat(request.Format);
        var reportRequest = BuildReportRequest(request);

        var rows = new List<AccountingReviewReportRow>();
        var differences = new List<AccountingReviewDifference>();
        var evidence = new List<AccountingReviewEvidenceReference>();
        var warnings = new List<string>();

        await BuildRowsAsync(request, rows, warnings, cancellationToken);
        await BuildDifferencesAsync(request, differences, warnings, cancellationToken);
        await BuildEvidenceAsync(request, evidence, warnings, cancellationToken);

        if (!rows.Any()) warnings.Add("No se encontraron filas reales para el alcance solicitado.");
        if (request.IncludeCudEvidence && !rows.Any(r => r.IsCudEvidence)) warnings.Add("CUD se mantiene como evidencia operacional sin API; no se encontró evidencia CUD runtime para el alcance.");
        warnings.Add("Reporte poblado parcialmente con servicios existentes; no constituye contabilidad ni asiento contable.");

        var report = reportBuilder.Build(reportRequest, rows, differences, evidence);
        var mergedWarnings = report.Warnings.Concat(warnings).Distinct().ToArray();
        report = new AccountingReviewReportResult
        {
            ReportId = report.ReportId, GeneratedAt = report.GeneratedAt, GeneratedBy = report.GeneratedBy,
            Scope = report.Scope, Summary = report.Summary, Rows = report.Rows, Differences = report.Differences,
            Evidence = report.Evidence, ExportMetadata = report.ExportMetadata, BoundaryFlags = report.BoundaryFlags,
            Warnings = mergedWarnings
        };

        return reportExporter.Export(report, new AccountingReviewExportRequest
        {
            Format = format, RequestedBy = reportRequest.RequestedBy, CsvDelimiter = request.CsvDelimiter,
            IncludeRows = request.IncludeRows, IncludeDifferences = request.IncludeDifferences, IncludeEvidence = request.IncludeEvidence,
            IncludeBoundaryFlags = request.IncludeBoundaryFlags, IncludeWarnings = request.IncludeWarnings,
            IncludeSummary = request.IncludeSummary, IncludeScope = request.IncludeScope
        });
    }

    private static AccountingReviewReportRequest BuildReportRequest(AccountingReviewExportApiRequest request) => new()
    {
        DateFrom = request.DateFrom, DateTo = request.DateTo, ClearingHouseId = request.ClearingHouseId, ClearingHouseCode = request.ClearingHouseCode,
        CycleId = request.CycleId, CycleName = request.CycleName, FileId = request.FileId, FileName = request.FileName, TransactionId = request.TransactionId,
        Status = request.Status, CauseCode = request.CauseCode, IncludeOutbound = request.IncludeOutbound, IncludeIncoming = request.IncludeIncoming,
        IncludeReturns = request.IncludeReturns, IncludeReturnOfReturn = request.IncludeReturnOfReturn, IncludeOrphans = request.IncludeOrphans,
        IncludeManualAuditOnly = request.IncludeManualAuditOnly, IncludeNetting = request.IncludeNetting, IncludeLiquidity = request.IncludeLiquidity,
        IncludeCudEvidence = request.IncludeCudEvidence, RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "sistema" : request.RequestedBy,
        CorrelationId = request.CorrelationId
    };

    private async Task BuildRowsAsync(AccountingReviewExportApiRequest request, List<AccountingReviewReportRow> rows, List<string> warnings, CancellationToken ct)
    {
        if (request.IncludeOutbound)
        {
            var sent = await transactionReportService.GetSentTransactionsAsync(new AchTransactionReportFilter { Date = request.DateFrom ?? request.DateTo, ClearingHouseId = request.ClearingHouseId, AchCycleId = request.CycleId, Reference = request.FileName, Page = 1, PageSize = 200 }, ct);
            rows.AddRange((sent?.Items ?? []).Select(x => new AccountingReviewReportRow { RowType = AccountingReviewRowType.OutboundTransaction, TransactionId = x.TransactionId, ExternalReference = x.Reference, FileName = x.NachaFileName, ClearingHouseCode = x.ClearingHouseName, CycleName = x.AchCycleName, OperationalDate = x.EffectiveEntryDate, Amount = x.Amount, Status = x.State.ToString(), Direction = "Outbound", IsAppliedOperationally = true }));
        }

        if (request.IncludeIncoming)
        {
            var received = await transactionReportService.GetReceivedTransactionsAsync(new AchTransactionReportFilter { Date = request.DateFrom ?? request.DateTo, ClearingHouseId = request.ClearingHouseId, AchCycleId = request.CycleId, Reference = request.FileName, Page = 1, PageSize = 200 }, ct);
            rows.AddRange((received?.Items ?? []).Select(x => new AccountingReviewReportRow { RowType = AccountingReviewRowType.IncomingReturn, TransactionId = x.TransactionId, ExternalReference = x.Reference, FileName = x.NachaFileName, ClearingHouseCode = x.ClearingHouseName, CycleName = x.AchCycleName, OperationalDate = x.EffectiveEntryDate, Amount = x.Amount, Status = x.State.ToString(), Direction = "Incoming", IsAppliedOperationally = true }));
        }

        if (request.IncludeReturns)
        {
            var ret = await returnRejectionReportService.GetReturnsAsync(new AchReturnRejectionReportFilter { Date = request.DateFrom ?? request.DateTo, ClearingHouseId = request.ClearingHouseId, Causal = request.CauseCode, Reference = request.FileName, Page = 1, PageSize = 200 }, ct);
            rows.AddRange((ret?.Items ?? []).Select(x => new AccountingReviewReportRow { RowType = AccountingReviewRowType.OutboundReturn, TransactionId = x.TransactionId, ExternalReference = x.Reference, ClearingHouseCode = x.ClearingHouseName, CycleName = x.AchCycleName, OperationalDate = x.EffectiveEntryDate, Amount = x.Amount, Status = x.State.ToString(), CauseCode = x.CausalCode, IsRejected = true, Observation = x.CausalDescription }));

            if (request.IncludeReturnOfReturn)
                rows.AddRange((ret?.Items ?? []).Where(x => x.OriginalTransactionId.HasValue).Select(x => new AccountingReviewReportRow { RowType = AccountingReviewRowType.ReturnOfReturn, TransactionId = x.TransactionId, ExternalReference = x.Reference, Amount = x.Amount, Status = x.State.ToString(), CauseCode = x.CausalCode, IsReturnOfReturn = true, Observation = "Return of Return identificado por correlación de transacción original." }));
        }

        if (request.IncludeManualAuditOnly)
            rows.Add(new AccountingReviewReportRow { RowType = AccountingReviewRowType.ManualAuditOnly, Observation = "Solo evidencia / revisión manual; no aplicación contable", IsManualAuditOnly = true, IsAppliedOperationally = false });

        if (!request.IncludeNetting) warnings.Add("Bloque neteo no solicitado en este alcance.");
        if (!request.IncludeLiquidity) warnings.Add("Bloque liquidez no solicitado en este alcance.");
    }

    private async Task BuildDifferencesAsync(AccountingReviewExportApiRequest request, List<AccountingReviewDifference> differences, List<string> warnings, CancellationToken ct)
    {
        var rec = await reconciliationReportService.GetReconciliationAsync(new AchReconciliationReportFilter { Date = request.DateFrom ?? request.DateTo, ClearingHouseId = request.ClearingHouseId, AchCycleId = request.CycleId }, ct);
        var diff = rec?.Differences ?? new AchReconciliationDifferencesDto();
        if (diff.SentVsReceivedAmountDiff != 0)
            differences.Add(new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Amount, Severity = AccountingReviewDifferenceSeverity.Warning, Description = "Diferencia monto enviados vs recibidos", DifferenceAmount = diff.SentVsReceivedAmountDiff });
        if (diff.SentVsReceivedCountDiff != 0)
            differences.Add(new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Count, Severity = AccountingReviewDifferenceSeverity.Warning, Description = "Diferencia conteo enviados vs recibidos", DifferenceAmount = diff.SentVsReceivedCountDiff });
        if (!(rec?.Inconsistencies?.Any() ?? false)) warnings.Add("No se reportaron inconsistencias de conciliación para el alcance.");
    }

    private async Task BuildEvidenceAsync(AccountingReviewExportApiRequest request, List<AccountingReviewEvidenceReference> evidence, List<string> warnings, CancellationToken ct)
    {
        var files = await nachaCycleReportService.GetNachaFilesAsync(new AchNachaFileReportFilter { Date = request.DateFrom ?? request.DateTo, ClearingHouseId = request.ClearingHouseId, Page = 1, PageSize = 50 }, ct);
        evidence.AddRange((files?.Items ?? []).Select(x => new AccountingReviewEvidenceReference { EvidenceType = AccountingReviewEvidenceType.NachaFile, ReferenceId = x.FileName, FileName = x.FileName, CreatedAt = DateTimeOffset.UtcNow, CreatedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "sistema" : request.RequestedBy, Description = $"Archivo NACHA {x.ExportKind}" }));

        var audit = await auditHistoryReportService.GetAuditAsync(new AchAuditReportFilter { FromUtc = request.DateFrom, ToUtc = request.DateTo, Page = 1, PageSize = 50 }, ct);
        evidence.AddRange((audit?.Items ?? []).Take(10).Select(x => new AccountingReviewEvidenceReference { EvidenceType = AccountingReviewEvidenceType.Traceability, ReferenceId = $"audit-{x.Entity}-{x.EntityId}-{x.DateUtc:yyyyMMddHHmmss}", CreatedAt = x.DateUtc, CreatedBy = x.User, Description = x.Action }));

        if (!request.IncludeCudEvidence)
            return;
        warnings.Add("Evidencia CUD no integrada a datos runtime en este alcance; CUD permanece como boundary operacional sin API.");
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
