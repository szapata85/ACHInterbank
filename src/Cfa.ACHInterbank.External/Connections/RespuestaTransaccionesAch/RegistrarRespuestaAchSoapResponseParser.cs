using System.IO;
using System.Xml;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.Integrations.Contracts;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

internal sealed class RegistrarRespuestaAchSoapResponseParser
{
    public ResultadoRegistroRespuestaAch Parse(string soapResponseXml)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
            throw new InvalidOperationException("SOAP_AXON_EMPTY_RESPONSE");

        XDocument document;
        try
        {
            using var reader = XmlReader.Create(new StringReader(soapResponseXml), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1_000_000,
                MaxCharactersFromEntities = 0
            });
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException("SOAP_AXON_INVALID_XML", ex);
        }

        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace contract = OfficialSoapContracts.TargetNamespace;
        XNamespace data = OfficialSoapContracts.AxonDataNamespace;
        var envelope = document.Root;
        if (envelope?.Name != soap + "Envelope"
            || envelope.Elements().Any(x => x.Name != soap + "Header" && x.Name != soap + "Body")
            || envelope.Elements(soap + "Header").Count() > 1
            || envelope.Elements(soap + "Body").Count() != 1)
            throw new InvalidOperationException("SOAP_AXON_INVALID_ENVELOPE");

        var body = envelope.Element(soap + "Body")!;
        var children = body.Elements().ToArray();
        if (children.Length == 1 && children[0].Name == soap + "Fault")
            throw new InvalidOperationException("SOAP_AXON_FAULT_RESULT_UNCERTAIN");
        if (children.Length != 1 || children[0].Name != contract + "RegistrarRespuestaTransaccionResponse"
            || body.Descendants().Any(x => x.Name.LocalName == "Fault"))
            throw new InvalidOperationException("SOAP_AXON_WRONG_OPERATION_OR_CARDINALITY");

        var response = children[0];
        try
        {
            OfficialSoapContracts.ValidateElement(response, OfficialSoapContracts.Operation.RegistrarRespuestaResponse);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException("SOAP_AXON_XSD_INVALID", ex);
        }

        var results = response.Elements(contract + "RegistrarRespuestaTransaccionResult").ToArray();
        if (results.Length != 1 || response.Elements().Count() != 1
            || results[0].Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance"))?.Value is "true" or "1")
            throw new InvalidOperationException("SOAP_AXON_RESULT_MISSING_OR_NIL");
        var result = results[0];
        var errorFields = result.Elements(data + "existeError").ToArray();
        if (errorFields.Length != 1 || string.IsNullOrWhiteSpace(errorFields[0].Value)
            || errorFields[0].Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance")) is not null)
            throw new InvalidOperationException("SOAP_AXON_ERROR_FLAG_MISSING");

        bool existeError;
        try
        {
            existeError = XmlConvert.ToBoolean(errorFields[0].Value.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("SOAP_AXON_ERROR_FLAG_INVALID", ex);
        }

        var codigoError = result.Element(data + "codigoError")?.Value;
        var descripcionError = result.Element(data + "descripcionError")?.Value;
        return new ResultadoRegistroRespuestaAch(existeError, codigoError, descripcionError);
    }
}
