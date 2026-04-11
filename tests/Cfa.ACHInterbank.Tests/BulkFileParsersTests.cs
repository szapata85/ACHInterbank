using System.Text;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.BulkParsers;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class BulkFileParsersTests
{
    [Fact]
    public async Task JsonParser_ParsesTransactionsArray_FromObjectRoot()
    {
        var parser = new JsonBulkFileParser();
        var json = """
                   {
                     "transactions": [
                       { "reference": "A1", "amount": 1000 },
                       { "reference": "A2", "amount": 2000 }
                     ]
                   }
                   """;

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await parser.ParseAsync(stream);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("A1", result.Items[0].Fields["reference"]);
        Assert.Equal("2000", result.Items[1].Fields["amount"]);
    }

    [Fact]
    public async Task CsvParser_ParsesQuotedValues_AndRows()
    {
        var parser = new CsvBulkFileParser();
        const string csv = "reference,recipientName,amount\nREF-1,\"DOE, JOHN\",1500\nREF-2,ANA,1600";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await parser.ParseAsync(stream);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("DOE, JOHN", result.Items[0].Fields["recipientName"]);
        Assert.Equal("1600", result.Items[1].Fields["amount"]);
    }

    [Fact]
    public async Task ExcelParser_ParsesFirstSheet_WithHeadersAndRows()
    {
        var parser = new ExcelBulkFileParser();
        await using var stream = BuildExcelStream(
            ["reference", "amount"],
            ["REF-1", "1000"],
            ["REF-2", "2300"]);

        var result = await parser.ParseAsync(stream);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("REF-1", result.Items[0].Fields["reference"]);
        Assert.Equal("2300", result.Items[1].Fields["amount"]);
    }

    private static MemoryStream BuildExcelStream(string[] headers, params string[][] rows)
    {
        var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();

            var headerRow = new Row();
            foreach (var header in headers)
            {
                headerRow.Append(CreateTextCell(header));
            }

            sheetData.Append(headerRow);

            foreach (var row in rows)
            {
                var dataRow = new Row();
                foreach (var cell in row)
                {
                    dataRow.Append(CreateTextCell(cell));
                }

                sheetData.Append(dataRow);
            }

            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);
            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static Cell CreateTextCell(string value)
    {
        return new Cell
        {
            DataType = CellValues.String,
            CellValue = new CellValue(value)
        };
    }
}
