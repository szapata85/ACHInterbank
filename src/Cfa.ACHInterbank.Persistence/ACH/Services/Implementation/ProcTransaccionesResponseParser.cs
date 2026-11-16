using System.Xml.Linq;
using Cfa.ACHInterbank.Application.Integrations.Contracts;
using System.Xml;
using System.IO;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ProcTransaccionesResponseParser : IProcTransaccionesResponseParser
{
    private static readonly HashSet<string> SuccessCodes = new(StringComparer.OrdinalIgnoreCase) { "0", "00", "OK", "SUCCESS" };

    private static readonly HashSet<string> PartialCodes = new(StringComparer.OrdinalIgnoreCase) { "PARTIAL", "WARN" };

    public ProcTransaccionesParsedResponse Parse(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
        {
            return new ProcTransaccionesParsedResponse(false, false, false, false, "EMPTY", "Respuesta vacía de Proc_Transacciones.", string.Empty);
        }

        try
        {
            // Never expand DTDs/external entities from bank-origin XML.
            using var reader = XmlReader.Create(new StringReader(responseXml), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                MaxCharactersInDocument = 1_000_000, MaxCharactersFromEntities = 0
            });
            var xml = XDocument.Load(reader, LoadOptions.None);
            XNamespace soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace contract = "http://tempuri.org/";
            var envelope = xml.Root;
            if (envelope is null || envelope.Name.LocalName != "Envelope"
                || envelope.Name.Namespace != soap11)
                return Invalid(responseXml, "INVALID_SOAP_ENVELOPE");
            if (envelope.Elements().Any(x => x.Name != envelope.Name.Namespace + "Body"
                && x.Name != envelope.Name.Namespace + "Header")
                || envelope.Elements(envelope.Name.Namespace + "Body").Count() != 1
                || envelope.Elements(envelope.Name.Namespace + "Header").Count() > 1)
                return Invalid(responseXml, "INVALID_SOAP_ENVELOPE");
            var body = envelope.Element(envelope.Name.Namespace + "Body");
            if (body is null || body.Elements().Count() != 1)
                return Invalid(responseXml, "INVALID_SOAP_BODY");
            var resultElement = body.Elements().Single();
            if (body.Descendants().Any(x => x.Name.LocalName == "Fault"))
                return Invalid(responseXml, "SOAP_FAULT");
            if (resultElement.Name != contract + "Proc_TransaccionesResponse")
                return Invalid(responseXml, "WRONG_SOAP_OPERATION");
            // XSD xsd0/xsd1 are bundled; additional internal requirements still
            // prohibit ambiguous / missing result codes, irrespective of minOccurs=0.
            var statusNodes = resultElement.Elements()
                .Where(x => x.Name.LocalName == "RTAACH").ToArray();
            if (resultElement.Descendants().Count(x => x.Name.LocalName == "RTAACH") != 1
                || statusNodes.Length != 1
                || statusNodes[0].HasElements
                || statusNodes[0].Name.Namespace != contract)
                return Invalid(responseXml, "MISSING_OR_AMBIGUOUS_RTAACH");
            var codes = statusNodes.Select(x => x.Value.Trim()).ToArray();
            if (codes.Length != 1 || string.IsNullOrWhiteSpace(codes[0]))
                return Invalid(responseXml, "MISSING_OR_AMBIGUOUS_RTAACH");
            var code = codes[0];
            var messages = resultElement.Descendants()
                .Where(x => x.Name.LocalName == "RTALOC").ToArray();
            if (messages.Length > 1 || messages.Any(x => x.Parent != resultElement || x.HasElements
                || x.Name.Namespace != contract))
                return Invalid(responseXml, "AMBIGUOUS_RTALOC");
            // Fail closed on additional fields even if an apparent success exists.
            if (resultElement.Elements().Any(x => x.Name.LocalName is not ("RTAACH" or "RTALOC")))
                return Invalid(responseXml, "UNEXPECTED_SOAP_RESPONSE_FIELD");
            try
            {
                OfficialSoapContracts.ValidateElement(resultElement, OfficialSoapContracts.Operation.ProcTransaccionesResponse);
            }
            catch (InvalidOperationException)
            {
                return Invalid(responseXml, "SOAP_XSD_INVALID");
            }
            var message = messages.SingleOrDefault()?.Value?.Trim() ?? string.Empty;
            var isSuccess = SuccessCodes.Contains(code);
            var isPartial = !isSuccess && PartialCodes.Contains(code);
            // Even an explicit timeout from the core may mean the bank applied funds.
            var isRetryable = false;
            var isFunctionalRejection = !isSuccess && !isPartial
                && !code.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase)
                && !code.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase);

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
                IsRetryable: false,
                ResponseCode: "PARSER_ERROR",
                ResponseMessage: ex.Message,
                RawResponse: responseXml);
        }
    }

    private static ProcTransaccionesParsedResponse Invalid(string xml, string code)
        => new(false, false, false, false, code, "Respuesta SOAP no verificable; conciliar antes de reenviar.", xml);

}
