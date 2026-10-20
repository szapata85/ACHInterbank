using System.Xml.Linq;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

/// <summary>
/// Adapts the common writer envelope to the existing WSAXON client contract, which accepts
/// the operation XML and owns SOAP 1.1 envelope creation itself.
/// </summary>
internal static class RegistrarRespuestaSoapOperationAdapter
{
    private static readonly XNamespace Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";

    public static string ExtractOperation(string envelopeXml)
    {
        if (string.IsNullOrWhiteSpace(envelopeXml))
        {
            throw new InvalidOperationException("REGISTRAR_RESPUESTA_SOAP_ENVELOPE_EMPTY: el writer contract-driven no produjo XML.");
        }

        var document = XDocument.Parse(envelopeXml, LoadOptions.PreserveWhitespace);
        var body = document.Root?.Element(Soap11Namespace + "Body")
            ?? throw new InvalidOperationException("REGISTRAR_RESPUESTA_SOAP_BODY_MISSING: el envelope contract-driven no contiene SOAP Body.");
        var operation = body.Elements().SingleOrDefault()
            ?? throw new InvalidOperationException("REGISTRAR_RESPUESTA_SOAP_OPERATION_INVALID: SOAP Body debe contener una única operación.");

        return operation.ToString(SaveOptions.DisableFormatting);
    }
}
