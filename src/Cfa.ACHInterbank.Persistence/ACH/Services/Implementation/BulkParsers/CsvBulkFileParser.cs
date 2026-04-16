using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.BulkParsers;

[Scoped]
public class CsvBulkFileParser : IBulkFileParser
{
    public bool CanParse(BulkIngestionFileTypeEnum fileType) => fileType == BulkIngestionFileTypeEnum.Csv;

    public async Task<ParsedFileResult> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new ArgumentException("El archivo CSV no contiene encabezado.");
        }

        var headers = SplitCsvLine(headerLine)
            .Select(h => h.Trim())
            .ToArray();

        if (headers.Length == 0)
        {
            throw new ArgumentException("No se detectaron columnas en el CSV.");
        }

        var items = new List<ParsedRawItem>();
        var rowIndex = 1;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                break;
            }
            rowIndex++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = SplitCsvLine(line);
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Length; i++)
            {
                var value = i < values.Count ? values[i] : null;
                fields[headers[i]] = string.IsNullOrWhiteSpace(value) ? null : value;
            }

            items.Add(new ParsedRawItem
            {
                Index = rowIndex - 1,
                Fields = fields
            });
        }

        return new ParsedFileResult
        {
            FileType = BulkIngestionFileTypeEnum.Csv,
            Items = items
        };
    }

    private static List<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                values.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        values.Add(sb.ToString().Trim());
        return values;
    }
}
