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
    private readonly IAchReturnRejectionReportService _returnRejectionReportService;
    private readonly IAchNachaCycleReportService _nachaCycleReportService;
    private readonly IAchReconciliationReportService _reconciliationReportService;
    private readonly IAchAuditHistoryReportService _auditHistoryReportService;
    private readonly IReportBrandingProvider _brandingProvider;

    public QuestPdfReportGenerator(
        IAchTraceabilityService traceabilityService,
        IAchTransactionReportService transactionReportService,
        IAchReturnRejectionReportService returnRejectionReportService,
        IAchNachaCycleReportService nachaCycleReportService,
        IAchReconciliationReportService reconciliationReportService,
        IAchAuditHistoryReportService auditHistoryReportService,
        IReportBrandingProvider brandingProvider)
    {
        _traceabilityService = traceabilityService;
        _transactionReportService = transactionReportService;
        _returnRejectionReportService = returnRejectionReportService;
        _nachaCycleReportService = nachaCycleReportService;
        _reconciliationReportService = reconciliationReportService;
        _auditHistoryReportService = auditHistoryReportService;
        _brandingProvider = brandingProvider;
        Settings.License = LicenseType.Community;
    }

    public async Task<GeneratedReportFile> GenerateTraceabilityPdfAsync(TraceabilityReportFilter filter, CancellationToken ct = default)
    {
        var branding = await _brandingProvider.GetAsync(ct);
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
        }, branding);

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


    public async Task<GeneratedReportFile> GenerateReturnsPdfAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default)
    {
        return await GenerateReturnRejectionPdfAsync("Reporte de devoluciones", "Returns", filter, ct);
    }

    public async Task<GeneratedReportFile> GenerateRejectionsPdfAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default)
    {
        return await GenerateReturnRejectionPdfAsync("Reporte de rechazos", "Rejections", filter, ct);
    }

    private async Task<GeneratedReportFile> GenerateReturnRejectionPdfAsync(
        string title,
        string filePrefix,
        AchReturnRejectionReportFilter filter,
        CancellationToken ct)
    {
        var branding = await _brandingProvider.GetAsync(ct);
        var response = filePrefix == "Returns"
            ? await _returnRejectionReportService.GetReturnsAsync(filter, ct)
            : await _returnRejectionReportService.GetRejectionsAsync(filter, ct);

        var generatedAt = DateTime.UtcNow;
        var document = new AchReturnRejectionReportDocument(new AchReturnRejectionReportDocumentModel
        {
            Title = title,
            Filter = filter,
            Rows = response.Items,
            Totals = response.Totals,
            GeneratedAtUtc = generatedAt
        }, branding);

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_{filePrefix}_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }


    public async Task<GeneratedReportFile> GenerateNachaFilesPdfAsync(AchNachaFileReportFilter filter, CancellationToken ct = default)
    {
        var branding = await _brandingProvider.GetAsync(ct);
        var response = await _nachaCycleReportService.GetNachaFilesAsync(filter, ct);
        var generatedAt = DateTime.UtcNow;
        var document = new AchNachaFileReportDocument(new AchNachaFileReportDocumentModel
        {
            Filter = filter,
            Rows = response.Items,
            Totals = response.Totals,
            GeneratedAtUtc = generatedAt
        }, branding);

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_NachaFiles_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }

    public async Task<GeneratedReportFile> GenerateCyclesPdfAsync(AchCycleReportFilter filter, CancellationToken ct = default)
    {
        var branding = await _brandingProvider.GetAsync(ct);
        var response = await _nachaCycleReportService.GetCyclesAsync(filter, ct);
        var generatedAt = DateTime.UtcNow;
        var document = new AchCycleReportDocument(new AchCycleReportDocumentModel
        {
            Filter = filter,
            Rows = response.Items,
            Totals = response.Totals,
            GeneratedAtUtc = generatedAt
        }, branding);

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_Cycles_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }


    public async Task<GeneratedReportFile> GenerateReconciliationPdfAsync(AchReconciliationReportFilter filter, CancellationToken ct = default)
    {
        var branding = await _brandingProvider.GetAsync(ct);
        var response = await _reconciliationReportService.GetReconciliationAsync(filter, ct);
        var generatedAt = DateTime.UtcNow;
        var document = new AchReconciliationReportDocument(new AchReconciliationReportDocumentModel
        {
            Filter = filter,
            Totals = response.Totals,
            Differences = response.Differences,
            Inconsistencies = response.Inconsistencies,
            GeneratedAtUtc = generatedAt
        }, branding);

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_Reconciliation_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }


    public async Task<GeneratedReportFile> GenerateAuditPdfAsync(AchAuditReportFilter filter, CancellationToken ct = default)
    {
        var branding = await _brandingProvider.GetAsync(ct);
        var response = await _auditHistoryReportService.GetAuditAsync(filter, ct);
        var generatedAt = DateTime.UtcNow;
        var document = new AchAuditReportDocument(new AchAuditReportDocumentModel
        {
            Filter = filter,
            Rows = response.Items,
            GeneratedAtUtc = generatedAt
        }, branding);

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_Audit_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }

    public async Task<GeneratedReportFile> GenerateHistoryPdfAsync(AchHistoryReportFilter filter, CancellationToken ct = default)
    {
        var branding = await _brandingProvider.GetAsync(ct);
        var response = await _auditHistoryReportService.GetHistoryAsync(filter, ct);
        var generatedAt = DateTime.UtcNow;
        var document = new AchHistoryReportDocument(new AchHistoryReportDocumentModel
        {
            Filter = filter,
            Rows = response.Items,
            GeneratedAtUtc = generatedAt
        }, branding);

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_History_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }

    private async Task<GeneratedReportFile> GenerateTransactionMovementPdfAsync(
        string title,
        string filePrefix,
        AchTransactionReportFilter filter,
        CancellationToken ct)
    {
        var branding = await _brandingProvider.GetAsync(ct);
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
        }, branding);

        return new GeneratedReportFile
        {
            Content = document.GeneratePdf(),
            ContentType = "application/pdf",
            FileName = $"ACH_{filePrefix}_{generatedAt:yyyyMMdd_HHmmss}.pdf"
        };
    }
}
