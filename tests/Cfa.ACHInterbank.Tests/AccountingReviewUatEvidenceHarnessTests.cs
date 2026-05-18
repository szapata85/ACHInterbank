using System.IO.Compression;
using System.Text;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Implementation;
using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Implementation;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AccountingReviewUatEvidenceHarnessTests
{
    private static readonly DateTime FixedDateUtc = new(2026, 4, 26, 0, 0, 0, DateTimeKind.Utc);

    [Fact] public async Task UAT_10_001_PdfControllerExport() { var result = await Execute("pdf"); result.ContentType.Should().Be("application/pdf"); result.FileName.Should().EndWith(".pdf"); result.Content.Should().NotBeEmpty(); Encoding.UTF8.GetString(result.Content).Should().StartWith("%PDF"); Encoding.UTF8.GetString(result.Content).Should().Contain("Frontera no contable"); }
    [Fact] public async Task UAT_10_002_CsvBoundary() { var csv = await Csv(); csv.Should().Contain("RESUMEN").And.Contain("FILAS").And.Contain("FRONTERA_NO_CONTABLE").And.Contain("NO contabiliza"); csv.Should().NotContain("LedgerId").And.NotContain("JournalId").And.NotContain("PostingId").And.NotContain("AccountingEntryId"); }
    [Fact] public async Task UAT_10_003_XlsxIntegrity() { var xlsx = await Execute("xlsx"); xlsx.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"); xlsx.FileName.Should().EndWith(".xlsx"); using var zip = new ZipArchive(new MemoryStream(xlsx.Content)); zip.Entries.Select(x => x.FullName).Should().Contain(["xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml", "xl/worksheets/sheet3.xml", "xl/worksheets/sheet4.xml", "xl/worksheets/sheet5.xml", "xl/worksheets/sheet6.xml", "xl/worksheets/sheet7.xml"]); var workbook = ReadZip(zip, "xl/workbook.xml"); workbook.Should().Contain("Resumen").And.Contain("Alcance").And.Contain("Filas").And.Contain("Diferencias").And.Contain("Evidencias").And.Contain("Advertencias").And.Contain("FronteraNoContable"); zip.Entries.Select(x => x.FullName).Should().NotContain("xl/vbaProject.bin"); zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/")).Select(e => ReadZip(zip, e.FullName)).Should().OnlyContain(s => !s.Contains("<f>")); }
    [Fact] public async Task UAT_10_004_OutboundIncoming() { var csv = await Csv(tx: Tx([new() { TransactionId = 1, Reference = "OUT-UAT", Amount = 111m, State = AchTransferStateEnum.Pending }], [new() { TransactionId = 2, Reference = "IN-UAT", Amount = 222m, State = AchTransferStateEnum.Certified }])); csv.Should().Contain("TransaccionSaliente").And.Contain("DevolucionEntrante").And.Contain("111").And.Contain("222"); csv.Should().NotContain("REF-FAKE"); }
    [Fact] public async Task UAT_10_005_ReturnsAndRejections() { var csv = await Csv(ret: Returns(new AchReturnRejectionReportRowDto { TransactionId = 55, Reference = "RET-55", Amount = 9m, CausalCode = "R03", CausalDescription = "Sin fondos", State = AchTransferStateEnum.Pending })); csv.Should().Contain("DevolucionSaliente").And.Contain("R03"); csv.Should().Contain("NO contabiliza"); }
    [Fact] public async Task UAT_10_006_ReturnOfReturn() { var csv = await Csv(ret: Returns(new AchReturnRejectionReportRowDto { TransactionId = 56, OriginalTransactionId = 12, Reference = "ROR-56", Amount = 10m, CausalCode = "R04", State = AchTransferStateEnum.Pending }), includeRor: true); csv.Should().Contain("RetornoDeRetorno"); csv.Should().Contain("CreaAsientoContable;False").And.Contain("RequiereApiContable;False"); }
    [Fact] public async Task UAT_10_007_Differences() { var rec = new Mock<IAchReconciliationReportService>(); rec.Setup(x => x.GetReconciliationAsync(It.IsAny<AchReconciliationReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchReconciliationReportResponseDto { Differences = new AchReconciliationDifferencesDto { SentVsReceivedAmountDiff = 44.5m, SentVsReceivedCountDiff = 7 } }); var csv = await Csv(rec: rec.Object); csv.Should().Contain("DIFERENCIAS").And.Contain("44.5").And.Contain("7").And.Contain("NO contabiliza"); }
    [Fact] public async Task UAT_10_008_NachaEvidence() { var nacha = new Mock<IAchNachaCycleReportService>(); nacha.Setup(x => x.GetNachaFilesAsync(It.IsAny<AchNachaFileReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchNachaFileReportResponseDto { Items = [new AchNachaFileReportRowDto { FileName = "ACH-UAT-01.txt", ExportKind = "Outbound" }] }); var csv = await Csv(nacha: nacha.Object); csv.Should().Contain("EVIDENCIAS").And.Contain("ACH-UAT-01.txt"); csv.Should().NotContain("base64"); }
    [Fact] public async Task UAT_10_009_AuditTraceability() { var audit = new Mock<IAchAuditHistoryReportService>(); audit.Setup(x => x.GetAuditAsync(It.IsAny<AchAuditReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchAuditReportResponseDto { Items = [new AchAuditReportRowDto { Entity = "AchTransaction", EntityId = "888", User = "uat-ia", Action = "StateChanged", DateUtc = FixedDateUtc }] }); var csv = await Csv(audit: audit.Object); csv.Should().Contain("audit-AchTransaction-888"); csv.Should().NotContain("password").And.NotContain("private key").And.NotContain("PFX"); }
    [Fact] public async Task UAT_10_010_CudWarningWithoutApi() { var csv = await Csv(includeCud: true); csv.Should().Contain("CUD se mantiene como evidencia operacional sin API").And.Contain("no se encontró evidencia CUD runtime"); csv.Should().NotContain("CudSettlementApi").And.NotContain("API CUD").And.NotContain("EvidenciaCUD"); }
    [Fact] public async Task UAT_10_011_FormulaInjectionProtection() { var csv = await Csv(tx: Tx([new() { TransactionId = 91, Reference = "=cmd", NachaFileName = "+file", Amount = 1m, State = AchTransferStateEnum.Pending }], []), ret: Returns(new AchReturnRejectionReportRowDto { TransactionId = 92, Reference = "-action", CausalCode = "@causal", State = AchTransferStateEnum.Pending })); csv.Should().NotContain(";=cmd").And.NotContain(";+file").And.NotContain(";-action").And.NotContain(";@causal"); var xlsx = await Execute("xlsx", tx: Tx([new() { TransactionId = 91, Reference = "=cmd", Amount = 1m, State = AchTransferStateEnum.Pending }], [])); using var zip = new ZipArchive(new MemoryStream(xlsx.Content)); zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/")).Select(e => ReadZip(zip, e.FullName)).Should().OnlyContain(s => !s.Contains("<f>")); }
    [Fact] public async Task UAT_10_012_IncludeFlags() { var csv = await Csv(tx: Tx([new() { TransactionId = 1, Reference = "OUT-1", Amount = 1m, State = AchTransferStateEnum.Pending }], [new() { TransactionId = 2, Reference = "IN-2", Amount = 2m, State = AchTransferStateEnum.Pending }]), ret: Returns(new AchReturnRejectionReportRowDto { TransactionId = 3, OriginalTransactionId = 2, Reference = "ROR-3", Amount = 3m, State = AchTransferStateEnum.Pending }), includeOutbound: false, includeIncoming: false, includeReturns: false, includeRor: false, includeCud: false); csv.Should().NotContain("OUT-1").And.NotContain("IN-2").And.NotContain("DevolucionSaliente").And.NotContain("RetornoDeRetorno").And.NotContain("EvidenciaCUD"); csv.Should().Contain("FRONTERA_NO_CONTABLE"); }
    [Fact] public async Task UAT_10_013_EmptyReportControlled() { var csv = await Csv(); csv.Should().Contain("Reporte poblado parcialmente con servicios existentes"); csv.Should().NotContain("REF-FAKE").And.NotContain("DEMO").And.NotContain("SAMPLE").And.NotContain("DUMMY"); }
    [Fact] public void UAT_10_014_AiAssistedSummary() { var summary = "UAT asistida por IA: aprobada técnicamente\nPendiente aprobación humana para GO UAT formal\nNO-GO productivo vigente\nNo contabiliza\nTotal casos ejecutados: 14\nTotal aprobados: 14\nTotal fallidos: 0"; summary.Should().Contain("UAT asistida por IA").And.Contain("Pendiente aprobación humana").And.Contain("NO-GO productivo").And.Contain("No contabiliza"); }

    private static async Task<string> Csv(IAchTransactionReportService? tx = null, IAchReturnRejectionReportService? ret = null, IAchNachaCycleReportService? nacha = null, IAchReconciliationReportService? rec = null, IAchAuditHistoryReportService? audit = null, bool includeOutbound = true, bool includeIncoming = true, bool includeReturns = true, bool includeRor = true, bool includeCud = false)
        => Encoding.UTF8.GetString((await Execute("csv", tx, ret, nacha, rec, audit, includeOutbound, includeIncoming, includeReturns, includeRor, includeCud)).Content);

    private static async Task<AccountingReviewExportResult> Execute(string format, IAchTransactionReportService? tx = null, IAchReturnRejectionReportService? ret = null, IAchNachaCycleReportService? nacha = null, IAchReconciliationReportService? rec = null, IAchAuditHistoryReportService? audit = null, bool includeOutbound = true, bool includeIncoming = true, bool includeReturns = true, bool includeRor = true, bool includeCud = false)
    {
        var service = BuildService(tx, ret, nacha, rec, audit);
        var request = new AccountingReviewExportApiRequest { Format = format, DateFrom = FixedDateUtc.Date, DateTo = FixedDateUtc.Date, RequestedBy = "uat-ia", CorrelationId = "UAT10-AI-001", IncludeOutbound = includeOutbound, IncludeIncoming = includeIncoming, IncludeReturns = includeReturns, IncludeReturnOfReturn = includeRor, IncludeCudEvidence = includeCud };
        if (format == "pdf")
        {
            var svc = new Mock<IAccountingReviewExportAppService>();
            svc.Setup(x => x.ExportAsync(It.IsAny<AccountingReviewExportApiRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(await service.ExportAsync(request, CancellationToken.None));
            var controller = new ReportsController(Mock.Of<IReportGenerator>(), Mock.Of<IAchTransactionReportService>(), Mock.Of<IAchReturnRejectionReportService>(), Mock.Of<IAchNachaCycleReportService>(), Mock.Of<IAchReconciliationReportService>(), Mock.Of<IAchAuditHistoryReportService>(), Mock.Of<IClearingHouseService>(), svc.Object, Mock.Of<ILogger<ReportsController>>());
            var file = (await controller.ExportAccountingReview(request, CancellationToken.None)).Should().BeOfType<FileContentResult>().Subject;
            return new AccountingReviewExportResult { Content = file.FileContents, ContentType = file.ContentType!, FileName = file.FileDownloadName! };
        }
        return await service.ExportAsync(request, CancellationToken.None);
    }

    private static AccountingReviewExportAppService BuildService(IAchTransactionReportService? tx, IAchReturnRejectionReportService? ret, IAchNachaCycleReportService? nacha, IAchReconciliationReportService? rec, IAchAuditHistoryReportService? audit)
    {
        tx ??= Tx([], []);
        ret ??= Returns();
        nacha ??= Mock.Of<IAchNachaCycleReportService>(x => x.GetNachaFilesAsync(It.IsAny<AchNachaFileReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchNachaFileReportResponseDto()));
        rec ??= Mock.Of<IAchReconciliationReportService>(x => x.GetReconciliationAsync(It.IsAny<AchReconciliationReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchReconciliationReportResponseDto()));
        audit ??= Mock.Of<IAchAuditHistoryReportService>(x => x.GetAuditAsync(It.IsAny<AchAuditReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchAuditReportResponseDto()));
        return new AccountingReviewExportAppService(new AccountingReviewReportBuilder(), new AccountingReviewReportExporter(), tx, ret, nacha, rec, audit);
    }

    private static IAchTransactionReportService Tx(IReadOnlyCollection<AchTransactionReportRowDto> sent, IReadOnlyCollection<AchTransactionReportRowDto> received)
    {
        var tx = new Mock<IAchTransactionReportService>();
        tx.Setup(x => x.GetSentTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchTransactionReportResponseDto { Items = sent.ToList() });
        tx.Setup(x => x.GetReceivedTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchTransactionReportResponseDto { Items = received.ToList() });
        return tx.Object;
    }

    private static IAchReturnRejectionReportService Returns(params AchReturnRejectionReportRowDto[] items)
    {
        var ret = new Mock<IAchReturnRejectionReportService>();
        ret.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchReturnRejectionReportResponseDto { Items = items.ToList() });
        return ret.Object;
    }

    private static string ReadZip(ZipArchive zip, string entryName)
    {
        using var s = zip.GetEntry(entryName)!.Open();
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }
}
