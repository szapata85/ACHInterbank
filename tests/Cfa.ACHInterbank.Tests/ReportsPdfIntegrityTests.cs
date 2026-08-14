using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Persistence.Reports;
using FluentAssertions;
using Moq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Cfa.ACHInterbank.Tests;

public sealed class ReportsPdfIntegrityTests
{
    [Fact]
    public async Task AllOperationalReports_ShouldGenerateParseablePdf_WithExpectedContent()
    {
        var generator = BuildGenerator();
        var reports = new (string Name, string Title, Task<GeneratedReportFile> File)[]
        {
            ("Enviados", "Reporte de transacciones enviadas", generator.GenerateSentTransactionsPdfAsync(new AchTransactionReportFilter { Date = new DateTime(2026, 8, 13) })),
            ("Recibidos", "Reporte de transacciones recibidas", generator.GenerateReceivedTransactionsPdfAsync(new AchTransactionReportFilter { Date = new DateTime(2026, 8, 13) })),
            ("Devoluciones", "Reporte de devoluciones", generator.GenerateReturnsPdfAsync(new AchReturnRejectionReportFilter { Date = new DateTime(2026, 8, 13) })),
            ("Rechazos", "Reporte de rechazos", generator.GenerateRejectionsPdfAsync(new AchReturnRejectionReportFilter { Date = new DateTime(2026, 8, 13) })),
            ("Archivos", "Reporte de archivos NACHA", generator.GenerateNachaFilesPdfAsync(new AchNachaFileReportFilter { Date = new DateTime(2026, 8, 13) })),
            ("Ciclos", "Reporte de ciclos ACH", generator.GenerateCyclesPdfAsync(new AchCycleReportFilter { Date = new DateTime(2026, 8, 13) })),
            ("Conciliación", "Reporte de conciliación ACH", generator.GenerateReconciliationPdfAsync(new AchReconciliationReportFilter { Date = new DateTime(2026, 8, 13) })),
            ("Auditoría", "Reporte de auditoría", generator.GenerateAuditPdfAsync(new AchAuditReportFilter { FromUtc = new DateTime(2026, 8, 13) })),
            ("Histórico", "Reporte histórico ACH", generator.GenerateHistoryPdfAsync(new AchHistoryReportFilter { FromUtc = new DateTime(2026, 8, 13) })),
            ("Trazabilidad ACH", "Reporte de trazabilidad ACH", generator.GenerateTraceabilityPdfAsync(new TraceabilityReportFilter { FromUtc = new DateTime(2026, 8, 13) }))
        };

        foreach (var report in reports)
        {
            var file = await report.File;
            file.ContentType.Should().Be("application/pdf", report.Name);
            file.FileName.Should().EndWith(".pdf", report.Name);
            file.Content.Should().StartWith([0x25, 0x50, 0x44, 0x46, 0x2D], report.Name);
            file.Content.Length.Should().BeGreaterThan(512, report.Name);

            using var document = PdfDocument.Open(file.Content);
            document.NumberOfPages.Should().BeGreaterThan(0, report.Name);
            var text = string.Join(" ", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
            text.Should().Contain(report.Title, report.Name);
            text.Should().Contain("2026", report.Name);
        }
    }

    private static QuestPdfReportGenerator BuildGenerator()
    {
        var traceability = new Mock<IAchTraceabilityService>();
        traceability.Setup(x => x.GetTraceabilityReportAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchTransferStateEnum?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AchTraceabilityReportRowDto { TransactionId = 101, Reference = "REF-2026", Amount = 1500, EffectiveEntryDate = new DateTime(2026, 8, 13), AchCycleName = "Ciclo prueba", ClearingHouseName = "ACH Colombia" }]);

        var transactions = new Mock<IAchTransactionReportService>();
        var transactionResponse = new AchTransactionReportResponseDto
        {
            Items = [new AchTransactionReportRowDto { TransactionId = 101, Reference = "REF-2026", Amount = 1500, EffectiveEntryDate = new DateTime(2026, 8, 13), ClearingHouseName = "ACH Colombia" }],
            Totals = new AchTransactionReportTotalsDto { TotalRecords = 1, TotalCreditAmount = 1500 }, Total = 1
        };
        transactions.Setup(x => x.GetSentTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(transactionResponse);
        transactions.Setup(x => x.GetReceivedTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(transactionResponse);

        var returns = new Mock<IAchReturnRejectionReportService>();
        var returnResponse = new AchReturnRejectionReportResponseDto
        {
            Items = [new AchReturnRejectionReportRowDto { TransactionId = 101, Reference = "REF-2026", CausalCode = "R01", Amount = 1500, EffectiveEntryDate = new DateTime(2026, 8, 13), ClearingHouseName = "ACH Colombia" }],
            Totals = new AchReturnRejectionReportTotalsDto { TotalRecords = 1, TotalAmount = 1500 }, Total = 1
        };
        returns.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(returnResponse);
        returns.Setup(x => x.GetRejectionsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(returnResponse);

        var nacha = new Mock<IAchNachaCycleReportService>();
        nacha.Setup(x => x.GetNachaFilesAsync(It.IsAny<AchNachaFileReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchNachaFileReportResponseDto
        {
            Items = [new AchNachaFileReportRowDto { FileName = "archivo-2026", GeneratedAtUtc = new DateTime(2026, 8, 13), ClearingHouseName = "ACH Colombia", TotalRecords = 1, TotalTransactions = 1 }],
            Totals = new AchNachaFileReportTotalsDto { TotalFiles = 1, TotalRecords = 1, TotalTransactions = 1 }, Total = 1
        });
        nacha.Setup(x => x.GetCyclesAsync(It.IsAny<AchCycleReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchCycleReportResponseDto
        {
            Items = [new AchCycleReportRowDto { CycleId = "C-2026", CycleName = "Ciclo prueba", ProcessingDate = new DateTime(2026, 8, 13), ClearingHouseName = "ACH Colombia", TotalTransactions = 1, TotalAmount = 1500 }],
            Totals = new AchCycleReportTotalsDto { TotalCycles = 1, TotalTransactions = 1, TotalAmount = 1500 }, Total = 1
        });

        var reconciliation = new Mock<IAchReconciliationReportService>();
        reconciliation.Setup(x => x.GetReconciliationAsync(It.IsAny<AchReconciliationReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchReconciliationReportResponseDto
        {
            Totals = new AchReconciliationTotalsDto { SentCount = 1, SentAmount = 1500, ReceivedCount = 1, ReceivedAmount = 1500 },
            Differences = new AchReconciliationDifferencesDto(),
            Inconsistencies = [new AchReconciliationInconsistencyDto { Code = "I-2026", Description = "Sin diferencias", AffectedCount = 0 }]
        });

        var audit = new Mock<IAchAuditHistoryReportService>();
        audit.Setup(x => x.GetAuditAsync(It.IsAny<AchAuditReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchAuditReportResponseDto
        {
            Items = [new AchAuditReportRowDto { User = "operador", Action = "Consulta 2026", Entity = "Reporte", EntityId = "101", DateUtc = new DateTime(2026, 8, 13) }], Total = 1
        });
        audit.Setup(x => x.GetHistoryAsync(It.IsAny<AchHistoryReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchHistoryReportResponseDto
        {
            Items = [new AchHistoryReportRowDto { TransactionId = 2026, DateUtc = new DateTime(2026, 8, 13), ReasonCode = "PRUEBA" }], Total = 1
        });

        var branding = new Mock<IReportBrandingProvider>();
        branding.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new PdfBrandingOptions { CompanyName = "ACH Interbank", FooterText = "Validación 2026" });
        return new QuestPdfReportGenerator(traceability.Object, transactions.Object, returns.Object, nacha.Object, reconciliation.Object, audit.Object, branding.Object);
    }
}
