using System.Text.RegularExpressions;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ProcContrapartidasResponseParser : IProcContrapartidasResponseParser
{
    private static readonly HashSet<string> SuccessCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "00", "OK", "SUCCESS"
    };

    private static readonly HashSet<string> RetryableCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TIMEOUT", "TEMP", "TEMPORARY", "R98", "R99", "E500", "UNKNOWN"
    };

    private static readonly HashSet<string> TechnicalAnomalyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "RE", "0", "SOAP_FAULT", "PARSER_ERROR", "EMPTY_RESPONSE", "SOAP_EXCEPTION"
    };

    public ProcContrapartidasParsedResponse Parse(string responseXml)
    {
        var raw = responseXml ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new ProcContrapartidasParsedResponse(
                IsSuccess: false,
                IsSoapFault: false,
                IsRetryable: true,
                IsFunctionalRejection: false,
                ErrorCode: "EMPTY_RESPONSE",
                ErrorMessage: "Respuesta vacía del servicio Proc_Contrapartidas.",
                RawResponse: raw,
                ResponseCode: "EMPTY_RESPONSE",
                ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
        }

        try
        {
            var xml = XDocument.Parse(raw);
            var ansStatus = xml.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("ANSST", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
            var ansCode = xml.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("ANCLC", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
            var ansTx = xml.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("ANSIDTX", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(ansStatus) || !string.IsNullOrWhiteSpace(ansCode))
            {
                var normalized = NormalizeResponseCode(ansStatus ?? ansCode);
                var successByContract = SuccessCodes.Contains(normalized) || string.Equals(ansCode, "00", StringComparison.OrdinalIgnoreCase);
                var item = !string.IsNullOrWhiteSpace(ansTx)
                    ? new Dictionary<int, ProcContrapartidasParsedItemResponse>
                    {
                        [1] = new ProcContrapartidasParsedItemResponse(1, successByContract, !successByContract && RetryableCodes.Contains(normalized), normalized, ansCode ?? ansStatus ?? string.Empty)
                    }
                    : new Dictionary<int, ProcContrapartidasParsedItemResponse>();

                var isTechnicalAnomaly = !successByContract && TechnicalAnomalyCodes.Contains(normalized);

                return new ProcContrapartidasParsedResponse(
                    IsSuccess: successByContract,
                    IsSoapFault: false,
                    IsRetryable: !successByContract && RetryableCodes.Contains(normalized),
                    IsFunctionalRejection: !successByContract && !isTechnicalAnomaly,
                    ErrorCode: successByContract ? string.Empty : (ansCode ?? normalized),
                    ErrorMessage: successByContract
                        ? string.Empty
                        : isTechnicalAnomaly
                            ? $"Proc_Contrapartidas respuesta tecnica/anomala: ANSST={ansStatus}, ANCLC={ansCode}"
                            : $"Proc_Contrapartidas rechazo: ANSST={ansStatus}, ANCLC={ansCode}",
                    RawResponse: raw,
                    ResponseCode: normalized,
                    ItemResults: item);
            }

            if (TryParseSoapFault(xml, out var faultCode, out var faultMessage, out var faultDetail))
            {
                var retryableFault = IsRetryableSoapFault(faultCode, faultMessage);
                return new ProcContrapartidasParsedResponse(
                    IsSuccess: false,
                    IsSoapFault: true,
                    IsRetryable: retryableFault,
                    IsFunctionalRejection: false,
                    ErrorCode: string.IsNullOrWhiteSpace(faultCode) ? "SOAP_FAULT" : faultCode,
                    ErrorMessage: string.IsNullOrWhiteSpace(faultMessage) ? "SOAP Fault en Proc_Contrapartidas." : faultMessage,
                    RawResponse: raw,
                    ResponseCode: string.IsNullOrWhiteSpace(faultCode) ? "SOAP_FAULT" : faultCode,
                    ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>(),
                    FaultCode: faultCode,
                    FaultDetail: faultDetail);
            }

            var responseCode = NormalizeResponseCode(ExtractResponseCode(xml, raw));
            var items = ExtractItemResults(xml);
            var hasItems = items.Count > 0;

            bool isSuccess = hasItems
                ? items.Values.All(i => i.IsSuccess)
                : SuccessCodes.Contains(responseCode);

            bool anyRetryableItem = items.Values.Any(i => i.IsRetryable);
            bool isFunctionalRejection = !isSuccess
                && !hasItems
                && !RetryableCodes.Contains(responseCode)
                && !TechnicalAnomalyCodes.Contains(responseCode)
                && !responseCode.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase);
            bool isRetryable = !isSuccess && (anyRetryableItem || RetryableCodes.Contains(responseCode));

            var errorCode = isSuccess ? string.Empty : responseCode;
            var errorMessage = isSuccess
                ? string.Empty
                : ExtractMessage(xml) ?? "Proc_Contrapartidas devolvió rechazo funcional.";

            return new ProcContrapartidasParsedResponse(
                IsSuccess: isSuccess,
                IsSoapFault: false,
                IsRetryable: isRetryable,
                IsFunctionalRejection: isFunctionalRejection,
                ErrorCode: errorCode,
                ErrorMessage: errorMessage,
                RawResponse: raw,
                ResponseCode: responseCode,
                ItemResults: items);
        }
        catch (Exception ex)
        {
            return new ProcContrapartidasParsedResponse(
                IsSuccess: false,
                IsSoapFault: false,
                IsRetryable: true,
                IsFunctionalRejection: false,
                ErrorCode: "PARSER_ERROR",
                ErrorMessage: $"No fue posible interpretar respuesta SOAP: {ex.Message}",
                RawResponse: raw,
                ResponseCode: "PARSER_ERROR",
                ItemResults: new Dictionary<int, ProcContrapartidasParsedItemResponse>());
        }
    }

    private static bool TryParseSoapFault(XDocument xml, out string code, out string message, out string detail)
    {
        code = string.Empty;
        message = string.Empty;
        detail = string.Empty;

        var fault = xml.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
        if (fault is null)
        {
            return false;
        }

        code = fault.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("faultcode", StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? string.Empty;
        message = fault.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("faultstring", StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? string.Empty;
        detail = fault.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("detail", StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? string.Empty;

        return true;
    }

    private static bool IsRetryableSoapFault(string faultCode, string faultMessage)
    {
        var normalizedCode = (faultCode ?? string.Empty).ToLowerInvariant();
        var normalizedMessage = (faultMessage ?? string.Empty).ToLowerInvariant();

        if (normalizedCode.Contains("server") || normalizedCode.Contains("receiver"))
        {
            return true;
        }

        if (normalizedCode.Contains("client") || normalizedCode.Contains("sender"))
        {
            return false;
        }

        return normalizedMessage.Contains("timeout")
               || normalizedMessage.Contains("tempor")
               || normalizedMessage.Contains("unavailable")
               || normalizedMessage.Contains("not available");
    }

    private static IReadOnlyDictionary<int, ProcContrapartidasParsedItemResponse> ExtractItemResults(XDocument xml)
    {
        var results = new Dictionary<int, ProcContrapartidasParsedItemResponse>();

        var itemNodes = xml.Descendants()
            .Where(e => e.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase)
                        || e.Name.LocalName.Equals("TransactionResult", StringComparison.OrdinalIgnoreCase)
                        || e.Name.LocalName.Equals("ResultadoTransaccion", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var node in itemNodes)
        {
            var transactionIdValue = GetNodeValue(node, "TransactionId", "IdTransaccion", "id");
            if (!int.TryParse(transactionIdValue, out var transactionId) || transactionId <= 0)
            {
                continue;
            }

            var code = NormalizeResponseCode(GetNodeValue(node,
                "Codigo", "CodigoRespuesta", "ResponseCode", "Code", "Estado", "ResultCode"));
            var message = GetNodeValue(node, "Mensaje", "Message", "Descripcion", "Description");

            var isSuccess = SuccessCodes.Contains(code);
            var retryable = !isSuccess && RetryableCodes.Contains(code);

            results[transactionId] = new ProcContrapartidasParsedItemResponse(
                transactionId,
                isSuccess,
                retryable,
                code,
                message);
        }

        return results;
    }

    private static string ExtractResponseCode(XDocument xml, string raw)
    {
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

        var match = Regex.Match(raw, @"\b[A-Za-z][A-Za-z0-9]{1,19}\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Trim() : "UNKNOWN";
    }

    private static string? ExtractMessage(XDocument xml)
    {
        var knownNodes = new[]
        {
            "Mensaje", "Message", "Descripcion", "Description", "ErrorMessage"
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

        return null;
    }

    private static string GetNodeValue(XElement parent, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var node = parent.Descendants().FirstOrDefault(e => string.Equals(e.Name.LocalName, candidate, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(node?.Value))
            {
                return node.Value.Trim();
            }
        }

        return string.Empty;
    }

    private static string NormalizeResponseCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "UNKNOWN";
        }

        var value = code.Trim().ToUpperInvariant();
        return value.Length <= 20 ? value : value[..20];
    }
}
