using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Reports.Documents;
using Cfa.ACHInterbank.Persistence.Reports.Models;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public class QuestPdfReportGenerator : IReportGenerator
{
    private readonly IAchTraceabilityService _traceabilityService;
    private readonly IAchTransactionReportService _transactionReportService;

    public QuestPdfReportGenerator(
        IAchTraceabilityService traceabilityService,
        IAchTransactionReportService transactionReportService)
    {
        _traceabilityService = traceabilityService;
        _transactionReportService = transactionReportService;
        Settings.License = LicenseType.Community;
    }

    public async Task<GeneratedReportFile> GenerateTraceabilityPdfAsync(TraceabilityReportFilter filter, CancellationToken ct = default)
    {
        var rows = await _traceabilityService.GetTraceabilityReportAsync(
            filter.FromUtc,
            filter.ToUtc,
            filter.State,
            filter.AchCycleId,
            ct);

        var generatedAt = DateTime.UtcNow;
        var document = new TraceabilityReportDocument(new TraceabilityReportDocumentModel
        {
            Filter = filter,
            Rows = rows,
            GeneratedAtUtc = generatedAt
        });

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_Traceability_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }

    public async Task<GeneratedReportFile> GenerateSentTransactionsPdfAsync(AchTransactionReportFilter filter, CancellationToken ct = default)
    {
        return await GenerateTransactionMovementPdfAsync("Reporte de transacciones enviadas", "Sent", filter, ct);
    }

    public async Task<GeneratedReportFile> GenerateReceivedTransactionsPdfAsync(AchTransactionReportFilter filter, CancellationToken ct = default)
    {
        return await GenerateTransactionMovementPdfAsync("Reporte de transacciones recibidas", "Received", filter, ct);
    }

    private async Task<GeneratedReportFile> GenerateTransactionMovementPdfAsync(
        string title,
        string filePrefix,
        AchTransactionReportFilter filter,
        CancellationToken ct)
    {
        var response = filePrefix == "Sent"
            ? await _transactionReportService.GetSentTransactionsAsync(filter, ct)
            : await _transactionReportService.GetReceivedTransactionsAsync(filter, ct);

        var generatedAt = DateTime.UtcNow;
        var document = new AchTransactionMovementReportDocument(new AchTransactionMovementReportDocumentModel
        {
            Title = title,
            Filter = filter,
            Rows = response.Items,
            Totals = response.Totals,
            GeneratedAtUtc = generatedAt
        });

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_{filePrefix}_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }
}
