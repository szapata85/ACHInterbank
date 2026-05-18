using System.IO.Compression;
using System.Text;
using Cfa.ACHInterbank.Application.Reports.Export.Implementation;
using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Implementation;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using FluentAssertions;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AccountingReviewExportPopulationTests
{
    [Fact]
    public async Task ExportAppService_ShouldPopulateReturnsAndRejections_FromReturnRejectionService()
    {
        var ret = new Mock<IAchReturnRejectionReportService>();
        ret.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchReturnRejectionReportResponseDto { Items = [new AchReturnRejectionReportRowDto { TransactionId = 101, Reference = "RET-101", Amount = 5000m, CausalCode = "R01", CausalDescription = "Cuenta inválida", State = AchTransferStateEnum.Pending, ClearingHouseName = "ACH Colombia", AchCycleName = "Ciclo 1" }] });

        var sut = BuildService(ret: ret.Object);
        var csv = await ExportCsv(sut, new AccountingReviewExportApiRequest { Format = "csv", IncludeReturns = true, IncludeReturnOfReturn = false, RequestedBy = "qa" });

        csv.Should().Contain("FILAS").And.Contain("R01");
        csv.Should().Contain("DevolucionSaliente").And.Contain("NO contabiliza");
        csv.Should().NotContain("LedgerId").And.NotContain("JournalId").And.NotContain("PostingId").And.NotContain("AccountingEntryId");
    }

    [Fact]
    public async Task ExportAppService_ShouldPopulateReturnOfReturn_WhenOriginalTransactionIdExists()
    {
        var ret = new Mock<IAchReturnRejectionReportService>();
        ret.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchReturnRejectionReportResponseDto { Items = [new AchReturnRejectionReportRowDto { TransactionId = 202, OriginalTransactionId = 100, Reference = "ROR-202", Amount = 7000m, CausalCode = "R02", State = AchTransferStateEnum.Pending }] });

        var sut = BuildService(ret: ret.Object);
        var csv = await ExportCsv(sut, new AccountingReviewExportApiRequest { Format = "csv", IncludeReturns = true, IncludeReturnOfReturn = true, RequestedBy = "qa" });

        csv.Should().Contain("RetornoDeRetorno").And.Contain("202").And.Contain("R02");
    }

    [Fact]
    public async Task ExportAppService_ShouldNotPopulateReturnOfReturn_WhenIncludeReturnOfReturnFalse()
    {
        var ret = new Mock<IAchReturnRejectionReportService>();
        ret.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchReturnRejectionReportResponseDto { Items = [new AchReturnRejectionReportRowDto { TransactionId = 202, OriginalTransactionId = 100, Reference = "ROR-202", Amount = 7000m, CausalCode = "R02", State = AchTransferStateEnum.Pending }] });

        var sut = BuildService(ret: ret.Object);
        var csv = await ExportCsv(sut, new AccountingReviewExportApiRequest { Format = "csv", IncludeReturns = true, IncludeReturnOfReturn = false, RequestedBy = "qa" });

        csv.Should().Contain("DevolucionSaliente");
        csv.Should().NotContain("RetornoDeRetorno");
    }

    [Fact]
    public async Task ExportAppService_ShouldPopulateReconciliationDifferences_FromReconciliationService()
    {
        var rec = new Mock<IAchReconciliationReportService>();
        rec.Setup(x => x.GetReconciliationAsync(It.IsAny<AchReconciliationReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchReconciliationReportResponseDto { Differences = new AchReconciliationDifferencesDto { SentVsReceivedAmountDiff = 1234.56m, SentVsReceivedCountDiff = 2 } });

        var sut = BuildService(rec: rec.Object);
        var csv = await ExportCsv(sut, new AccountingReviewExportApiRequest { Format = "csv", RequestedBy = "qa" });

        csv.Should().Contain("DIFERENCIAS").And.Contain("Diferencia monto enviados vs recibidos").And.Contain("Diferencia conteo enviados vs recibidos").And.Contain("1234.56").And.Contain("NO contabiliza");
    }

    [Fact]
    public async Task ExportAppService_ShouldPopulateNachaAndAuditEvidence_AndCudWarning()
    {
        var nacha = new Mock<IAchNachaCycleReportService>();
        nacha.Setup(x => x.GetNachaFilesAsync(It.IsAny<AchNachaFileReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchNachaFileReportResponseDto { Items = [new AchNachaFileReportRowDto { FileName = "ACH-OUT-001.txt", ExportKind = "Outbound", ClearingHouseName = "ACH" }] });

        var audit = new Mock<IAchAuditHistoryReportService>();
        audit.Setup(x => x.GetAuditAsync(It.IsAny<AchAuditReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchAuditReportResponseDto { Items = [new AchAuditReportRowDto { Entity = "AchTransaction", EntityId = "777", Action = "StateChanged", User = "operador.ach", DateUtc = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc) }] });

        var sut = BuildService(nacha: nacha.Object, audit: audit.Object);
        var csv = await ExportCsv(sut, new AccountingReviewExportApiRequest { Format = "csv", IncludeCudEvidence = true, RequestedBy = "qa" });

        csv.Should().Contain("EVIDENCIAS").And.Contain("ACH-OUT-001.txt").And.Contain("audit-AchTransaction-777");
        csv.Should().Contain("CUD se mantiene como evidencia operacional sin API").And.Contain("no se encontró evidencia CUD runtime");
        csv.Should().NotContain("CudSettlementApi").And.NotContain("API CUD").And.NotContain("EvidenciaCUD");
    }

    [Fact]
    public async Task ExportAppService_ShouldRespectCombinedIncludeFlags_AndBoundary()
    {
        var tx = new Mock<IAchTransactionReportService>();
        tx.Setup(x => x.GetSentTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchTransactionReportResponseDto { Items = [new AchTransactionReportRowDto { TransactionId = 1, Reference = "OUT-1", Amount = 10m, State = AchTransferStateEnum.Pending }] });
        tx.Setup(x => x.GetReceivedTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchTransactionReportResponseDto { Items = [new AchTransactionReportRowDto { TransactionId = 2, Reference = "IN-2", Amount = 20m, State = AchTransferStateEnum.Pending }] });

        var ret = new Mock<IAchReturnRejectionReportService>();
        ret.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchReturnRejectionReportResponseDto { Items = [new AchReturnRejectionReportRowDto { TransactionId = 3, OriginalTransactionId = 2, Reference = "RET-3", Amount = 30m, State = AchTransferStateEnum.Pending }] });

        var sut = BuildService(tx: tx.Object, ret: ret.Object);
        var csv = await ExportCsv(sut, new AccountingReviewExportApiRequest { Format = "csv", IncludeOutbound = true, IncludeIncoming = false, IncludeReturns = true, IncludeReturnOfReturn = false, IncludeManualAuditOnly = false, IncludeCudEvidence = false, RequestedBy = "qa" });

        csv.Should().Contain("10").And.Contain("30").And.Contain("FRONTERA_NO_CONTABLE");
        csv.Should().NotContain("IN-2").And.NotContain("RetornoDeRetorno").And.NotContain("AuditoriaManualSoloEvidencia").And.NotContain("EvidenciaCUD");
    }

    [Fact]
    public async Task PopulatedExport_ShouldPreserveSpanishAndFormulaInjectionAndNoInventedRows()
    {
        var tx = new Mock<IAchTransactionReportService>();
        tx.Setup(x => x.GetSentTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(
            new AchTransactionReportResponseDto { Items = [new AchTransactionReportRowDto { TransactionId = 9, Reference = "=cmd", NachaFileName = "+file", Amount = 99m, State = AchTransferStateEnum.Pending }] });
        tx.Setup(x => x.GetReceivedTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchTransactionReportResponseDto());

        var ret = new Mock<IAchReturnRejectionReportService>();
        ret.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AchReturnRejectionReportResponseDto());

        var sut = BuildService(tx: tx.Object, ret: ret.Object);
        var csv = await ExportCsv(sut, new AccountingReviewExportApiRequest { Format = "csv", RequestedBy = "qa" });

        csv.Should().Contain("RESUMEN").And.Contain("FILAS").And.Contain("DIFERENCIAS").And.Contain("EVIDENCIAS").And.Contain("ADVERTENCIAS").And.Contain("FRONTERA_NO_CONTABLE");
        csv.Should().NotContain("SUMMARY").And.NotContain("ROWS").And.NotContain("DIFFERENCES").And.NotContain("EVIDENCE").And.NotContain("WARNINGS").And.NotContain("BOUNDARY");
        csv.Should().Contain("99").And.NotContain(";=cmd");
        csv.Should().Contain("NO contabiliza").And.Contain("no genera asientos");
        csv.Should().NotContain("LedgerId").And.NotContain("JournalId").And.NotContain("PostingId").And.NotContain("AccountingEntryId").And.NotContain("AccountingPosted").And.NotContain("DebitAccount").And.NotContain("CreditAccount").And.NotContain("BookedAt").And.NotContain("PostedAt");
        csv.Should().NotContain("REF-FAKE").And.NotContain("DEMO").And.NotContain("SAMPLE").And.NotContain("DUMMY");

        var xlsx = await sut.ExportAsync(new AccountingReviewExportApiRequest { Format = "xlsx", RequestedBy = "qa" }, CancellationToken.None);
        using var zip = new ZipArchive(new MemoryStream(xlsx.Content), ZipArchiveMode.Read);
        zip.Entries.Select(e => e.FullName).Should().NotContain("xl/vbaProject.bin");
        var sheets = string.Join("\n", zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/")).Select(e => { using var s = e.Open(); using var r = new StreamReader(s); return r.ReadToEnd(); }));
        sheets.Should().NotContain("<f>");
    }

    private static async Task<string> ExportCsv(IAccountingReviewExportAppService sut, AccountingReviewExportApiRequest request)
        => Encoding.UTF8.GetString((await sut.ExportAsync(request, CancellationToken.None)).Content);

    private static AccountingReviewExportAppService BuildService(
        IAchTransactionReportService? tx = null,
        IAchReturnRejectionReportService? ret = null,
        IAchNachaCycleReportService? nacha = null,
        IAchReconciliationReportService? rec = null,
        IAchAuditHistoryReportService? audit = null)
    {
        nacha ??= Mock.Of<IAchNachaCycleReportService>(x => x.GetNachaFilesAsync(It.IsAny<AchNachaFileReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchNachaFileReportResponseDto()));
        rec ??= Mock.Of<IAchReconciliationReportService>(x => x.GetReconciliationAsync(It.IsAny<AchReconciliationReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchReconciliationReportResponseDto()));
        audit ??= Mock.Of<IAchAuditHistoryReportService>(x => x.GetAuditAsync(It.IsAny<AchAuditReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchAuditReportResponseDto()));
        tx ??= Mock.Of<IAchTransactionReportService>(x => x.GetSentTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchTransactionReportResponseDto()) && x.GetReceivedTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchTransactionReportResponseDto()));
        ret ??= Mock.Of<IAchReturnRejectionReportService>(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>()) == Task.FromResult(new AchReturnRejectionReportResponseDto()));

        return new AccountingReviewExportAppService(new AccountingReviewReportBuilder(), new AccountingReviewReportExporter(), tx, ret, nacha, rec, audit);
    }
}
