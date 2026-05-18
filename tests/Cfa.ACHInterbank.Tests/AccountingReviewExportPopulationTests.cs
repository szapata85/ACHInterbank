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
    public async Task ExportAppService_ShouldPopulateRows_FromTransactionReportService()
    {
        var tx = new Mock<IAchTransactionReportService>();
        tx.Setup(x => x.GetSentTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransactionReportResponseDto { Items = [new AchTransactionReportRowDto { TransactionId = 7, Reference = "REF-7", Amount = 123.45m, State = AchTransferStateEnum.Pending, AchCycleName = "C1", ClearingHouseName = "ACH" }] });
        tx.Setup(x => x.GetReceivedTransactionsAsync(It.IsAny<AchTransactionReportFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransactionReportResponseDto());

        var sut = BuildService(tx: tx.Object);
        var result = await sut.ExportAsync(new AccountingReviewExportApiRequest { Format = "csv", IncludeOutbound = true, IncludeIncoming = false, RequestedBy = "qa" }, CancellationToken.None);
        var csv = Encoding.UTF8.GetString(result.Content);

        csv.Should().Contain("FILAS").And.Contain("123.45").And.Contain("NO contabiliza");
    }

    [Fact]
    public async Task ExportAppService_ShouldRespectIncludeFlags_AndKeepSpanish()
    {
        var ret = new Mock<IAchReturnRejectionReportService>();
        ret.Setup(x => x.GetReturnsAsync(It.IsAny<AchReturnRejectionReportFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnRejectionReportResponseDto { Items = [new AchReturnRejectionReportRowDto { TransactionId = 99, Reference = "RET-99", Amount = 50m, State = AchTransferStateEnum.Pending, CausalCode = "R01", CausalDescription = "rechazo" }] });

        var sut = BuildService(ret: ret.Object);
        var result = await sut.ExportAsync(new AccountingReviewExportApiRequest { Format = "xlsx", IncludeReturns = false, IncludeCudEvidence = true, RequestedBy = "qa" }, CancellationToken.None);

        using var zip = new ZipArchive(new MemoryStream(result.Content), ZipArchiveMode.Read);
        using var ws = zip.GetEntry("xl/worksheets/sheet3.xml")!.Open();
        using var sr = new StreamReader(ws);
        var rowsXml = sr.ReadToEnd();
        rowsXml.Should().NotContain("RET-99");

        using var wb = zip.GetEntry("xl/workbook.xml")!.Open();
        using var wr = new StreamReader(wb);
        var wbXml = wr.ReadToEnd();
        wbXml.Should().Contain("Resumen").And.Contain("Filas").And.Contain("FronteraNoContable");
    }

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
