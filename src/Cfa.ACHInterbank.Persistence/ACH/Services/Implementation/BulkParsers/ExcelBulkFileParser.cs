using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.BulkParsers;

[Scoped]
public class ExcelBulkFileParser : IBulkFileParser
{
    public bool CanParse(BulkIngestionFileTypeEnum fileType) => fileType == BulkIngestionFileTypeEnum.Excel;

    public Task<ParsedFileResult> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart ?? throw new ArgumentException("El archivo Excel no contiene workbook válido.");
        var firstSheet = workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault()
            ?? throw new ArgumentException("El archivo Excel no contiene hojas.");

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheet.Id!);
        var rows = worksheetPart.Worksheet.Descendants<Row>().ToList();
        if (rows.Count == 0)
        {
            throw new ArgumentException("La hoja principal de Excel está vacía.");
        }

        var headerCells = rows[0].Elements<Cell>().ToList();
        var headers = headerCells
            .Select(cell => (GetCellValue(cell, workbookPart) ?? string.Empty).Trim())
            .ToArray();

        if (headers.All(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("No se detectaron encabezados en la primera fila del Excel.");
        }

        var items = new List<ParsedRawItem>();

        for (var r = 1; r < rows.Count; r++)
        {
            ct.ThrowIfCancellationRequested();
            var cells = rows[r].Elements<Cell>().ToList();
            if (cells.Count == 0)
            {
                continue;
            }

            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Length; c++)
            {
                var header = headers[c];
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                var cell = c < cells.Count ? cells[c] : null;
                var value = cell is null ? null : GetCellValue(cell, workbookPart);
                fields[header] = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }

            items.Add(new ParsedRawItem
            {
                Index = r + 1,
                Fields = fields
            });
        }

        return Task.FromResult(new ParsedFileResult
        {
            FileType = BulkIngestionFileTypeEnum.Excel,
            Items = items
        });
    }

    private static string? GetCellValue(Cell cell, WorkbookPart workbookPart)
    {
        var rawValue = cell.CellValue?.Text;
        if (rawValue is null)
        {
            return cell.InnerText;
        }

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            if (!int.TryParse(rawValue, out var index))
            {
                return rawValue;
            }

            return workbookPart.SharedStringTablePart?.SharedStringTable
                .Elements<SharedStringItem>()
                .ElementAtOrDefault(index)
                ?.InnerText;
        }

        return rawValue;
    }
}
