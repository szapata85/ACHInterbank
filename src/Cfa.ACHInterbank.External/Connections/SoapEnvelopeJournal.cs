using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections;

/// <summary>
/// Optional, best-effort diagnostic journal for Core Banking SOAP calls.
/// Records the exact XML strings supplied to and returned by the HTTP clients:
/// no redaction, normalization, XML reserialization, size truncation or substitution.
/// This is not a durable financial audit ledger. Restrict access to its output.
/// </summary>
public sealed class SoapEnvelopeJournal
{
    private const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string Soap12Namespace = "http://www.w3.org/2003/05/soap-envelope";

    private readonly ILogger<SoapEnvelopeJournal> _logger;
    private readonly IOptionsMonitor<SoapEnvelopeLoggingOptions> _options;

    public SoapEnvelopeJournal(
        ILogger<SoapEnvelopeJournal> logger,
        IOptionsMonitor<SoapEnvelopeLoggingOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public bool Enabled => _options.CurrentValue.Enabled && _logger.IsEnabled(LogLevel.Information);

    public void Request(string operation, string correlationId, string envelope)
    {
        try
        {
            Write("REQUEST", operation, correlationId, null, null, envelope, false);
        }
        catch (Exception)
        {
            // A diagnostic write must not interfere with the bank request.
        }
    }

    public void Response(string operation, string correlationId, int httpStatus, double elapsedMs, string envelope)
    {
        try
        {
            if (!Enabled)
            {
                return;
            }

            Write("RESPONSE", operation, correlationId, httpStatus, elapsedMs, envelope, HasSoapFault(envelope));
        }
        catch (Exception)
        {
            // A diagnostic write must not alter the response or cause retries.
        }
    }

    public void Failure(string operation, string correlationId, string kind, double elapsedMs)
    {
        try
        {
            if (!Enabled)
            {
                return;
            }

            // A missing HTTP response must not be presented as a SOAP envelope.
            _logger.LogInformation(
                "SOAP_ENVELOPE direction=NO_RESPONSE operation={SoapOperation} correlationId={SoapCorrelationId} " +
                "reason={SoapFailureKind} elapsedMs={SoapElapsedMs:F0}",
                operation, correlationId, kind, elapsedMs);
        }
        catch (Exception)
        {
            // Preserve the original transport error.
        }
    }

    private void Write(string direction, string operation, string correlationId,
        int? httpStatus, double? elapsedMs, string envelope, bool hasFault)
    {
        if (!Enabled)
        {
            return;
        }

        // The envelope is logged verbatim; SOAP message content is not parsed for rewriting.
        // XML may contain line breaks, attributes, comments and sensitive values.
        _logger.LogInformation(
            "SOAP_ENVELOPE direction={SoapDirection} operation={SoapOperation} " +
            "correlationId={SoapCorrelationId} httpStatus={SoapHttpStatus} " +
            "elapsedMs={SoapElapsedMs} soapFault={SoapFault} payloadChars={SoapPayloadChars} envelope={SoapEnvelope}",
            direction, operation, correlationId, httpStatus?.ToString() ?? "n/a",
            elapsedMs?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "n/a",
            hasFault, envelope?.Length ?? 0, envelope);
    }

    private static bool HasSoapFault(string? envelope)
    {
        // Fast path: avoid parsing most successful responses. The read-only XML scan
        // only detects a Fault; it never changes or suppresses the original content.
        if (string.IsNullOrEmpty(envelope) ||
            envelope.IndexOf("Fault", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        try
        {
            using var input = new StringReader(envelope);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 0
            });

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element &&
                    reader.LocalName.Equals("Fault", StringComparison.Ordinal) &&
                    (reader.NamespaceURI == Soap11Namespace || reader.NamespaceURI == Soap12Namespace))
                {
                    return true;
                }
            }
        }
        catch (XmlException)
        {
            // Malformed XML is still written verbatim to the journal.
        }
        catch (Exception)
        {
            // Fault classification must never affect SOAP processing.
        }

        return false;
    }
}

public sealed class SoapEnvelopeLoggingOptions
{
    public const string SectionName = "SoapEnvelopeLogging";
    public bool Enabled { get; set; }
}
