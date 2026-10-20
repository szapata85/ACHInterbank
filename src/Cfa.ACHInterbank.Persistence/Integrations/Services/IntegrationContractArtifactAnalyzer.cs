using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

public sealed class IntegrationContractArtifactAnalyzer : IIntegrationContractArtifactAnalyzer
{
    private const string SupportedStatus = "Supported";
    private static readonly XNamespace Wsdl = "http://schemas.xmlsoap.org/wsdl/";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/wsdl/soap/";
    private static readonly XNamespace Soap12 = "http://schemas.xmlsoap.org/wsdl/soap12/";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    private static readonly HashSet<string> SupportedBuiltInTypes = new(StringComparer.Ordinal)
    {
        "string", "normalizedString", "token", "boolean", "byte", "short", "int", "long", "integer",
        "nonNegativeInteger", "positiveInteger", "unsignedByte", "unsignedShort", "unsignedInt", "unsignedLong",
        "decimal", "float", "double", "date", "dateTime", "time", "base64Binary", "hexBinary", "anyURI"
    };

    public Task<IntegrationContractArtifactAnalysisDto> AnalyzeAsync(
        AnalyzeIntegrationContractArtifactRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var fileName = NormalizeFileName(request.FileName);
        var content = request.Content ?? [];
        var issues = new List<IntegrationContractArtifactIssueDto>();

        if (fileName.Length > 260)
        {
            issues.Add(Issue("SOURCE_DOCUMENT_NAME_TOO_LONG", "El nombre local del artefacto excede 260 caracteres."));
        }

        if (content.Length == 0)
        {
            issues.Add(Issue("EMPTY_ARTIFACT", "El archivo WSDL/XSD está vacío."));
            return Task.FromResult(Result(fileName, IntegrationContractArtifactKindEnum.Unknown, content.Length, string.Empty, null, null, null, issues, []));
        }

        if (content.Length > IntegrationContractArtifactLimits.MaxArtifactBytes)
        {
            issues.Add(Issue(
                "ARTIFACT_TOO_LARGE",
                $"El artefacto excede el límite de {IntegrationContractArtifactLimits.MaxArtifactBytes} bytes."));
            return Task.FromResult(Result(fileName, IntegrationContractArtifactKindEnum.Unknown, content.Length, string.Empty, null, null, null, issues, []));
        }

        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".wsdl", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xsd", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Issue("UNSUPPORTED_FILE_TYPE", "Solo se aceptan artefactos locales .wsdl o .xsd."));
            return Task.FromResult(Result(fileName, IntegrationContractArtifactKindEnum.Unknown, content.Length, hash, null, null, null, issues, []));
        }

        XDocument document;
        try
        {
            document = ParseSecurely(content);
        }
        catch (XmlException ex)
        {
            issues.Add(Issue("UNSAFE_OR_MALFORMED_XML", SanitizeXmlError(ex)));
            return Task.FromResult(Result(fileName, IntegrationContractArtifactKindEnum.Unknown, content.Length, hash, null, null, null, issues, []));
        }
        catch (InvalidOperationException ex)
        {
            issues.Add(Issue("XML_LIMIT_EXCEEDED", ex.Message));
            return Task.FromResult(Result(fileName, IntegrationContractArtifactKindEnum.Unknown, content.Length, hash, null, null, null, issues, []));
        }

        ValidateDocumentLimits(document, issues);
        ValidateNoExternalReferences(document, issues);
        if (issues.Count != 0)
        {
            return Task.FromResult(Result(fileName, DetectKind(document), content.Length, hash, null, null, null, issues, []));
        }

        IntegrationContractArtifactAnalysisDto result;
        var kind = DetectKind(document);
        if (kind == IntegrationContractArtifactKindEnum.Wsdl11)
        {
            result = AnalyzeWsdl(fileName, content.Length, hash, document, request.OperationName, issues);
        }
        else if (kind == IntegrationContractArtifactKindEnum.XmlSchema)
        {
            result = AnalyzeStandaloneXsd(
                fileName,
                content.Length,
                hash,
                document,
                request.OperationName,
                request.StandaloneXsdSoapAction,
                issues);
        }
        else
        {
            issues.Add(Issue("UNSUPPORTED_ROOT", "El documento no es WSDL 1.1 ni XML Schema 1.0."));
            result = Result(fileName, kind, content.Length, hash, null, null, null, issues, []);
        }

        return Task.FromResult(result);
    }

    private static XDocument ParseSecurely(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = IntegrationContractArtifactLimits.MaxXmlCharacters,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            CloseInput = true
        };
        using var reader = XmlReader.Create(stream, settings);
        return XDocument.Load(reader, LoadOptions.SetLineInfo);
    }

    private static void ValidateDocumentLimits(XDocument document, ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        var root = document.Root;
        if (root is null)
        {
            issues.Add(Issue("EMPTY_XML", "El documento XML no contiene elemento raíz."));
            return;
        }

        var count = 0;
        foreach (var element in root.DescendantsAndSelf())
        {
            count++;
            if (count > IntegrationContractArtifactLimits.MaxXmlElements)
            {
                issues.Add(Issue(
                    "XML_ELEMENT_LIMIT_EXCEEDED",
                    $"El documento excede {IntegrationContractArtifactLimits.MaxXmlElements} elementos XML."));
                break;
            }

            var depth = element.Ancestors().Count() + 1;
            if (depth > IntegrationContractArtifactLimits.MaxXmlDepth)
            {
                issues.Add(Issue(
                    "XML_DEPTH_LIMIT_EXCEEDED",
                    $"El documento excede la profundidad XML máxima de {IntegrationContractArtifactLimits.MaxXmlDepth}."));
                break;
            }
        }
    }

    private static void ValidateNoExternalReferences(XDocument document, ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        foreach (var import in document.Descendants(Wsdl + "import"))
        {
            var location = (string?)import.Attribute("location") ?? "(sin location)";
            issues.Add(Issue(
                "EXTERNAL_REFERENCE_NOT_ALLOWED",
                $"wsdl:import no está permitido en análisis local fail-closed: {location}."));
        }

        foreach (var reference in document.Descendants()
                     .Where(element => element.Name == Xsd + "import"
                         || element.Name == Xsd + "include"
                         || element.Name == Xsd + "redefine"))
        {
            var location = (string?)reference.Attribute("schemaLocation") ?? "(sin schemaLocation)";
            issues.Add(Issue(
                "EXTERNAL_REFERENCE_NOT_ALLOWED",
                $"{reference.Name.LocalName} XSD no está permitido en análisis local fail-closed: {location}."));
        }
    }

    private static IntegrationContractArtifactAnalysisDto AnalyzeWsdl(
        string fileName,
        long sizeBytes,
        string hash,
        XDocument document,
        string? requestedOperation,
        List<IntegrationContractArtifactIssueDto> issues)
    {
        var definitions = document.Root!;
        var targetNamespace = RequiredAttribute(definitions, "targetNamespace", "WSDL_TARGET_NAMESPACE_MISSING", issues);

        if (definitions.Descendants(Soap12 + "binding").Any())
        {
            issues.Add(Issue("SOAP12_UNSUPPORTED", "SOAP 1.2 no está soportado por este importador; el runtime actual permanece SOAP 1.1."));
        }

        if (definitions.Descendants(Soap + "binding")
            .Any(binding => string.Equals((string?)binding.Attribute("style"), "rpc", StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(Issue("RPC_STYLE_UNSUPPORTED", "WSDL rpc/encoded no está soportado; solo document/literal escalar."));
        }

        if (definitions.Descendants(Soap + "body")
            .Any(body => !string.Equals((string?)body.Attribute("use") ?? "literal", "literal", StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(Issue("ENCODED_BODY_UNSUPPORTED", "SOAP body encoded no está soportado; solo use=literal."));
        }

        if (definitions.Descendants(Soap + "header").Any())
        {
            issues.Add(Issue("SOAP_HEADER_UNSUPPORTED", "soap:header no está representado por el modelo contractual actual."));
        }

        var portTypeOperations = definitions.Elements(Wsdl + "portType")
            .Elements(Wsdl + "operation")
            .Where(operation => !string.IsNullOrWhiteSpace((string?)operation.Attribute("name")))
            .ToList();

        var availableOperations = ResolveCompatibleOperationNames(
            fileName,
            sizeBytes,
            hash,
            document,
            requestedOperation,
            portTypeOperations);

        var operation = SelectOperation(portTypeOperations, requestedOperation, issues);
        if (operation is null || targetNamespace is null)
        {
            return Result(
                fileName,
                IntegrationContractArtifactKindEnum.Wsdl11,
                sizeBytes,
                hash,
                operation?.Attribute("name")?.Value,
                targetNamespace,
                null,
                issues,
                [],
                availableOperations);
        }

        var operationName = operation.Attribute("name")!.Value.Trim();
        var soapAction = ResolveSoapAction(definitions, operationName, issues);
        var parameters = ResolveWsdlInputParameters(definitions, operation, issues);
        ValidateParameterCount(parameters, issues);
        ValidateContractMetadata(operationName, targetNamespace, soapAction, issues);

        return Result(
            fileName,
            IntegrationContractArtifactKindEnum.Wsdl11,
            sizeBytes,
            hash,
            operationName,
            targetNamespace,
            soapAction,
            issues,
            parameters,
            availableOperations);
    }

    private static IReadOnlyCollection<string> ResolveCompatibleOperationNames(
        string fileName,
        long sizeBytes,
        string hash,
        XDocument document,
        string? requestedOperation,
        IReadOnlyCollection<XElement> operations)
    {
        if (!string.IsNullOrWhiteSpace(requestedOperation))
        {
            return operations
                .Where(operation => string.Equals((string?)operation.Attribute("name"), requestedOperation.Trim(), StringComparison.Ordinal))
                .Select(operation => operation.Attribute("name")!.Value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        if (operations.Count <= 1)
        {
            return operations
                .Select(operation => operation.Attribute("name")!.Value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        var compatible = new List<string>();
        foreach (var operationName in operations
                     .Select(operation => operation.Attribute("name")!.Value.Trim())
                     .Distinct(StringComparer.Ordinal))
        {
            var candidateIssues = new List<IntegrationContractArtifactIssueDto>();
            var candidate = AnalyzeWsdl(fileName, sizeBytes, hash, document, operationName, candidateIssues);
            if (candidate.Importable)
            {
                compatible.Add(operationName);
            }
        }

        return compatible;
    }

    private static IntegrationContractArtifactAnalysisDto AnalyzeStandaloneXsd(
        string fileName,
        long sizeBytes,
        string hash,
        XDocument document,
        string? operationName,
        string? soapAction,
        List<IntegrationContractArtifactIssueDto> issues)
    {
        var schema = document.Root!;
        var targetNamespace = RequiredAttribute(schema, "targetNamespace", "XSD_TARGET_NAMESPACE_MISSING", issues);
        var normalizedOperation = string.IsNullOrWhiteSpace(operationName) ? null : operationName.Trim();
        var normalizedSoapAction = string.IsNullOrWhiteSpace(soapAction) ? null : soapAction.Trim();

        if (normalizedOperation is null)
        {
            issues.Add(Issue(
                "XSD_OPERATION_REQUIRED",
                "Un XSD standalone no define una operación SOAP; OperationName explícito es obligatorio para importarlo."));
        }

        if (normalizedSoapAction is null)
        {
            issues.Add(Issue(
                "XSD_SOAP_ACTION_REQUIRED",
                "Un XSD standalone no define SoapAction; StandaloneXsdSoapAction explícito es obligatorio para importarlo."));
        }

        var parameters = ResolveStandaloneXsdParameters(schema, issues);
        ValidateParameterCount(parameters, issues);
        ValidateContractMetadata(normalizedOperation, targetNamespace, normalizedSoapAction, issues);
        return Result(
            fileName,
            IntegrationContractArtifactKindEnum.XmlSchema,
            sizeBytes,
            hash,
            normalizedOperation,
            targetNamespace,
            normalizedSoapAction,
            issues,
            parameters);
    }

    private static XElement? SelectOperation(
        IReadOnlyCollection<XElement> operations,
        string? requestedOperation,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        if (!string.IsNullOrWhiteSpace(requestedOperation))
        {
            var requested = requestedOperation.Trim();
            var matches = operations
                .Where(operation => string.Equals((string?)operation.Attribute("name"), requested, StringComparison.Ordinal))
                .ToList();
            if (matches.Count == 1)
            {
                return matches[0];
            }

            issues.Add(Issue("WSDL_OPERATION_NOT_FOUND", $"No existe una única operación WSDL '{requested}'."));
            return null;
        }

        if (operations.Count == 1)
        {
            return operations.Single();
        }

        issues.Add(Issue(
            "WSDL_OPERATION_AMBIGUOUS",
            operations.Count == 0
                ? "El WSDL no define operaciones en portType."
                : "El WSDL define múltiples operaciones; OperationName explícito es obligatorio."));
        return null;
    }

    private static string? ResolveSoapAction(
        XElement definitions,
        string operationName,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        var bindingOperations = definitions.Elements(Wsdl + "binding")
            .Where(binding => binding.Element(Soap + "binding") is not null)
            .Elements(Wsdl + "operation")
            .Where(operation => string.Equals((string?)operation.Attribute("name"), operationName, StringComparison.Ordinal))
            .ToList();

        if (bindingOperations.Count != 1)
        {
            issues.Add(Issue(
                "SOAP_BINDING_AMBIGUOUS",
                bindingOperations.Count == 0
                    ? $"No se encontró binding SOAP 1.1 para '{operationName}'."
                    : $"Se encontraron múltiples bindings SOAP 1.1 para '{operationName}'."));
            return null;
        }

        var action = (string?)bindingOperations[0].Element(Soap + "operation")?.Attribute("soapAction");
        if (string.IsNullOrWhiteSpace(action))
        {
            issues.Add(Issue("SOAP_ACTION_MISSING", $"No se encontró soapAction SOAP 1.1 para '{operationName}'."));
            return null;
        }

        return action.Trim();
    }

    private static IReadOnlyCollection<IntegrationContractArtifactParameterDto> ResolveWsdlInputParameters(
        XElement definitions,
        XElement operation,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        var input = operation.Element(Wsdl + "input");
        var messageAttribute = (string?)input?.Attribute("message");
        if (string.IsNullOrWhiteSpace(messageAttribute))
        {
            issues.Add(Issue("INPUT_MESSAGE_MISSING", "La operación WSDL no declara un input message soportado."));
            return [];
        }

        var messageName = QNameLocalName(messageAttribute);
        var messageNamespace = ResolveQNameNamespace(input!, messageAttribute);
        var definitionsNamespace = (string?)definitions.Attribute("targetNamespace") ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(messageNamespace)
            && !string.Equals(messageNamespace, definitionsNamespace, StringComparison.Ordinal))
        {
            issues.Add(Issue("INPUT_MESSAGE_NAMESPACE_UNSUPPORTED", $"El input message '{messageAttribute}' pertenece a un namespace externo no soportado."));
            return [];
        }

        var messages = definitions.Elements(Wsdl + "message")
            .Where(message => string.Equals((string?)message.Attribute("name"), messageName, StringComparison.Ordinal))
            .ToList();
        if (messages.Count != 1)
        {
            issues.Add(Issue("INPUT_MESSAGE_AMBIGUOUS", $"No se pudo resolver de forma única el mensaje '{messageName}'."));
            return [];
        }

        var parts = messages[0].Elements(Wsdl + "part").ToList();
        if (parts.Count != 1)
        {
            issues.Add(Issue("MULTIPART_MESSAGE_UNSUPPORTED", "Solo se soporta un único wsdl:part document/literal por input."));
            return [];
        }

        var elementRef = (string?)parts[0].Attribute("element");
        var typeRef = (string?)parts[0].Attribute("type");
        if (!string.IsNullOrWhiteSpace(elementRef) && !string.IsNullOrWhiteSpace(typeRef))
        {
            issues.Add(Issue("WSDL_PART_AMBIGUOUS", "wsdl:part no puede declarar element y type simultáneamente."));
            return [];
        }

        var schemas = definitions.Descendants(Xsd + "schema").ToList();
        if (!string.IsNullOrWhiteSpace(elementRef))
        {
            var elementName = QNameLocalName(elementRef);
            var elementNamespace = ResolveQNameNamespace(parts[0], elementRef);
            var candidateSchemas = schemas
                .Where(schema => string.IsNullOrWhiteSpace(elementNamespace)
                    || string.Equals((string?)schema.Attribute("targetNamespace"), elementNamespace, StringComparison.Ordinal))
                .ToList();
            var candidates = candidateSchemas
                .Elements(Xsd + "element")
                .Where(element => string.Equals((string?)element.Attribute("name"), elementName, StringComparison.Ordinal))
                .ToList();
            if (candidates.Count != 1)
            {
                issues.Add(Issue("WSDL_ELEMENT_AMBIGUOUS", $"No se pudo resolver de forma única el elemento '{elementName}'."));
                return [];
            }

            return ResolveElementAsParameterContainer(candidates[0], schemas, issues);
        }

        if (!string.IsNullOrWhiteSpace(typeRef))
        {
            var synthetic = new XElement(Xsd + "element", new XAttribute("name", parts[0].Attribute("name")?.Value ?? "value"), new XAttribute("type", typeRef));
            return ResolveElementAsParameterContainer(synthetic, schemas, issues);
        }

        issues.Add(Issue("WSDL_PART_UNSUPPORTED", "wsdl:part debe declarar element o type."));
        return [];
    }

    private static IReadOnlyCollection<IntegrationContractArtifactParameterDto> ResolveStandaloneXsdParameters(
        XElement schema,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        var schemas = new List<XElement> { schema };
        var globalElements = schema.Elements(Xsd + "element").ToList();
        if (globalElements.Count == 0)
        {
            issues.Add(Issue("XSD_NO_GLOBAL_ELEMENTS", "El XSD no define elementos globales para importar."));
            return [];
        }

        if (globalElements.Count != 1)
        {
            issues.Add(Issue(
                "XSD_ROOT_ELEMENT_AMBIGUOUS",
                "Un XSD standalone importable debe declarar exactamente un elemento global raíz."));
            return [];
        }

        return ResolveElementAsParameterContainer(globalElements[0], schemas, issues);
    }

    private static IReadOnlyCollection<IntegrationContractArtifactParameterDto> ResolveElementAsParameterContainer(
        XElement element,
        IReadOnlyCollection<XElement> schemas,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        if (element.Attribute("ref") is not null || element.Attribute("substitutionGroup") is not null)
        {
            issues.Add(Issue("XSD_REFERENCE_UNSUPPORTED", "xsd:element ref/substitutionGroup no está soportado."));
            return [];
        }

        var complexType = element.Element(Xsd + "complexType");
        var typeName = (string?)element.Attribute("type");
        if (complexType is null && !string.IsNullOrWhiteSpace(typeName))
        {
            var localType = QNameLocalName(typeName);
            var complexTypes = schemas
                .Elements(Xsd + "complexType")
                .Where(type => string.Equals((string?)type.Attribute("name"), localType, StringComparison.Ordinal))
                .ToList();
            if (complexTypes.Count > 1)
            {
                issues.Add(Issue("XSD_TYPE_AMBIGUOUS", $"El tipo complejo '{typeName}' no se pudo resolver de forma única."));
                return [];
            }

            complexType = complexTypes.SingleOrDefault();
        }

        if (complexType is null)
        {
            var scalar = ResolveScalarElement(element, schemas, 1, issues);
            return scalar is null ? [] : [scalar];
        }

        if (complexType.Descendants(Xsd + "choice").Any()
            || complexType.Descendants(Xsd + "all").Any()
            || complexType.Descendants(Xsd + "any").Any()
            || complexType.Descendants(Xsd + "anyAttribute").Any()
            || complexType.Descendants(Xsd + "attribute").Any()
            || complexType.Descendants(Xsd + "complexContent").Any()
            || complexType.Descendants(Xsd + "simpleContent").Any())
        {
            issues.Add(Issue(
                "XSD_CONSTRUCT_UNSUPPORTED",
                "Solo se soporta complexType con sequence plana de elementos escalares; choice/all/any/attributes/content extension no están soportados."));
            return [];
        }

        var sequence = complexType.Element(Xsd + "sequence");
        if (sequence is null)
        {
            issues.Add(Issue("XSD_SEQUENCE_REQUIRED", "El complexType soportado debe contener una sequence plana."));
            return [];
        }

        if (complexType.Attribute("mixed") is not null
            || sequence.Attribute("minOccurs") is not null
            || sequence.Attribute("maxOccurs") is not null)
        {
            issues.Add(Issue("XSD_SEQUENCE_CARDINALITY_UNSUPPORTED", "mixed y cardinalidad a nivel de sequence no están representados por el modelo contractual."));
            return [];
        }

        var schema = element.AncestorsAndSelf(Xsd + "schema").FirstOrDefault();
        if (schema is not null
            && !string.Equals((string?)schema.Attribute("elementFormDefault") ?? "unqualified", "qualified", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Issue("XSD_ELEMENT_FORM_UNSUPPORTED", "Los parámetros locales requieren elementFormDefault=qualified para representación contractual inequívoca."));
            return [];
        }

        if (sequence.Elements().Any(child => child.Name != Xsd + "element"))
        {
            issues.Add(Issue("XSD_SEQUENCE_UNSUPPORTED", "La sequence contiene constructos distintos de xsd:element."));
            return [];
        }

        var result = new List<IntegrationContractArtifactParameterDto>();
        var order = 0;
        foreach (var child in sequence.Elements(Xsd + "element"))
        {
            var parameter = ResolveScalarElement(child, schemas, ++order, issues);
            if (parameter is not null)
            {
                result.Add(parameter);
            }
        }

        return result;
    }

    private static IntegrationContractArtifactParameterDto? ResolveScalarElement(
        XElement element,
        IReadOnlyCollection<XElement> schemas,
        int sortOrder,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        var name = (string?)element.Attribute("name");
        if (string.IsNullOrWhiteSpace(name))
        {
            issues.Add(Issue("XSD_ELEMENT_NAME_MISSING", "Todo parámetro importable debe declarar name."));
            return null;
        }

        name = name.Trim();
        if (name.Length > 250)
        {
            issues.Add(Issue("XSD_ELEMENT_NAME_TOO_LONG", $"El parámetro '{name}' excede 250 caracteres."));
            return null;
        }

        if (element.Attribute("ref") is not null || element.Element(Xsd + "complexType") is not null)
        {
            issues.Add(Issue("XSD_NESTED_STRUCTURE_UNSUPPORTED", $"El parámetro '{name}' no es escalar soportado."));
            return null;
        }

        var minOccurs = ParseOccurs((string?)element.Attribute("minOccurs"), 1, false, name, issues);
        var maxOccurs = ParseOccurs((string?)element.Attribute("maxOccurs"), 1, true, name, issues);
        if (minOccurs is null || maxOccurs is null)
        {
            return null;
        }

        if (minOccurs > 1 || maxOccurs != 1 || minOccurs > maxOccurs)
        {
            issues.Add(Issue("XSD_COLLECTION_UNSUPPORTED", $"El parámetro '{name}' no tiene cardinalidad escalar 0..1 o 1..1."));
            return null;
        }

        var resolvedType = ResolveSimpleType(element, schemas, issues);
        if (resolvedType is null)
        {
            return null;
        }

        var nillableText = (string?)element.Attribute("nillable");
        var nillable = false;
        if (!string.IsNullOrWhiteSpace(nillableText) && !bool.TryParse(nillableText, out nillable))
        {
            issues.Add(Issue("XSD_NILLABLE_INVALID", $"El parámetro '{name}' contiene nillable inválido."));
            return null;
        }

        return new IntegrationContractArtifactParameterDto(
            name,
            name,
            name,
            MapDataType(resolvedType),
            $"xsd:{resolvedType}",
            IntegrationParameterDirectionEnum.Input,
            IntegrationParameterCardinalityEnum.Scalar,
            minOccurs > 0,
            nillable,
            minOccurs.Value,
            maxOccurs,
            SupportedStatus,
            sortOrder);
    }

    private static string? ResolveSimpleType(
        XElement element,
        IReadOnlyCollection<XElement> schemas,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        var typeName = (string?)element.Attribute("type");
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            var local = QNameLocalName(typeName);
            var ns = ResolveQNameNamespace(element, typeName);
            if (ns == Xsd.NamespaceName)
            {
                return RequireSupportedBuiltIn(local, element, issues);
            }

            var namedSimpleTypes = schemas
                .Elements(Xsd + "simpleType")
                .Where(type => string.Equals((string?)type.Attribute("name"), local, StringComparison.Ordinal))
                .ToList();
            if (namedSimpleTypes.Count != 1)
            {
                issues.Add(Issue("XSD_TYPE_UNRESOLVED", $"No se pudo resolver el tipo simple '{typeName}'."));
                return null;
            }

            return ResolveRestrictionBase(namedSimpleTypes[0], issues);
        }

        var inlineSimpleType = element.Element(Xsd + "simpleType");
        if (inlineSimpleType is not null)
        {
            return ResolveRestrictionBase(inlineSimpleType, issues);
        }

        issues.Add(Issue("XSD_TYPE_REQUIRED", $"El parámetro '{element.Attribute("name")?.Value}' debe declarar un tipo simple explícito."));
        return null;
    }

    private static string? ResolveRestrictionBase(
        XElement simpleType,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        if (simpleType.Elements().Any(child => child.Name != Xsd + "restriction"))
        {
            issues.Add(Issue("XSD_SIMPLE_TYPE_UNSUPPORTED", "Solo se soporta simpleType basado en restriction de un tipo XSD built-in."));
            return null;
        }

        var restriction = simpleType.Element(Xsd + "restriction");
        if (restriction?.Elements().Any() == true)
        {
            issues.Add(Issue("XSD_FACET_UNSUPPORTED", "Facetas de simpleType no están representadas por el modelo contractual actual."));
            return null;
        }

        var baseName = (string?)restriction?.Attribute("base");
        if (string.IsNullOrWhiteSpace(baseName))
        {
            issues.Add(Issue("XSD_RESTRICTION_BASE_MISSING", "simpleType/restriction debe declarar base."));
            return null;
        }

        var ns = ResolveQNameNamespace(restriction!, baseName);
        if (ns != Xsd.NamespaceName)
        {
            issues.Add(Issue("XSD_RESTRICTION_BASE_UNSUPPORTED", $"La base '{baseName}' no pertenece a XML Schema built-ins."));
            return null;
        }

        return RequireSupportedBuiltIn(QNameLocalName(baseName), simpleType, issues);
    }

    private static string? RequireSupportedBuiltIn(
        string localType,
        XElement context,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        if (SupportedBuiltInTypes.Contains(localType))
        {
            return localType;
        }

        issues.Add(Issue("XSD_TYPE_UNSUPPORTED", $"El tipo XSD '{localType}' no está soportado por el motor contractual actual."));
        return null;
    }

    private static int? ParseOccurs(
        string? value,
        int defaultValue,
        bool max,
        string parameterName,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (max && string.Equals(value, "unbounded", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Issue("XSD_COLLECTION_UNSUPPORTED", $"El parámetro '{parameterName}' declara maxOccurs=unbounded."));
            return null;
        }

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            return parsed;
        }

        issues.Add(Issue("XSD_OCCURS_INVALID", $"Ocurrencia inválida '{value}' para '{parameterName}'."));
        return null;
    }

    private static void ValidateParameterCount(
        IReadOnlyCollection<IntegrationContractArtifactParameterDto> parameters,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        if (parameters.Count == 0 && issues.Count == 0)
        {
            issues.Add(Issue("NO_IMPORTABLE_PARAMETERS", "No se encontraron parámetros de entrada importables."));
        }

        if (parameters.Count > IntegrationContractArtifactLimits.MaxParameters)
        {
            issues.Add(Issue(
                "PARAMETER_LIMIT_EXCEEDED",
                $"El contrato excede el límite de {IntegrationContractArtifactLimits.MaxParameters} parámetros."));
        }

        var duplicate = parameters
            .GroupBy(parameter => parameter.ParameterPath, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            issues.Add(Issue("DUPLICATE_PARAMETER", $"El parámetro '{duplicate.Key}' aparece más de una vez."));
        }
    }

    private static void ValidateContractMetadata(
        string? operationName,
        string? targetNamespace,
        string? soapAction,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        if (operationName is not null && operationName.Length > 200)
        {
            issues.Add(Issue("OPERATION_NAME_TOO_LONG", "OperationName excede 200 caracteres."));
        }
        if (targetNamespace is not null && targetNamespace.Length > 500)
        {
            issues.Add(Issue("TARGET_NAMESPACE_TOO_LONG", "TargetNamespace excede 500 caracteres."));
        }
        if (soapAction is not null && soapAction.Length > 1000)
        {
            issues.Add(Issue("SOAP_ACTION_TOO_LONG", "SoapAction excede 1000 caracteres."));
        }
    }

    private static IntegrationContractArtifactKindEnum DetectKind(XDocument document)
        => document.Root?.Name == Wsdl + "definitions"
            ? IntegrationContractArtifactKindEnum.Wsdl11
            : document.Root?.Name == Xsd + "schema"
                ? IntegrationContractArtifactKindEnum.XmlSchema
                : IntegrationContractArtifactKindEnum.Unknown;

    private static string? RequiredAttribute(
        XElement element,
        XName attributeName,
        string issueCode,
        ICollection<IntegrationContractArtifactIssueDto> issues)
    {
        var value = (string?)element.Attribute(attributeName);
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(Issue(issueCode, $"Falta el atributo requerido '{attributeName}'."));
            return null;
        }

        return value.Trim();
    }

    private static string QNameLocalName(string qname)
    {
        var trimmed = qname.Trim();
        var separator = trimmed.IndexOf(':');
        return separator < 0 ? trimmed : trimmed[(separator + 1)..];
    }

    private static string ResolveQNameNamespace(XElement context, string qname)
    {
        var trimmed = qname.Trim();
        var separator = trimmed.IndexOf(':');
        if (separator < 0)
        {
            return context.GetDefaultNamespace().NamespaceName;
        }

        var prefix = trimmed[..separator];
        return context.GetNamespaceOfPrefix(prefix)?.NamespaceName ?? string.Empty;
    }

    private static string MapDataType(string xsdType)
        => xsdType switch
        {
            "boolean" => "bool",
            "byte" or "short" or "int" or "long" or "integer" or "nonNegativeInteger" or "positiveInteger"
                or "unsignedByte" or "unsignedShort" or "unsignedInt" or "unsignedLong" => "integer",
            "decimal" or "float" or "double" => "decimal",
            "date" => "date",
            "dateTime" => "datetime",
            "time" => "time",
            "base64Binary" or "hexBinary" => "binary",
            _ => "string"
        };

    private static string NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "unnamed";
        }

        var normalizedSeparators = fileName.Trim().Replace('\\', '/');
        return Path.GetFileName(normalizedSeparators);
    }

    private static string SanitizeXmlError(XmlException ex)
        => $"XML rechazado de forma segura (línea {ex.LineNumber}, posición {ex.LinePosition}). DTD/entidades externas no están permitidas y el XML debe ser bien formado.";

    private static IntegrationContractArtifactIssueDto Issue(string code, string message) => new(code, message);

    private static IntegrationContractArtifactAnalysisDto Result(
        string fileName,
        IntegrationContractArtifactKindEnum kind,
        long sizeBytes,
        string hash,
        string? operationName,
        string? targetNamespace,
        string? soapAction,
        IReadOnlyCollection<IntegrationContractArtifactIssueDto> issues,
        IReadOnlyCollection<IntegrationContractArtifactParameterDto> parameters,
        IReadOnlyCollection<string>? availableOperations = null)
        => new(
            fileName,
            kind,
            sizeBytes,
            hash,
            operationName,
            targetNamespace,
            soapAction,
            issues.Count == 0,
            issues.ToList(),
            parameters.ToList())
        {
            AvailableOperations = availableOperations?.ToList() ?? []
        };
}
