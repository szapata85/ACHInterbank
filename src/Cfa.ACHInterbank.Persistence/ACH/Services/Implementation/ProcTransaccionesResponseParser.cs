using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ProcTransaccionesResponseParser : IProcTransaccionesResponseParser
{
    private static readonly HashSet<string> SuccessCodes = new(StringComparer.OrdinalIgnoreCase) { "0", "00", "OK", "SUCCESS" };
    private static readonly HashSet<string> RetryableCodes = new(StringComparer.OrdinalIgnoreCase) { "TIMEOUT", "SOAP_TIMEOUT", "TEMP", "503", "500" };
    private static readonly HashSet<string> PartialCodes = new(StringComparer.OrdinalIgnoreCase) { "PARTIAL", "WARN" };

    public ProcTransaccionesParsedResponse Parse(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
        {
            return new ProcTransaccionesParsedResponse(false, false, false, true, "EMPTY", "Respuesta vacía de Proc_Transacciones.", string.Empty);
        }

        try
        {
            var xml = XDocument.Parse(responseXml);
            var code = ExtractValue(xml, "RTAACH")
                       ?? ExtractValue(xml, "RTALOC")
                       ?? ExtractValue(xml, "ANSST")
                       ?? ExtractValue(xml, "Codigo")
                       ?? ExtractValue(xml, "Code")
                       ?? "UNKNOWN";
            var message = ExtractValue(xml, "RTALOC")
                          ?? ExtractValue(xml, "ANSMEN")
                          ?? ExtractValue(xml, "Mensaje")
                          ?? ExtractValue(xml, "Message")
                          ?? string.Empty;

            var isSuccess = SuccessCodes.Contains(code);
            var isPartial = !isSuccess && PartialCodes.Contains(code);
            var isRetryable = !isSuccess && (RetryableCodes.Contains(code) || IsSoapFault(xml));
            var isFunctionalRejection = !isSuccess && !isRetryable;

            return new ProcTransaccionesParsedResponse(
                IsSuccess: isSuccess,
                IsPartialSuccess: isPartial,
                IsFunctionalRejection: isFunctionalRejection,
                IsRetryable: isRetryable,
                ResponseCode: code,
                ResponseMessage: message,
                RawResponse: responseXml);
        }
        catch (Exception ex)
        {
            return new ProcTransaccionesParsedResponse(
                IsSuccess: false,
                IsPartialSuccess: false,
                IsFunctionalRejection: false,
                IsRetryable: true,
                ResponseCode: "PARSER_ERROR",
                ResponseMessage: ex.Message,
                RawResponse: responseXml);
        }
    }

    private static bool IsSoapFault(XDocument xml)
    {
        return xml.Descendants().Any(x => x.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractValue(XDocument xml, string localName)
    {
        return xml.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
    }
}
