using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IReportGenerator
{
    Task<GeneratedReportFile> GenerateTraceabilityPdfAsync(TraceabilityReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateSentTransactionsPdfAsync(AchTransactionReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateReceivedTransactionsPdfAsync(AchTransactionReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateReturnsPdfAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateRejectionsPdfAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateNachaFilesPdfAsync(AchNachaFileReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateCyclesPdfAsync(AchCycleReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateReconciliationPdfAsync(AchReconciliationReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateAuditPdfAsync(AchAuditReportFilter filter, CancellationToken ct = default);
    Task<GeneratedReportFile> GenerateHistoryPdfAsync(AchHistoryReportFilter filter, CancellationToken ct = default);
}
