using System.IO;
using System.Xml;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// F5.3: offline integrity check for the F5.2 diagnostic SOAP preview.
/// Not an XSD validator or CFA homologation approval; never authorizes dispatch.
/// Exposes names and counts only, not PII or monetary amounts.
/// </summary>
public static class AchColProcTransaccionesDryRunContractCheck
{
    private const string RequiredBlockingCode = "ACHCOL_SOAP_CONTRACT_NOT_HOMOLOGATED_F52";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Action = "http://tempuri.org/";
    private static readonly HashSet<string> AllowedCandidates = new(StringComparer.OrdinalIgnoreCase)
    {
        "TIPTRAN", "MONTO", "NCTARECEP", "NRECEP", "IDRECEP"
    };

    /// <summary>
    /// Check an existing in-memory preview without sending, saving or logging its values.
    /// The caller is responsible for invoking F5.2's full database/context validations.
    /// </summary>
    public static AchColProcTransaccionesContractReadiness Inspect(
        AchColProcTransaccionesDryRunPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (preview.CanTransmit || preview.BlockingCode != RequiredBlockingCode
            || preview.DispatchQueueId == Guid.Empty || preview.EntryDetailId <= 0
            || preview.MappingSetId == Guid.Empty || preview.MappingVersion <= 0)
            throw new InvalidOperationException("ACHCOL_F53_PREVIEW_INVALID: falta contexto diagnostico bloqueado.");

        var observed = preview.ObservedCandidateParameters
            ?? throw new InvalidOperationException("ACHCOL_F53_PREVIEW_INVALID: parametros ausentes.");
        if (observed.Count == 0 || !observed.ContainsKey("TIPTRAN") || !observed.ContainsKey("MONTO")
            || observed.Any(x => !AllowedCandidates.Contains(x.Key) || string.IsNullOrWhiteSpace(x.Value)))
            throw new InvalidOperationException("ACHCOL_F53_UNAPPROVED_FIELD: la proyeccion contiene parametros no autorizados para DryRun.");

        var pending = preview.PendingHomologationParameters
            ?? throw new InvalidOperationException("ACHCOL_F53_PREVIEW_INVALID: faltan pendientes de homologacion.");
        var missing = preview.MissingRequiredParameters
            ?? throw new InvalidOperationException("ACHCOL_F53_PREVIEW_INVALID: faltan parametros requeridos.");
        if (pending.Count == 0 || pending.Any(x => !AchColProcTransaccionesFieldSources.Fields.ContainsKey(x))
            || missing.Any(x => !AchColProcTransaccionesFieldSources.Fields.ContainsKey(x))
            || pending.Distinct(StringComparer.OrdinalIgnoreCase).Count() != pending.Count
            || missing.Distinct(StringComparer.OrdinalIgnoreCase).Count() != missing.Count)
            throw new InvalidOperationException("ACHCOL_F53_HOMOLOGATION_INVENTORY_INVALID: lista incompleta o inconsistente.");

        XDocument xml;
        try
        {
            // Prevent DTD/entity expansion and any external resource fetch.
            using var input = new StringReader(preview.SoapPreviewXml);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 65536
            });
            xml = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException)
        {
            throw new InvalidOperationException("ACHCOL_F53_SOAP_PREVIEW_INVALID: XML no seguro o mal formado.");
        }

        var envelope = xml.Root;
        var body = envelope?.Element(Soap + "Body");
        var operations = body?.Elements().ToArray() ?? Array.Empty<XElement>();
        var operation = operations.Length == 1 ? operations[0] : null;
        if (envelope?.Name != Soap + "Envelope"
            || envelope.Attributes().Any(x => !x.IsNamespaceDeclaration)
            || envelope.Elements().Any(x => x.Name != Soap + "Body")
            || body is null || body.HasAttributes || operations.Length != 1
            || ContainsUnexpectedContent(body) || ContainsUnexpectedContent(operation)
            || operation?.Name != Action + "Proc_Transacciones"
            || operation.HasAttributes
            || operation.Elements().Any(x => x.HasElements || x.HasAttributes))
            throw new InvalidOperationException("ACHCOL_F53_SOAP_PREVIEW_INVALID: estructura SOAP inesperada.");

        var nodes = operation.Elements().ToArray();
        if (nodes.Length != observed.Count
            || nodes.Select(x => x.Name.LocalName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != nodes.Length
            || nodes.Any(x => x.Name.Namespace != Action
                || !observed.TryGetValue(x.Name.LocalName, out var value)
                || !string.Equals(value, x.Value, StringComparison.Ordinal)))
            throw new InvalidOperationException("ACHCOL_F53_SOAP_PREVIEW_MISMATCH: XML y valores observados no coinciden.");

        // No values (account numbers, identity, amount, names) in this result.
        return new AchColProcTransaccionesContractReadiness(
            preview.DispatchQueueId,
            preview.MappingSetId,
            observed.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            pending.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            missing.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            "ACHCOL_F53_EXTERNAL_CONTRACT_NOT_HOMOLOGATED");
    }

    // Preserve the minimal SOAP shape: values are allowed only inside the five
    // observed parameters, never as stray text, CDATA or processing instructions.
    private static bool ContainsUnexpectedContent(XElement? element) =>
        element is not null && element.Nodes().Any(node => node is not XElement
            && (node is not XText text || !string.IsNullOrWhiteSpace(text.Value)));
}

/// <summary>Redacted offline readiness evidence; deliberately never a transport contract.</summary>
public sealed record AchColProcTransaccionesContractReadiness(
    Guid DispatchQueueId,
    Guid ReferenceMappingSetId,
    IReadOnlyList<string> ObservedParameterNames,
    IReadOnlyList<string> PendingHomologationParameterNames,
    IReadOnlyList<string> MissingRequiredParameterNames,
    string BlockingCode)
{
    public bool IsSoapPreviewStructurallyValid => true;
    public bool HasExternalXsdEvidence => false;
    public bool HasApprovedAchColMapping => false;
    public bool CanTransmit => false;
}
