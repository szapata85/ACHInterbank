using System.IO.Compression;
using System.Reflection;
using System.Text;
using Cfa.ACHInterbank.Application.Reports.Export.Implementation;
using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public class AccountingReviewReportExportTests
{
    [Fact]
    public void Exporter_ShouldExportPdf_WithNonAccountingBoundary()
    {
        var exporter = new AccountingReviewReportExporter();
        var report = CreateReport();

        var result = exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Pdf, RequestedBy = "qa" });

        result.Format.Should().Be(AccountingReviewExportFormat.Pdf);
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        result.Content.Should().NotBeEmpty();
        Encoding.UTF8.GetString(result.Content).Should().StartWith("%PDF");
        result.BoundaryFlags.IsAccountingPosting.Should().BeFalse();
        result.BoundaryFlags.IsOfficialLedger.Should().BeFalse();
        result.BoundaryFlags.IsJournalEntry.Should().BeFalse();
        result.BoundaryFlags.CreatesAccountingEntry.Should().BeFalse();
        result.BoundaryFlags.RequiresAccountingApi.Should().BeFalse();
    }

    [Fact]
    public void Exporter_ShouldExportCsv_WithSummaryRowsDifferencesEvidenceAndBoundary()
    {
        var exporter = new AccountingReviewReportExporter();
        var report = CreateReport();

        var result = exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Csv });
        var csv = Encoding.UTF8.GetString(result.Content);

        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().EndWith(".csv");
        csv.Should().Contain("SUMMARY").And.Contain("ROWS").And.Contain("DIFFERENCES").And.Contain("EVIDENCE").And.Contain("WARNINGS").And.Contain("BOUNDARY");
        csv.Should().Contain("NO contabiliza");
        csv.Should().Contain("ManualAuditOnly").And.Contain("Orphan").And.Contain("ReturnOfReturn").And.Contain("CudEvidence").And.Contain("Rejection");
        csv.Should().NotContain("LedgerId").And.NotContain("JournalId").And.NotContain("PostingId").And.NotContain("AccountingEntryId");
    }

    [Fact]
    public void Exporter_ShouldExportExcelXlsx_WithExpectedOpenXmlPackage()
    {
        var exporter = new AccountingReviewReportExporter();
        var report = CreateReport();

        var result = exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Excel });

        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.FileName.Should().EndWith(".xlsx");
        result.Content.Should().NotBeEmpty();

        using var ms = new MemoryStream(result.Content);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var entries = zip.Entries.Select(e => e.FullName).ToArray();

        entries.Should().Contain("[Content_Types].xml");
        entries.Should().Contain("_rels/.rels");
        entries.Should().Contain("xl/workbook.xml");
        entries.Should().Contain("xl/_rels/workbook.xml.rels");
        entries.Should().Contain("xl/styles.xml");
        entries.Should().Contain("xl/worksheets/sheet1.xml");
        entries.Should().Contain("xl/worksheets/sheet2.xml");
        entries.Should().Contain("xl/worksheets/sheet3.xml");
        entries.Should().Contain("xl/worksheets/sheet4.xml");
        entries.Should().Contain("xl/worksheets/sheet5.xml");
        entries.Should().Contain("xl/worksheets/sheet6.xml");
        entries.Should().Contain("xl/worksheets/sheet7.xml");
        entries.Should().NotContain("xl/vbaProject.bin");

        using var workbookStream = zip.GetEntry("xl/workbook.xml")!.Open();
        using var reader = new StreamReader(workbookStream);
        var workbookXml = reader.ReadToEnd();
        workbookXml.Should().Contain("name=\"Summary\"")
            .And.Contain("name=\"Scope\"")
            .And.Contain("name=\"Rows\"")
            .And.Contain("name=\"Differences\"")
            .And.Contain("name=\"Evidence\"")
            .And.Contain("name=\"Warnings\"")
            .And.Contain("name=\"Boundary\"");
    }

    [Fact]
    public void CsvExporter_ShouldEscapeValuesAndPreventFormulaInjection()
    {
        var exporter = new AccountingReviewReportExporter();
        var report = CreateReportWithDangerousText();

        var result = exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Csv, CsvDelimiter = ";" });
        var csv = Encoding.UTF8.GetString(result.Content);

        csv.Should().Contain("\"NO contabiliza; no ledger; no journal; no posting\"");
        csv.Should().Contain("'-minus");
        csv.Should().Contain("'@at");
        csv.Should().Contain("'=cmd");
        csv.Should().Contain("'+plus");
        csv.Should().Contain("'-minus");
        csv.Should().Contain("'@at");
        csv.Should().NotContain(";=cmd");
        csv.Should().NotContain(";+plus");
        csv.Should().NotContain(";-minus");
        csv.Should().NotContain(";@at");
    }

    [Fact]
    public void ExcelExporter_ShouldPreventFormulaInjection()
    {
        var exporter = new AccountingReviewReportExporter();
        var report = CreateReportWithDangerousText();

        var result = exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Excel });

        using var ms = new MemoryStream(result.Content);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        var worksheetsXml = zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal))
            .Select(e =>
            {
                using var s = e.Open();
                using var r = new StreamReader(s);
                return r.ReadToEnd();
            })
            .ToArray();

        var allXml = string.Join("\n", worksheetsXml);
        (allXml.Contains("&apos;=cmd", StringComparison.Ordinal) || allXml.Contains("'=cmd", StringComparison.Ordinal)).Should().BeTrue();
        (allXml.Contains("&apos;+plus", StringComparison.Ordinal) || allXml.Contains("'+plus", StringComparison.Ordinal)).Should().BeTrue();
        (allXml.Contains("&apos;-minus", StringComparison.Ordinal) || allXml.Contains("'-minus", StringComparison.Ordinal)).Should().BeTrue();
        (allXml.Contains("&apos;@at", StringComparison.Ordinal) || allXml.Contains("'@at", StringComparison.Ordinal)).Should().BeTrue();
        allXml.Should().NotContain("<f>");
    }

    [Theory]
    [InlineData(AccountingReviewExportFormat.Pdf, ".pdf")]
    [InlineData(AccountingReviewExportFormat.Csv, ".csv")]
    [InlineData(AccountingReviewExportFormat.Excel, ".xlsx")]
    public void Exporter_ShouldGenerateSafeFileNames(AccountingReviewExportFormat format, string ext)
    {
        var exporter = new AccountingReviewReportExporter();

        var result = exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = format });

        result.FileName.Should().NotContain("/");
        result.FileName.Should().NotContain("\\");
        result.FileName.Should().NotContain("..");
        result.FileName.Should().EndWith(ext);
    }

    [Fact]
    public void Exporter_ShouldNotPersistAnything()
    {
        var ctorParams = typeof(AccountingReviewReportExporter).GetConstructors()
            .SelectMany(c => c.GetParameters())
            .ToArray();
        ctorParams.Should().BeEmpty();

        var dbSetProps = typeof(AchDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .ToHashSet();
        dbSetProps.Should().NotContain(name => name.Contains("AccountingReviewExport", StringComparison.Ordinal));

        var hasConfig = typeof(AccountingReviewReportExporter).Assembly.GetTypes()
            .Any(t => t.Name.Contains("AccountingReviewExport", StringComparison.Ordinal) &&
                      t.GetInterfaces().Any(i => i.Name.StartsWith("IEntityTypeConfiguration", StringComparison.Ordinal)));
        hasConfig.Should().BeFalse();

        typeof(AccountingReviewReportExporter).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(m => m.GetMethodBody()?.LocalVariables ?? [])
            .Should().NotContain(v => v.LocalType == typeof(FileStream));
    }

    [Fact]
    public void Exporter_ShouldPreserveWarningsAndBoundaryFlags()
    {
        var exporter = new AccountingReviewReportExporter();
        var report = CreateReport();

        var result = exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Csv });

        result.Warnings.Should().BeEquivalentTo(report.Warnings);
        result.BoundaryFlags.Should().BeEquivalentTo(report.BoundaryFlags);
        result.BoundaryFlags.IsAccountingPosting.Should().BeFalse();
    }

    [Fact]
    public void Exporter_ShouldSupportManualAuditOnlyOrphanRorCudRows()
    {
        var exporter = new AccountingReviewReportExporter();
        var report = CreateReport();

        var csv = Encoding.UTF8.GetString(exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Csv }).Content);
        csv.Should().Contain("ManualAuditOnly").And.Contain("Orphan").And.Contain("ReturnOfReturn").And.Contain("CudEvidence").And.Contain("Rejection");

        var xlsx = exporter.Export(report, new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Excel }).Content;
        using var ms = new MemoryStream(xlsx);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        using var rowSheet = zip.GetEntry("xl/worksheets/sheet3.xml")!.Open();
        using var reader = new StreamReader(rowSheet);
        var rowsXml = reader.ReadToEnd();
        rowsXml.Should().Contain("ManualAuditOnly").And.Contain("Orphan").And.Contain("ReturnOfReturn").And.Contain("CudEvidence").And.Contain("Rejection");
    }

    [Fact]
    public void Exporter_ShouldRejectUnknownFormat_IfApplicable()
    {
        var exporter = new AccountingReviewReportExporter();
        var invalid = new AccountingReviewExportRequest { Format = (AccountingReviewExportFormat)999 };

        var act = () => exporter.Export(CreateReport(), invalid);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static AccountingReviewReportResult CreateReport()
        => new()
        {
            ReportId = Guid.NewGuid(),
            GeneratedAt = DateTimeOffset.UtcNow,
            GeneratedBy = "qa-user",
            Scope = new AccountingReviewScope { CycleId = "C1", FileName = "f1.ach" },
            Summary = new AccountingReviewReportSummary { TotalRows = 5, TotalAmount = 123.45m },
            Rows =
            [
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.ManualAuditOnly, Status = "Manual", IsManualAuditOnly = true, Amount = 1 },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.Orphan, Status = "Orphan", IsOrphan = true, Amount = 2 },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.ReturnOfReturn, Status = "ROR", IsReturnOfReturn = true, Amount = 3 },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.CudEvidence, Status = "CUD", IsCudEvidence = true, Amount = 4 },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.Rejection, Status = "Rejected", IsRejected = true, Amount = 5 }
            ],
            Differences = [new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Amount, Severity = AccountingReviewDifferenceSeverity.Warning, Description = "diff", DifferenceAmount = 1.11m }],
            Evidence = [new AccountingReviewEvidenceReference { EvidenceType = AccountingReviewEvidenceType.Report, ReferenceId = "ev-1", CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "qa" }],
            BoundaryFlags = AccountingReviewBoundaryFlags.Default,
            Warnings = ["warn-1", "warn-2"]
        };

    private static AccountingReviewReportResult CreateReportWithDangerousText()
    {
        var report = CreateReport();

        return new AccountingReviewReportResult
        {
            ReportId = report.ReportId,
            GeneratedAt = report.GeneratedAt,
            GeneratedBy = report.GeneratedBy,
            Scope = report.Scope,
            Summary = report.Summary,
            Rows =
            [
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.ManualAuditOnly, Status = "text;with:semicolon", Amount = 1m },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.Orphan, Status = "text\"with\"quotes", Amount = 2m },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.ReturnOfReturn, Status = "text with\nnewline", Amount = 3m },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.CudEvidence, Status = "=cmd", Amount = 4m },
                new AccountingReviewReportRow { RowType = AccountingReviewRowType.Rejection, Status = "+plus", Amount = 5m }
            ],
            Differences =
            [
                new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Status, Description = "-minus", DifferenceAmount = 0m },
                new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.CauseCode, Description = "@at", DifferenceAmount = 0m }
            ],
            Evidence = [new AccountingReviewEvidenceReference { EvidenceType = AccountingReviewEvidenceType.Report, ReferenceId = "=cmd", CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "qa" }],
            ExportMetadata = report.ExportMetadata,
            BoundaryFlags = report.BoundaryFlags,
            Warnings = ["+plus", "-minus", "@at"]
        };
    }
}
