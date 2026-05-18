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
    public void Exporter_ShouldExportPdf_WithSpanishNonAccountingBoundary()
    {
        var exporter = new AccountingReviewReportExporter();
        var result = exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Pdf, RequestedBy = "qa" });
        var pdfText = Encoding.UTF8.GetString(result.Content);

        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        pdfText.Should().StartWith("%PDF");
        pdfText.Should().Contain("Reporte").And.Contain("No constituye asiento contable").And.Contain("mayor contable").And.Contain("libro diario").And.Contain("revisión contra terceros");
        result.BoundaryFlags.IsAccountingPosting.Should().BeFalse();
        result.BoundaryFlags.IsOfficialLedger.Should().BeFalse();
        result.BoundaryFlags.IsJournalEntry.Should().BeFalse();
        result.BoundaryFlags.CreatesAccountingEntry.Should().BeFalse();
        result.BoundaryFlags.RequiresAccountingApi.Should().BeFalse();
    }

    [Fact]
    public void Exporter_ShouldExportCsv_WithSpanishSectionsAndBoundary()
    {
        var exporter = new AccountingReviewReportExporter();
        var csv = Encoding.UTF8.GetString(exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Csv }).Content);

        csv.Should().Contain("RESUMEN").And.Contain("FILAS").And.Contain("DIFERENCIAS").And.Contain("EVIDENCIAS").And.Contain("ADVERTENCIAS").And.Contain("FRONTERA_NO_CONTABLE");
        csv.Should().Contain("NO contabiliza").And.Contain("no genera asientos").And.Contain("revisión de terceros");
        csv.Should().NotContain("SUMMARY").And.NotContain("ROWS").And.NotContain("DIFFERENCES").And.NotContain("EVIDENCE").And.NotContain("WARNINGS").And.NotContain("BOUNDARY");
    }

    [Fact]
    public void Exporter_ShouldExportCsv_WithSpanishRowTypes()
    {
        var exporter = new AccountingReviewReportExporter();
        var csv = Encoding.UTF8.GetString(exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Csv }).Content);

        csv.Should().Contain("AuditoriaManualSoloEvidencia").And.Contain("Huerfana").And.Contain("RetornoDeRetorno").And.Contain("EvidenciaCUD").And.Contain("Rechazo");
        csv.Should().NotContain("ManualAuditOnly").And.NotContain("Orphan").And.NotContain("ReturnOfReturn").And.NotContain("CudEvidence").And.NotContain("Rejection");
    }

    [Fact]
    public void Exporter_ShouldExportExcelXlsx_WithSpanishSheetNames()
    {
        var exporter = new AccountingReviewReportExporter();
        var content = exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Excel }).Content;

        using var zip = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
        using var workbookStream = zip.GetEntry("xl/workbook.xml")!.Open();
        using var reader = new StreamReader(workbookStream);
        var workbookXml = reader.ReadToEnd();

        workbookXml.Should().Contain("Resumen").And.Contain("Alcance").And.Contain("Filas").And.Contain("Diferencias").And.Contain("Evidencias").And.Contain("Advertencias").And.Contain("FronteraNoContable");
        workbookXml.Should().NotContain("Summary").And.NotContain("Scope").And.NotContain("Rows").And.NotContain("Differences").And.NotContain("Evidence").And.NotContain("Warnings").And.NotContain("Boundary");
    }

    [Fact]
    public void Exporter_ShouldExportExcelXlsx_WithSpanishBoundaryContent()
    {
        var exporter = new AccountingReviewReportExporter();
        var content = exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Excel }).Content;

        using var zip = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
        var allXml = string.Join("\n", zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)).Select(e => { using var s = e.Open(); using var r = new StreamReader(s); return r.ReadToEnd(); }));

        allXml.Should().Contain("NO contabiliza").And.Contain("No genera asientos").And.Contain("EsContabilizacion").And.Contain("EsMayorContableOficial").And.Contain("EsAsientoDiario").And.Contain("CreaAsientoContable").And.Contain("RequiereApiContable");
    }

    [Fact]
    public void CsvExporter_ShouldStillEscapeValuesAndPreventFormulaInjection()
    {
        var exporter = new AccountingReviewReportExporter();
        var csv = Encoding.UTF8.GetString(exporter.Export(CreateReportWithDangerousText(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Csv, CsvDelimiter = ";" }).Content);

        csv.Should().Contain("\"NO contabiliza; no genera asientos; no usa mayor contable; no usa libro diario; no realiza posting contable; es evidencia operativa para revisión de terceros\"");
        csv.Should().Contain("'=cmd").And.Contain("'+plus").And.Contain("'-minus").And.Contain("'@at");
        csv.Should().NotContain(";=cmd").And.NotContain(";+plus").And.NotContain(";-minus").And.NotContain(";@at");
    }

    [Fact]
    public void ExcelExporter_ShouldStillPreventFormulaInjection()
    {
        var exporter = new AccountingReviewReportExporter();
        var content = exporter.Export(CreateReportWithDangerousText(), new AccountingReviewExportRequest { Format = AccountingReviewExportFormat.Excel }).Content;

        using var zip = new ZipArchive(new MemoryStream(content), ZipArchiveMode.Read);
        var worksheetsXml = string.Join("\n", zip.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)).Select(e => { using var s = e.Open(); using var r = new StreamReader(s); return r.ReadToEnd(); }));

        worksheetsXml.Should().Contain("&apos;=cmd").And.Contain("&apos;+plus").And.Contain("&apos;-minus").And.Contain("&apos;@at");
        worksheetsXml.Should().NotContain("<f>");
        zip.Entries.Select(e => e.FullName).Should().NotContain("xl/vbaProject.bin");
    }

    [Fact]
    public void Exporter_ShouldNotPersistAnything()
    {
        var ctorParams = typeof(AccountingReviewReportExporter).GetConstructors().SelectMany(c => c.GetParameters()).ToArray();
        ctorParams.Should().BeEmpty();

        var dbSetProps = typeof(AchDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Microsoft.EntityFrameworkCore.DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0].Name)
            .ToHashSet();
        dbSetProps.Should().NotContain(name => name.Contains("AccountingReviewExport", StringComparison.Ordinal));

        var hasConfig = typeof(AccountingReviewReportExporter).Assembly.GetTypes()
            .Any(t => t.Name.Contains("AccountingReviewExport", StringComparison.Ordinal) && t.GetInterfaces().Any(i => i.Name.StartsWith("IEntityTypeConfiguration", StringComparison.Ordinal)));
        hasConfig.Should().BeFalse();
    }

    [Theory]
    [InlineData(AccountingReviewExportFormat.Pdf, ".pdf")]
    [InlineData(AccountingReviewExportFormat.Csv, ".csv")]
    [InlineData(AccountingReviewExportFormat.Excel, ".xlsx")]
    public void Exporter_ShouldGenerateSafeFileNames(AccountingReviewExportFormat format, string ext)
    {
        var exporter = new AccountingReviewReportExporter();
        var result = exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = format });

        result.FileName.Should().NotContain("/").And.NotContain("\\").And.NotContain("..").And.EndWith(ext);
    }

    [Fact]
    public void Exporter_ShouldRejectUnknownFormat_IfApplicable()
    {
        var exporter = new AccountingReviewReportExporter();
        var act = () => exporter.Export(CreateReport(), new AccountingReviewExportRequest { Format = (AccountingReviewExportFormat)999 });
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static AccountingReviewReportResult CreateReport() => new()
    {
        ReportId = Guid.NewGuid(), GeneratedAt = DateTimeOffset.UtcNow, GeneratedBy = "qa-user",
        Scope = new AccountingReviewScope { CycleId = "C1", FileName = "f1.ach" },
        Summary = new AccountingReviewReportSummary { TotalRows = 5, TotalAmount = 123.45m },
        Rows =
        [
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.ManualAuditOnly, Status = "Manual", IsManualAuditOnly = true, Amount = 1 },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.Orphan, Status = "Huerfana", IsOrphan = true, Amount = 2 },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.ReturnOfReturn, Status = "ROR", IsReturnOfReturn = true, Amount = 3 },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.CudEvidence, Status = "CUD", IsCudEvidence = true, Amount = 4 },
            new AccountingReviewReportRow { RowType = AccountingReviewRowType.Rejection, Status = "Rechazo", IsRejected = true, Amount = 5 }
        ],
        Differences = [new AccountingReviewDifference { DifferenceType = AccountingReviewDifferenceType.Amount, Severity = AccountingReviewDifferenceSeverity.Warning, Description = "diff", DifferenceAmount = 1.11m }],
        Evidence = [new AccountingReviewEvidenceReference { EvidenceType = AccountingReviewEvidenceType.Report, ReferenceId = "ev-1", CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "qa" }],
        BoundaryFlags = AccountingReviewBoundaryFlags.Default,
        Warnings = ["warn-1", "warn-2"]
    };

    private static AccountingReviewReportResult CreateReportWithDangerousText() => new()
    {
        ReportId = Guid.NewGuid(), GeneratedAt = DateTimeOffset.UtcNow, GeneratedBy = "qa-user",
        Scope = new AccountingReviewScope { CycleId = "C1", FileName = "f1.ach" },
        Summary = new AccountingReviewReportSummary { TotalRows = 5, TotalAmount = 123.45m },
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
        BoundaryFlags = AccountingReviewBoundaryFlags.Default,
        Warnings = ["+plus", "-minus", "@at"]
    };
}
