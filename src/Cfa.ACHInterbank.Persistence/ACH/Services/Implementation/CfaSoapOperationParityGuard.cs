using System.Xml.Linq;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Verifies the active contract-driven SOAP operation against the legacy wire baseline
/// before allowing an opt-in request to reach the transport. No payload is logged.
/// </summary>
internal static class CfaSoapOperationParityGuard
{
    private static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private const string TargetNamespace = "http://tempuri.org/";

    internal static void EnsureEquivalent(
        string method,
        string operationName,
        string soapAction,
        string resolvedOperationName,
        string resolvedNamespace,
        string resolvedSoapAction,
        string legacyXml,
        string contractXml)
    {
        if (!string.Equals(resolvedOperationName, operationName, StringComparison.Ordinal)
            || !string.Equals(resolvedNamespace, TargetNamespace, StringComparison.Ordinal)
            || !string.Equals(resolvedSoapAction, soapAction, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{method}_WIRE_PARITY_MISMATCH: operacion, namespace o SOAPAction no coincide con el baseline.");
        }

        var expected = ReadOperation(legacyXml, requireEnvelope: false);
        var actual = ReadOperation(contractXml, requireEnvelope: true);
        if (expected.Name != actual.Name
            || !expected.Elements().Select(Field).SequenceEqual(actual.Elements().Select(Field)))
        {
            throw new InvalidOperationException($"{method}_WIRE_PARITY_MISMATCH: nombres, orden, valores o semantica de nulos del SOAP difieren del baseline legacy.");
        }
    }

    private static (XName Name, string Value, string? Nil) Field(XElement element)
        => (element.Name, element.Value, element.Attribute(Xsi + "nil")?.Value);

    private static XElement ReadOperation(string xml, bool requireEnvelope)
    {
        var root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
        if (root.Name != Soap11 + "Envelope")
        {
            if (requireEnvelope)
            {
                throw new InvalidOperationException("SOAP_WIRE_PARITY_ENVELOPE_MISSING: falta SOAP 1.1 Envelope.");
            }

            return root;
        }

        var body = root.Element(Soap11 + "Body")
            ?? throw new InvalidOperationException("SOAP_WIRE_PARITY_BODY_MISSING: falta SOAP Body.");
        return body.Elements().Single();
    }
}
