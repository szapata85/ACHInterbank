using System.IO.Compression;
using System.Security;
using System.Text;
using System.Xml;
using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Models;
using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Export.Implementation;

public sealed class AccountingReviewReportExporter : IAccountingReviewReportExporter
{
    public AccountingReviewExportResult Export(AccountingReviewReportResult report, AccountingReviewExportRequest request)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var fileBase = $"accounting-review-reconciliation-{generatedAt:yyyyMMddHHmmss}";

        return request.Format switch
        {
            AccountingReviewExportFormat.Pdf => BuildResult(".pdf", "application/pdf", BuildPdf(report), request, report, generatedAt, fileBase),
            AccountingReviewExportFormat.Csv => BuildResult(".csv", "text/csv", BuildCsv(report, request), request, report, generatedAt, fileBase),
            AccountingReviewExportFormat.Excel => BuildResult(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", BuildXlsx(report), request, report, generatedAt, fileBase),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Format))
        };
    }

    private static AccountingReviewExportResult BuildResult(string ext, string contentType, byte[] content, AccountingReviewExportRequest request, AccountingReviewReportResult report, DateTimeOffset generatedAt, string fileBase)
        => new()
        {
            Format = request.Format,
            FileName = SafeFileName(fileBase + ext),
            ContentType = contentType,
            Content = content,
            GeneratedAt = generatedAt,
            GeneratedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? report.GeneratedBy : request.RequestedBy,
            BoundaryFlags = report.BoundaryFlags,
            Warnings = report.Warnings
        };

    private static string SafeFileName(string name)
    {
        var cleaned = name.Replace("/", "-").Replace("\\", "-").Replace("..", "-");
        foreach (var c in Path.GetInvalidFileNameChars()) cleaned = cleaned.Replace(c, '-');
        return cleaned;
    }

    private static byte[] BuildPdf(AccountingReviewReportResult report)
    {
        var lines = new List<string>
        {
            "Reporte de conciliación operativa y revisión contra terceros",
            $"Fecha de generación: {report.GeneratedAt:O}",
            $"Generado por: {report.GeneratedBy}",
            "Este reporte es evidencia operacional y soporte de revisión contra terceros. No constituye asiento contable, mayor contable, libro diario ni contabilización oficial.",
            "Resumen",
            $"Total de filas: {report.Summary.TotalRows}",
            $"Valor total: {report.Summary.TotalAmount}",
            "Advertencias",
            report.Warnings.Count == 0 ? "Sin advertencias" : string.Join(" | ", report.Warnings.Select(SanitizeFormula)),
            "Frontera no contable"
        };

        var text = string.Join("\n", lines);
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var pdf = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj\n4 0 obj<</Length {escaped.Length + 40}>>stream\nBT /F1 10 Tf 40 740 Td ({escaped}) Tj ET\nendstream endobj\n5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\nxref\n0 6\n0000000000 65535 f \ntrailer<</Root 1 0 R/Size 6>>\nstartxref\n0\n%%EOF";
        return Encoding.UTF8.GetBytes(pdf);
    }

    private static byte[] BuildCsv(AccountingReviewReportResult report, AccountingReviewExportRequest request)
    {
        var sb = new StringBuilder();
        var d = string.IsNullOrWhiteSpace(request.CsvDelimiter) ? ";" : request.CsvDelimiter;
        void W(params string[] vals) => sb.AppendLine(string.Join(d, vals.Select(v => EscapeCsv(SanitizeFormula(v), d))));

        W("SECCION", "RESUMEN");
        W("NO_CONTABLE", "NO contabiliza; no genera asientos; no usa mayor contable; no usa libro diario; no realiza posting contable; es evidencia operativa para revisión de terceros");
        W("TotalFilas", report.Summary.TotalRows.ToString());
        W("ValorTotal", report.Summary.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));

        W("SECCION", "FILAS");
        W("TipoFila", "IdTransaccion", "Valor", "Estado", "Causal", "Indicadores");
        foreach (var r in report.Rows)
            W(RowTypeLabel(r.RowType), r.TransactionId?.ToString() ?? "", r.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture), r.Status ?? "", r.CauseCode ?? "", $"huerfana={r.IsOrphan};auditoriaManual={r.IsManualAuditOnly};retornoDeRetorno={r.IsReturnOfReturn};evidenciaCud={r.IsCudEvidence}");

        W("SECCION", "DIFERENCIAS");
        foreach (var x in report.Differences)
            W(x.DifferenceType.ToString(), x.Severity.ToString(), x.Description, x.DifferenceAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));

        W("SECCION", "EVIDENCIAS");
        foreach (var e in report.Evidence)
            W(e.EvidenceType.ToString(), e.ReferenceId, e.FileName ?? "", e.FileHash ?? "");

        W("SECCION", "ADVERTENCIAS");
        foreach (var w in report.Warnings) W(w);

        W("SECCION", "FRONTERA_NO_CONTABLE");
        W("EsContabilizacion", report.BoundaryFlags.IsAccountingPosting.ToString());
        W("EsMayorContableOficial", report.BoundaryFlags.IsOfficialLedger.ToString());
        W("EsAsientoDiario", report.BoundaryFlags.IsJournalEntry.ToString());
        W("CreaAsientoContable", report.BoundaryFlags.CreatesAccountingEntry.ToString());
        W("RequiereApiContable", report.BoundaryFlags.RequiresAccountingApi.ToString());

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value, string delimiter)
    {
        var needs = value.Contains('"') || value.Contains('\n') || value.Contains('\r') || value.Contains(delimiter);
        var v = value.Replace("\"", "\"\"");
        return needs ? $"\"{v}\"" : v;
    }

    private static string SanitizeFormula(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return "=+-@".Contains(value[0]) ? "'" + value : value;
    }

    private static byte[] BuildXlsx(AccountingReviewReportResult report)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            Add(zip, "[Content_Types].xml", ContentTypes());
            Add(zip, "_rels/.rels", RelsRoot());
            Add(zip, "xl/workbook.xml", Workbook());
            Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
            Add(zip, "xl/styles.xml", Styles());
            Add(zip, "xl/worksheets/sheet1.xml", Sheet(new[]
            {
                new[] { "NO contabiliza", "No genera asientos" },
                new[] { "NO contabiliza", "No usa mayor contable" },
                new[] { "NO contabiliza", "No usa libro diario" },
                new[] { "NO contabiliza", "No realiza posting contable" },
                new[] { "NO contabiliza", "Es evidencia operativa para revisión de terceros" },
                new[] { "TotalFilas", report.Summary.TotalRows.ToString() },
                new[] { "ValorTotal", report.Summary.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            }));
            Add(zip, "xl/worksheets/sheet2.xml", Sheet(new[] { new[] { "Ciclo", report.Scope.CycleId ?? "" }, new[] { "Archivo", report.Scope.FileName ?? "" } }));
            Add(zip, "xl/worksheets/sheet3.xml", Sheet(report.Rows.Select(r => new[] { RowTypeLabel(r.RowType), r.TransactionId?.ToString() ?? "", SanitizeFormula(r.ExternalReference ?? ""), r.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture), r.Status ?? "", r.CauseCode ?? "" })));
            Add(zip, "xl/worksheets/sheet4.xml", Sheet(report.Differences.Select(d => new[] { d.DifferenceType.ToString(), d.Severity.ToString(), SanitizeFormula(d.Description), d.DifferenceAmount.ToString(System.Globalization.CultureInfo.InvariantCulture) })));
            Add(zip, "xl/worksheets/sheet5.xml", Sheet(report.Evidence.Select(e => new[] { e.EvidenceType.ToString(), SanitizeFormula(e.ReferenceId), e.FileName ?? "", e.FileHash ?? "" })));
            Add(zip, "xl/worksheets/sheet6.xml", Sheet(report.Warnings.Select(w => new[] { SanitizeFormula(w) })));
            Add(zip, "xl/worksheets/sheet7.xml", Sheet(new[] { new[] { "EsContabilizacion", report.BoundaryFlags.IsAccountingPosting.ToString() }, new[] { "EsMayorContableOficial", report.BoundaryFlags.IsOfficialLedger.ToString() }, new[] { "EsAsientoDiario", report.BoundaryFlags.IsJournalEntry.ToString() }, new[] { "CreaAsientoContable", report.BoundaryFlags.CreatesAccountingEntry.ToString() }, new[] { "RequiereApiContable", report.BoundaryFlags.RequiresAccountingApi.ToString() } }));
        }
        return ms.ToArray();
    }

    private static string RowTypeLabel(AccountingReviewRowType rowType)
        => rowType switch
        {
            AccountingReviewRowType.ManualAuditOnly => "AuditoriaManualSoloEvidencia",
            AccountingReviewRowType.Orphan => "Huerfana",
            AccountingReviewRowType.ReturnOfReturn => "RetornoDeRetorno",
            AccountingReviewRowType.CudEvidence => "EvidenciaCUD",
            AccountingReviewRowType.Rejection => "Rechazo",
            AccountingReviewRowType.OutboundTransaction => "TransaccionSaliente",
            AccountingReviewRowType.IncomingReturn => "DevolucionEntrante",
            AccountingReviewRowType.OutboundReturn => "DevolucionSaliente",
            AccountingReviewRowType.Netting => "Neteo",
            AccountingReviewRowType.Liquidity => "Liquidez",
            _ => rowType.ToString()
        };

    private static void Add(ZipArchive zip, string path, string content)
    {
        var e = zip.CreateEntry(path);
        using var s = new StreamWriter(e.Open(), new UTF8Encoding(false));
        s.Write(content);
    }

    private static string ContentTypes() => "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet2.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet3.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet4.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet5.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet6.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/worksheets/sheet7.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>";
    private static string RelsRoot() => "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";
    private static string Workbook() => "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Resumen\" sheetId=\"1\" r:id=\"rId1\"/><sheet name=\"Alcance\" sheetId=\"2\" r:id=\"rId2\"/><sheet name=\"Filas\" sheetId=\"3\" r:id=\"rId3\"/><sheet name=\"Diferencias\" sheetId=\"4\" r:id=\"rId4\"/><sheet name=\"Evidencias\" sheetId=\"5\" r:id=\"rId5\"/><sheet name=\"Advertencias\" sheetId=\"6\" r:id=\"rId6\"/><sheet name=\"FronteraNoContable\" sheetId=\"7\" r:id=\"rId7\"/></sheets></workbook>";
    private static string WorkbookRels() => "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet2.xml\"/><Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet3.xml\"/><Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet4.xml\"/><Relationship Id=\"rId5\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet5.xml\"/><Relationship Id=\"rId6\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet6.xml\"/><Relationship Id=\"rId7\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet7.xml\"/><Relationship Id=\"rId8\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";
    private static string Styles() => "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"1\"><xf/></cellXfs></styleSheet>";

    private static string Sheet(IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        var r = 1;
        foreach (var row in rows)
        {
            sb.Append($"<row r=\"{r}\">");
            var c = 0;
            foreach (var val in row)
            {
                c++;
                var cell = XmlEscape(SanitizeFormula(val ?? string.Empty));
                sb.Append($"<c r=\"{Col(c)}{r}\" t=\"inlineStr\"><is><t>{cell}</t></is></c>");
            }
            sb.Append("</row>");
            r++;
        }
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string Col(int index)
    {
        var s = string.Empty;
        while (index > 0) { var m = (index - 1) % 26; s = (char)('A' + m) + s; index = (index - 1) / 26; }
        return s;
    }

    private static string XmlEscape(string text) => SecurityElement.Escape(text) ?? string.Empty;
}
