using System.IO.Compression;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Implementation;
using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AccountingReviewExportEndpointTests
{
    [Fact]
    public async Task Endpoint_ShouldReturnPdfFile_ForPdfFormat()
    {
        var svc = new Mock<IAccountingReviewExportAppService>();
        svc.Setup(x => x.ExportAsync(It.IsAny<AccountingReviewExportApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountingReviewExportResult { ContentType = "application/pdf", FileName = "a.pdf", Content = Encoding.UTF8.GetBytes("%PDF-1.4") });
        var controller = BuildController(svc.Object);

        var result = await controller.ExportAccountingReview(new AccountingReviewExportApiRequest { Format = "pdf" }, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        file.ContentType.Should().Be("application/pdf");
        file.FileDownloadName.Should().EndWith(".pdf");
        Encoding.UTF8.GetString(file.FileContents).Should().StartWith("%PDF");
        file.FileContents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Endpoint_ShouldReturnCsvFile_ForCsvFormat()
    {
        var content = Encoding.UTF8.GetBytes("SECCION;RESUMEN\nSECCION;FILAS\nSECCION;FRONTERA_NO_CONTABLE\nNO contabiliza");
        var svc = new Mock<IAccountingReviewExportAppService>();
        svc.Setup(x => x.ExportAsync(It.IsAny<AccountingReviewExportApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountingReviewExportResult { ContentType = "text/csv", FileName = "a.csv", Content = content });
        var controller = BuildController(svc.Object);

        var result = await controller.ExportAccountingReview(new AccountingReviewExportApiRequest { Format = "csv" }, CancellationToken.None);
        var file = Assert.IsType<FileContentResult>(result);
        var csv = Encoding.UTF8.GetString(file.FileContents);

        file.ContentType.Should().Be("text/csv");
        file.FileDownloadName.Should().EndWith(".csv");
        csv.Should().Contain("RESUMEN").And.Contain("FILAS").And.Contain("FRONTERA_NO_CONTABLE").And.Contain("NO contabiliza");
        csv.Should().NotContain("LedgerId").And.NotContain("JournalId").And.NotContain("PostingId").And.NotContain("AccountingEntryId");
    }

    [Fact]
    public async Task Endpoint_ShouldReturnExcelFile_ForExcelFormat()
    {
        var exporter = new AccountingReviewReportExporter();
        var xlsx = exporter.Export(new Cfa.ACHInterbank.Application.Reports.Models.AccountingReviewReportResult(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Excel }).Content;
        var svc = new Mock<IAccountingReviewExportAppService>();
        svc.Setup(x => x.ExportAsync(It.IsAny<AccountingReviewExportApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountingReviewExportResult { ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName = "a.xlsx", Content = xlsx });
        var controller = BuildController(svc.Object);

        var result = await controller.ExportAccountingReview(new AccountingReviewExportApiRequest { Format = "excel" }, CancellationToken.None);
        var file = Assert.IsType<FileContentResult>(result);

        file.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        file.FileDownloadName.Should().EndWith(".xlsx");

        using var zip = new ZipArchive(new MemoryStream(file.FileContents), ZipArchiveMode.Read);
        using var workbookStream = zip.GetEntry("xl/workbook.xml")!.Open();
        using var reader = new StreamReader(workbookStream);
        var workbook = reader.ReadToEnd();
        workbook.Should().Contain("Resumen").And.Contain("Alcance").And.Contain("Filas").And.Contain("Diferencias").And.Contain("Evidencias").And.Contain("Advertencias").And.Contain("FronteraNoContable");
        zip.Entries.Select(e => e.FullName).Should().NotContain("xl/vbaProject.bin");
    }

    [Theory]
    [InlineData("xlsx")]
    [InlineData("excel")]
    public async Task Endpoint_ShouldAcceptXlsxAlias(string format)
    {
        var svc = new Mock<IAccountingReviewExportAppService>();
        svc.Setup(x => x.ExportAsync(It.IsAny<AccountingReviewExportApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountingReviewExportResult { ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileName = "a.xlsx", Content = [1,2,3] });
        var controller = BuildController(svc.Object);

        var result = await controller.ExportAccountingReview(new AccountingReviewExportApiRequest { Format = format }, CancellationToken.None);
        Assert.IsType<FileContentResult>(result);
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("posting")]
    [InlineData("ledger")]
    [InlineData("journal")]
    [InlineData("asiento")]
    public async Task Endpoint_ShouldRejectInvalidFormat(string format)
    {
        var svc = new Mock<IAccountingReviewExportAppService>();
        svc.Setup(x => x.ExportAsync(It.IsAny<AccountingReviewExportApiRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Formato inválido. Use: pdf, csv, excel o xlsx."));
        var controller = BuildController(svc.Object);

        var result = await controller.ExportAccountingReview(new AccountingReviewExportApiRequest { Format = format }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Endpoint_ShouldNotPersistExport()
    {
        typeof(AccountingReviewExportAppService).GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType.Name)
            .Should().NotContain("AchDbContext");

        var dbSetProps = typeof(AchDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .ToHashSet();
        dbSetProps.Should().NotContain(name => name.Contains("AccountingReviewExport", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Endpoint_ShouldNotExposeSensitiveOrAccountingArtifacts()
    {
        var content = Encoding.UTF8.GetBytes("NO contabiliza; sin private key ni password ni PFX");
        var svc = new Mock<IAccountingReviewExportAppService>();
        svc.Setup(x => x.ExportAsync(It.IsAny<AccountingReviewExportApiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountingReviewExportResult { ContentType = "text/csv", FileName = "a.csv", Content = content });
        var controller = BuildController(svc.Object);

        var result = await controller.ExportAccountingReview(new AccountingReviewExportApiRequest { Format = "csv" }, CancellationToken.None);
        var file = Assert.IsType<FileContentResult>(result);
        var text = Encoding.UTF8.GetString(file.FileContents);

        text.Should().NotContain("LedgerId").And.NotContain("JournalId").And.NotContain("PostingId").And.NotContain("AccountingEntryId");
    }

    private static ReportsController BuildController(IAccountingReviewExportAppService exportService)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "qa")], "test"));
        var ctx = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        return new ReportsController(
            Mock.Of<IReportGenerator>(),
            Mock.Of<IAchTransactionReportService>(),
            Mock.Of<IAchReturnRejectionReportService>(),
            Mock.Of<IAchNachaCycleReportService>(),
            Mock.Of<IAchReconciliationReportService>(),
            Mock.Of<IAchAuditHistoryReportService>(),
            Mock.Of<IClearingHouseService>(),
            exportService,
            Mock.Of<ILogger<ReportsController>>())
        { ControllerContext = ctx };
    }
}
