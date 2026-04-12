using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ContrapartidaSoapResponseParser : IContrapartidaSoapResponseParser
{
    public ContrapartidaSoapResponseParseResult Parse(string responseXml)
    {
        var code = ExtractResponseCode(responseXml);
        var normalizedCode = NormalizeResponseCode(code);
        var itemResults = ExtractItemResults(responseXml);

        var hasItemResults = itemResults.Count > 0;
        var itemSuccessCount = itemResults.Values.Count(x => x.IsSuccess);
        var itemFailedCount = itemResults.Count - itemSuccessCount;

        var batchSuccess = IsSuccessCode(normalizedCode);
        var partial = hasItemResults && itemSuccessCount > 0 && itemFailedCount > 0;
        var success = hasItemResults ? itemFailedCount == 0 : batchSuccess;

        return new ContrapartidaSoapResponseParseResult(
            normalizedCode,
            success,
            partial,
            itemResults);
    }

    private static IReadOnlyDictionary<int, ContrapartidaSoapItemResult> ExtractItemResults(string response)
    {
        var result = new Dictionary<int, ContrapartidaSoapItemResult>();

        if (string.IsNullOrWhiteSpace(response))
        {
            return result;
        }

        try
        {
            var xml = XDocument.Parse(response);
            var itemNodes = xml.Descendants()
                .Where(e => e.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase)
                            || e.Name.LocalName.Equals("TransactionResult", StringComparison.OrdinalIgnoreCase)
                            || e.Name.LocalName.Equals("ResultadoTransaccion", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var itemNode in itemNodes)
            {
                var transactionIdValue = GetNodeValue(itemNode, "TransactionId", "IdTransaccion", "id");
                if (!int.TryParse(transactionIdValue, out var transactionId) || transactionId <= 0)
                {
                    continue;
                }

                var code = NormalizeResponseCode(GetNodeValue(itemNode,
                    "Codigo", "CodigoRespuesta", "ResponseCode", "Code", "Estado", "ResultCode"));
                var message = GetNodeValue(itemNode, "Mensaje", "Message", "Descripcion", "Description");

                result[transactionId] = new ContrapartidaSoapItemResult(
                    transactionId,
                    code,
                    IsSuccessCode(code),
                    message);
            }
        }
        catch
        {
            return result;
        }

        return result;
    }

    private static string GetNodeValue(XElement parent, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var node = parent.Descendants()
                .FirstOrDefault(e => string.Equals(e.Name.LocalName, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(node?.Value))
            {
                return node.Value.Trim();
            }
        }

        return string.Empty;
    }

    private static string ExtractResponseCode(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return "UNKNOWN";
        }

        try
        {
            var xml = XDocument.Parse(response);
            var knownNodes = new[]
            {
                "Codigo", "CodigoRespuesta", "ResponseCode", "Code", "Estado", "ResultCode"
            };

            foreach (var nodeName in knownNodes)
            {
                var value = xml
                    .Descendants()
                    .FirstOrDefault(e => string.Equals(e.Name.LocalName, nodeName, StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }
        catch
        {
            // fallback regex
        }

        var match = Regex.Match(response, @"\b[A-Za-z][A-Za-z0-9]{1,9}\b", RegexOptions.IgnoreCase);
        return match.Success
            ? match.Value.Trim()
            : "UNKNOWN";
    }

    private static bool IsSuccessCode(string code)
        => string.Equals(code, "R96", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeResponseCode(string? responseCode)
    {
        if (string.IsNullOrWhiteSpace(responseCode))
        {
            return "UNKNOWN";
        }

        var value = responseCode.Trim().ToUpperInvariant();
        return value.Length <= 20 ? value : value[..20];
    }
}
