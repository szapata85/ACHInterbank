using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.BulkParsers;

[Scoped]
public class JsonBulkFileParser : IBulkFileParser
{
    public bool CanParse(BulkIngestionFileTypeEnum fileType) => fileType == BulkIngestionFileTypeEnum.Json;

    public async Task<ParsedFileResult> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var records = new List<ParsedRawItem>();
        var root = document.RootElement;

        JsonElement transactionsElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            transactionsElement = root;
        }
        else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("transactions", out var txArray))
        {
            transactionsElement = txArray;
        }
        else
        {
            throw new ArgumentException("El archivo JSON debe contener un arreglo raíz o una propiedad 'transactions'.");
        }

        if (transactionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("La propiedad 'transactions' debe ser un arreglo.");
        }

        var index = 1;
        foreach (var element in transactionsElement.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();

            if (element.ValueKind != JsonValueKind.Object)
            {
                records.Add(new ParsedRawItem
                {
                    Index = index++,
                    Fields = new Dictionary<string, string?> { ["__rowError"] = "Cada elemento de transactions debe ser un objeto." }
                });
                continue;
            }

            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                fields[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => property.Value.GetString(),
                    _ => property.Value.ToString()
                };
            }

            records.Add(new ParsedRawItem { Index = index++, Fields = fields });
        }

        return new ParsedFileResult
        {
            FileType = BulkIngestionFileTypeEnum.Json,
            Items = records
        };
    }
}
