using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Cfa.ACHInterbank.Application.Integrations.Contracts;

/// <summary>
/// Offline, immutable WSCFAACH / WSAxon SOAP 1.1 XSD snapshots supplied with the
/// project. Schema validation is structural only: it is NOT banking homologation,
/// success-code interpretation, origin-account approval or authorization to send.
/// </summary>
public static class OfficialSoapContracts
{
    public const string TargetNamespace = "http://tempuri.org/";
    public const string AxonDataNamespace = "http://schemas.datacontract.org/2004/07/WSCFAACH";
    public const string CfaSoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones";
    public const string CfaContrapartidasSoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas";
    public const string AxonSoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion";

    private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";
    private const string WsdlNamespace = "http://schemas.xmlsoap.org/wsdl/";
    private const string WsdlSoapNamespace = "http://schemas.xmlsoap.org/wsdl/soap/";
    private const string WsdlActionNamespace = "http://www.w3.org/2006/05/addressing/wsdl";
    private static readonly Lazy<XmlSchemaSet> Cfa = new(() => LoadSchemas("WSCFAACH", ["xsd0.xml", "xsd1.xml"]));
    private static readonly Lazy<XmlSchemaSet> Axon = new(() => LoadSchemas("WSAxonRespuestaTransacciones", ["xsd0.xml", "xsd1.xml", "xsd2.xml"]));
    private static readonly Lazy<IReadOnlyList<string>> CfaRequestOrder = new(() => Array.AsReadOnly(ReadCfaFieldOrder()));

    public enum Operation
    {
        ProcTransaccionesRequest,
        ProcTransaccionesResponse,
        ProcContrapartidasRequest,
        ProcContrapartidasResponse,
        RegistrarRespuestaRequest,
        RegistrarRespuestaResponse
    }

    /// <summary>Schema-declared ordering; optional elements remain optional, including monetary input fields.</summary>
    public static IReadOnlyList<string> ProcTransaccionesFieldOrder => CfaRequestOrder.Value;

    public static void ValidateElement(XElement operation, Operation expected)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var (name, schemas) = expected switch
        {
            Operation.ProcTransaccionesRequest => ("Proc_Transacciones", Cfa.Value),
            Operation.ProcContrapartidasRequest => ("Proc_Contrapartidas", Cfa.Value),
            Operation.ProcContrapartidasResponse => ("Proc_ContrapartidasResponse", Cfa.Value),
            Operation.ProcTransaccionesResponse => ("Proc_TransaccionesResponse", Cfa.Value),
            Operation.RegistrarRespuestaRequest => ("RegistrarRespuestaTransaccion", Axon.Value),
            Operation.RegistrarRespuestaResponse => ("RegistrarRespuestaTransaccionResponse", Axon.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(expected))
        };

        if (operation.Name != XName.Get(name, TargetNamespace))
            throw new InvalidOperationException("SOAP_XSD_OPERATION_MISMATCH: operación o namespace distinto del WSDL.");
        try
        {
            // The input is already a parsed XElement; no parser-side schemaLocation,
            // DTD, remote import, external entity or inline schema processing is used.
            // An XElement requires an explicit schema declaration for partial validation.
            // Resolve only from the already compiled, bundled XSD set (no external lookup).
            var schemaElement = schemas.GlobalElements[new XmlQualifiedName(name, TargetNamespace)] as XmlSchemaElement
                ?? throw new InvalidOperationException("SOAP_XSD_ELEMENT_NOT_FOUND");
            operation.Validate(schemaElement, schemas,
                (_, e) => throw new XmlSchemaValidationException(e.Message), false);
        }
        catch (XmlSchemaException ex)
        {
            // Avoid leaking account numbers from validation messages into logs.
            throw new InvalidOperationException("SOAP_XSD_VALIDATION_FAILED: respuesta o solicitud incompatible con los XSD oficiales locales.", ex);
        }
    }

    /// <summary>
    /// Last outbound boundary for the two bank operations. Accepts a SOAP 1.1
    /// envelope or a single operation element; rejects extra body entries,
    /// wrong namespaces, DTDs and nonconforming XSD content before transport.
    /// Schema compliance is NOT authorization or evidence of banking homologation.
    /// </summary>
    public static void ValidateTransportRequest(string xml, Operation expected)
    {
        if (expected is not (Operation.ProcTransaccionesRequest or Operation.ProcContrapartidasRequest or Operation.RegistrarRespuestaRequest))
            throw new ArgumentOutOfRangeException(nameof(expected));
        if (string.IsNullOrWhiteSpace(xml))
            throw new InvalidOperationException("SOAP_XSD_REQUEST_EMPTY");

        XDocument document;
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 2_000_000,
                MaxCharactersFromEntities = 0
            });
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException("SOAP_XSD_REQUEST_INVALID_XML: XML SOAP no permitido.", ex);
        }

        var root = document.Root ?? throw new InvalidOperationException("SOAP_XSD_REQUEST_EMPTY");
        XNamespace soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
        XElement operation;
        if (root.Name == soap11 + "Envelope")
        {
            var bodies = root.Elements(soap11 + "Body").ToArray();
            var headers = root.Elements(soap11 + "Header").ToArray();
            if (bodies.Length != 1 || headers.Length > 1
                || root.Elements().Any(e => e.Name != soap11 + "Body" && e.Name != soap11 + "Header")
                || root.Elements().LastOrDefault() != bodies[0])
                throw new InvalidOperationException("SOAP_XSD_ENVELOPE_INVALID: Body SOAP 1.1 único requerido.");
            var entries = bodies[0].Elements().ToArray();
            if (entries.Length != 1 || bodies[0].Nodes().OfType<XText>().Any(t => !string.IsNullOrWhiteSpace(t.Value)))
                throw new InvalidOperationException("SOAP_XSD_BODY_INVALID: operación SOAP única requerida.");
            operation = entries[0];
        }
        else
        {
            operation = root;
        }
        ValidateElement(operation, expected);
    }

    /// <summary>Offline WSDL-to-XSD correspondence; call in automated tests or diagnostics, never via HTTP.</summary>
    public static void VerifyBundledWsdlContracts()
    {
        VerifyWsdl("WSCFAACH", "WSCFAACH.wsdl.xml", "Proc_Transacciones", "IWSCFAACH", CfaSoapAction, 2);
        VerifyWsdl("WSCFAACH", "WSCFAACH.wsdl.xml", "Proc_Contrapartidas", "IWSCFAACH", CfaContrapartidasSoapAction, 2);
        VerifyWsdl("WSAxonRespuestaTransacciones", "WSAxonRespuestaTransacciones.wsdl.xml",
            "RegistrarRespuestaTransaccion", "IWSAxonRespuestaTransacciones", AxonSoapAction, 3);
        _ = Cfa.Value;
        _ = Axon.Value;
        if (CfaRequestOrder.Value.Count != 27
            || CfaRequestOrder.Value[5] != "NCTAORIG"
            || CfaRequestOrder.Value[10] != "MONTO"
            || CfaRequestOrder.Value[8] != "FECEFEC"
            || CfaRequestOrder.Value[17] != "IDTRAN")
            throw new InvalidOperationException("SOAP_XSD_CFA_FIELD_ORDER_MISMATCH: revisar snapshot oficial.");
    }

    private static XmlSchemaSet LoadSchemas(string service, string[] filenames)
    {
        // Load every bundled XSD explicitly; do not follow the WSDL localhost URL.
        var schemaByNamespace = new Dictionary<string, XmlSchema>(StringComparer.Ordinal);
        var all = new List<XmlSchema>();
        foreach (var filename in filenames)
        {
            using var reader = OpenSafeXml(service, filename);
            var schema = XmlSchema.Read(reader, (_, e) => throw new XmlSchemaException(e.Message))
                ?? throw new InvalidOperationException("SOAP_XSD_EMPTY_RESOURCE");
            if (string.IsNullOrWhiteSpace(schema.TargetNamespace) || !schemaByNamespace.TryAdd(schema.TargetNamespace, schema))
                throw new InvalidOperationException("SOAP_XSD_DUPLICATE_NAMESPACE");
            all.Add(schema);
        }
        foreach (var schema in all)
        foreach (XmlSchemaExternal include in schema.Includes)
        {
            if (include is not XmlSchemaImport import
                || string.IsNullOrWhiteSpace(import.Namespace)
                || !schemaByNamespace.TryGetValue(import.Namespace, out var local))
                throw new InvalidOperationException("SOAP_XSD_UNRESOLVED_LOCAL_IMPORT: no se permite importación externa.");
            // Original files contain localhost:7083 schemaLocation. Relink only to
            // an actual bundled schema and NEVER consult that external address.
            import.SchemaLocation = null;
            import.Schema = local;
        }
        var set = new XmlSchemaSet { XmlResolver = null };
        set.ValidationEventHandler += (_, e) => throw new XmlSchemaException(e.Message);
        foreach (var schema in all)
            set.Add(schema);
        set.Compile();
        return set;
    }

    private static string[] ReadCfaFieldOrder()
    {
        var schema = LoadXml("WSCFAACH", "xsd0.xml");
        XNamespace xs = XsdNamespace;
        var request = schema.Root?.Elements(xs + "element")
            .Single(x => (string?)x.Attribute("name") == "Proc_Transacciones");
        return request?.Element(xs + "complexType")?.Element(xs + "sequence")?
            .Elements(xs + "element").Select(x => (string?)x.Attribute("name") ?? "").ToArray()
            ?? throw new InvalidOperationException("SOAP_XSD_CFA_REQUEST_ELEMENT_MISSING");
    }

    private static void VerifyWsdl(string service, string file, string operationName,
        string portType, string action, int importedSchemas)
    {
        var wsdl = LoadXml(service, file);
        XNamespace w = WsdlNamespace;
        XNamespace soap = WsdlSoapNamespace;
        XNamespace xs = XsdNamespace;
        if ((string?)wsdl.Root?.Attribute("targetNamespace") != TargetNamespace)
            throw new InvalidOperationException("SOAP_WSDL_NAMESPACE_MISMATCH");
        var binding = wsdl.Root?.Elements(w + "binding").SingleOrDefault(x =>
            (string?)x.Attribute("type") == "tns:" + portType)
            ?? throw new InvalidOperationException("SOAP_WSDL_BINDING_MISSING");
        if ((string?)binding.Element(soap + "binding")?.Attribute("transport") != "http://schemas.xmlsoap.org/soap/http")
            throw new InvalidOperationException("SOAP_WSDL_SOAP11_BINDING_REQUIRED");
        var op = binding.Elements(w + "operation").SingleOrDefault(x => (string?)x.Attribute("name") == operationName)
            ?? throw new InvalidOperationException("SOAP_WSDL_OPERATION_MISSING");
        if ((string?)op.Element(soap + "operation")?.Attribute("soapAction") != action
            || (string?)op.Element(soap + "operation")?.Attribute("style") != "document"
            || op.Elements(w + "input").Elements(soap + "body").SingleOrDefault()?.Attribute("use")?.Value != "literal"
            || op.Elements(w + "output").Elements(soap + "body").SingleOrDefault()?.Attribute("use")?.Value != "literal")
            throw new InvalidOperationException("SOAP_WSDL_ACTION_OR_LITERAL_MISMATCH");
        XNamespace wsaw = WsdlActionNamespace;
        var portOperation = wsdl.Root?.Elements(w + "portType")
            .SingleOrDefault(x => (string?)x.Attribute("name") == portType)?.Elements(w + "operation").SingleOrDefault(x => (string?)x.Attribute("name") == operationName)
            ?? throw new InvalidOperationException("SOAP_WSDL_PORTTYPE_OPERATION_MISSING");
        if ((string?)portOperation.Element(w + "input")?.Attribute(wsaw + "Action") != action
            || (string?)portOperation.Element(w + "output")?.Attribute(wsaw + "Action") != action + "Response")
            throw new InvalidOperationException("SOAP_WSDL_PORTTYPE_ACTION_MISMATCH");

        foreach (var name in new[] { operationName, operationName + "Response" })
        {
            var matching = wsdl.Root?.Elements(w + "message").Where(m => m.Elements(w + "part")
                .Any(p => (string?)p.Attribute("element") == "tns:" + name)).ToArray();
            if (matching is not { Length: 1 })
                throw new InvalidOperationException("SOAP_WSDL_MESSAGE_ELEMENT_MISMATCH");
        }
        var imports = wsdl.Descendants(xs + "import").ToArray();
        if (imports.Length != importedSchemas || imports.Any(i =>
                !(((string?)i.Attribute("schemaLocation"))?.Contains("?xsd=xsd", StringComparison.Ordinal) ?? false)))
            throw new InvalidOperationException("SOAP_WSDL_XSD_IMPORT_INVENTORY_MISMATCH");
        // WSDL soap:address is *not* a configured endpoint. Never use localhost:7083 to transmit.
    }

    private static XDocument LoadXml(string service, string filename)
    {
        using var reader = OpenSafeXml(service, filename);
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static XmlReader OpenSafeXml(string service, string filename)
    {
        var logicalName = $"OfficialSoapSchemas.{service}.{filename}";
        var stream = typeof(OfficialSoapContracts).Assembly.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"SOAP_XSD_RESOURCE_MISSING: {logicalName}");
        return XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1_000_000,
            MaxCharactersFromEntities = 0,
            CloseInput = true
        });
    }
}
