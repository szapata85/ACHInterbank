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

    public QuestPdfReportGenerator(IAchTraceabilityService traceabilityService)
    {
        _traceabilityService = traceabilityService;
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
}
