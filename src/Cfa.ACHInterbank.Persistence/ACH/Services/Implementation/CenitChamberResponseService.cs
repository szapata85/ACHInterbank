using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class CenitChamberResponseService(
    AchDbContext context,
    ILogger<CenitChamberResponseService> logger) : ICenitChamberResponseService
{
    private const string CenitCode = "CENIT";

    public async Task<CenitChamberResponseResult> ImportAsync(
        CenitChamberResponseImportCommand command,
        CancellationToken ct = default)
    {
        Validate(command);
        var sourceId = Trim(command.SourceResponseId, 128);
        var sourceFileName = Path.GetFileName(Trim(command.SourceFileName, 180));
        var content = command.Content ?? string.Empty;
        var contentHash = Hash(content);
        var clearingHouseId = await context.ClearingHouses
            .AsNoTracking()
            .Where(x => x.Code == CenitCode)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("CENIT_CLEARING_HOUSE_NOT_CONFIGURED");

        var sameSource = await context.CenitChamberResponses
            .AsNoTracking()
            .Include(x => x.AchFileExport)
            .Where(x => x.ClearingHouseId == clearingHouseId && x.SourceResponseId == sourceId)
            .OrderBy(x => x.ItemSequence)
            .ToListAsync(ct);
        if (sameSource.Count > 0)
        {
            return sameSource.All(x => string.Equals(x.ContentSha256, contentHash, StringComparison.Ordinal))
                ? Map(sameSource[0], true)
                : MapConflict(sameSource[0], "CENIT_RESPONSE_ID_CONFLICT");
        }

        var parsedItems = Parse(command.MessageType, content);
        logger.LogInformation(
            "CENIT_CHAMBER_RESPONSE_RECEIVED SourceResponseId={SourceResponseId} SourceFileName={SourceFileName} ItemCount={ItemCount}",
            sourceId,
            sourceFileName,
            parsedItems.Count);

        var results = new List<CenitChamberResponseResult>(parsedItems.Count);
        for (var index = 0; index < parsedItems.Count; index++)
        {
            results.Add(await ImportItemAsync(
                command,
                parsedItems[index],
                index + 1,
                parsedItems.Count,
                sourceId,
                sourceFileName,
                contentHash,
                clearingHouseId,
                ct));
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            var winner = await context.CenitChamberResponses
                .AsNoTracking()
                .Include(x => x.AchFileExport)
                .Where(x => x.ClearingHouseId == clearingHouseId && x.SourceResponseId == sourceId)
                .OrderBy(x => x.ItemSequence)
                .FirstOrDefaultAsync(CancellationToken.None);
            if (winner is not null) return Map(winner, true);
            throw;
        }

        return results[0];
    }

    private async Task<CenitChamberResponseResult> ImportItemAsync(
        CenitChamberResponseImportCommand command,
        ParsedResponse parsed,
        int itemSequence,
        int itemCount,
        string sourceId,
        string sourceFileName,
        string contentHash,
        int clearingHouseId,
        CancellationToken ct)
    {
        var relatedFileName = NormalizeOptionalFileName(command.RelatedOutboundFileName);
        var relatedReference = NormalizeOptional(command.RelatedReference, 120) ?? parsed.RelatedReference;
        var transactionTrace = NormalizeOptional(command.TransactionTraceNumber, 20) ?? parsed.TransactionTrace;
        var requestedCycleId = NormalizeOptional(command.AchCycleId, 40);
        var now = DateTime.UtcNow;
        var sessionOutput = parsed.Type is CenitChamberResponseType.Reconciliation or CenitChamberResponseType.NoActivity;

        AchFileExport? export = null;
        AchFileExportTransaction? membership = null;
        string? cycleId = null;
        var correlation = CenitChamberCorrelationOutcome.Pending;
        var problemCode = parsed.ProblemCode;

        if (parsed.Type == CenitChamberResponseType.Unknown)
        {
            correlation = CenitChamberCorrelationOutcome.Invalid;
        }
        else if (sessionOutput)
        {
            if (requestedCycleId is null)
            {
                correlation = CenitChamberCorrelationOutcome.NotFound;
                problemCode = "CENIT_SESSION_IDENTIFIER_REQUIRED";
            }
            else
            {
                cycleId = await context.AchCycles.AsNoTracking()
                    .Where(x => x.Id == requestedCycleId && x.ClearingHouseId == clearingHouseId)
                    .Select(x => x.Id)
                    .SingleOrDefaultAsync(ct);
                correlation = cycleId is null
                    ? CenitChamberCorrelationOutcome.NotFound
                    : CenitChamberCorrelationOutcome.Matched;
                if (cycleId is null) problemCode = "CENIT_SESSION_CORRELATION_NOT_FOUND";
            }
        }
        else
        {
            var candidates = context.AchFileExports
                .Include(x => x.Transactions)
                .Where(x => x.ClearingHouseId == clearingHouseId && x.ExportKind == "NACHA");
            var hasFileIdentifier = false;
            if (relatedFileName is not null)
            {
                candidates = candidates.Where(x => x.FileName == relatedFileName);
                hasFileIdentifier = true;
            }
            else if (relatedReference is not null)
            {
                candidates = candidates.Where(x => x.TransmissionReference == relatedReference);
                hasFileIdentifier = true;
            }
            if (requestedCycleId is not null)
            {
                candidates = candidates.Where(x => x.AchCycleId == requestedCycleId);
                hasFileIdentifier = true;
            }

            if (!hasFileIdentifier)
            {
                correlation = CenitChamberCorrelationOutcome.NotFound;
                problemCode = "CENIT_CORRELATION_IDENTIFIER_REQUIRED";
            }
            else
            {
                var matches = await candidates.Take(2).ToListAsync(ct);
                correlation = matches.Count switch
                {
                    0 => CenitChamberCorrelationOutcome.NotFound,
                    1 => CenitChamberCorrelationOutcome.Matched,
                    _ => CenitChamberCorrelationOutcome.Ambiguous
                };
                problemCode = correlation switch
                {
                    CenitChamberCorrelationOutcome.NotFound => "CENIT_CORRELATION_NOT_FOUND",
                    CenitChamberCorrelationOutcome.Ambiguous => "CENIT_CORRELATION_AMBIGUOUS",
                    _ => problemCode
                };
                export = matches.Count == 1 ? matches[0] : null;
                cycleId = export?.AchCycleId;
            }

            if (export is not null && transactionTrace is not null)
            {
                var transactionMatches = export.Transactions
                    .Where(x => string.Equals(x.TraceNumber, transactionTrace, StringComparison.Ordinal))
                    .Take(2)
                    .ToArray();
                if (transactionMatches.Length != 1)
                {
                    correlation = transactionMatches.Length == 0
                        ? CenitChamberCorrelationOutcome.TransactionNotFound
                        : CenitChamberCorrelationOutcome.TransactionAmbiguous;
                    problemCode = transactionMatches.Length == 0
                        ? "CENIT_TRANSACTION_CORRELATION_NOT_FOUND"
                        : "CENIT_TRANSACTION_CORRELATION_AMBIGUOUS";
                }
                else
                {
                    membership = transactionMatches[0];
                }
            }
        }

        var targetState = ToState(parsed.Type);
        var idempotencyKey = Hash(string.Join('|',
            clearingHouseId,
            parsed.Type,
            export?.Id,
            cycleId,
            membership?.AchTransactionId,
            parsed.ReasonCode,
            relatedFileName,
            relatedReference,
            transactionTrace,
            itemSequence,
            contentHash));
        var duplicate = await context.CenitChamberResponses
            .AsNoTracking()
            .Include(x => x.AchFileExport)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
        if (duplicate is not null) return Map(duplicate, true);

        var response = new CenitChamberResponse
        {
            Id = Guid.NewGuid(),
            ClearingHouseId = clearingHouseId,
            AchFileExportId = export?.Id,
            AchCycleId = cycleId,
            AchTransactionId = membership?.AchTransactionId,
            SourceResponseId = sourceId,
            SourceFileName = sourceFileName,
            ResponseType = parsed.Type,
            ResultingState = targetState,
            ReasonCode = NormalizeOptional(parsed.ReasonCode, 60),
            Description = NormalizeOptional(parsed.Description, 500),
            ReceivedAtUtc = NormalizeUtc(command.ReceivedAtUtc),
            CorrelationOutcome = correlation,
            RawTechnicalReference = Trim($"{sourceFileName}#{sourceId}#{itemSequence}", 256),
            ContentSha256 = contentHash,
            IdempotencyKey = idempotencyKey,
            RelatedOutboundFileName = relatedFileName,
            RelatedReference = relatedReference,
            XmlNamespace = NormalizeOptional(parsed.XmlNamespace, 120),
            MessageGroupId = NormalizeOptional(parsed.GroupId, 16),
            MessageStatus = NormalizeOptional(parsed.Status, 35),
            MessageCreatedAtUtc = parsed.CreationDateUtc,
            OriginatingSender = NormalizeOptional(parsed.OriginatingSender, 8),
            TransactionTraceNumber = transactionTrace,
            ProblemCode = problemCode,
            ItemSequence = itemSequence,
            ItemCount = itemCount
        };

        if (correlation == CenitChamberCorrelationOutcome.Matched && sessionOutput)
        {
            response.IsApplied = true;
            response.ProcessedAtUtc = now;
        }
        else if (export is not null && correlation == CenitChamberCorrelationOutcome.Matched)
        {
            if (CanTransition(export.ChamberResponseState, targetState))
            {
                export.ChamberResponseState = targetState;
                export.ChamberResponseUpdatedAtUtc = now;
                response.IsApplied = true;
                response.ProcessedAtUtc = now;
            }
            else if (export.ChamberResponseState == targetState)
            {
                response.ProcessedAtUtc = now;
            }
            else
            {
                response.ResultingState = export.ChamberResponseState;
                response.CorrelationOutcome = CenitChamberCorrelationOutcome.InvalidTransition;
                response.ProblemCode = "CENIT_INVALID_LIFECYCLE_TRANSITION";
            }
        }

        context.CenitChamberResponses.Add(response);
        logger.LogInformation(
            "CENIT_CHAMBER_RESPONSE_CLASSIFIED SourceResponseId={SourceResponseId} ItemSequence={ItemSequence} ResponseType={ResponseType} Correlation={Correlation}",
            sourceId,
            itemSequence,
            response.ResponseType,
            response.CorrelationOutcome);
        return Map(response, false, export?.FileName);
    }

    public async Task<CenitChamberResponseResult?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var response = await context.CenitChamberResponses
            .AsNoTracking()
            .Include(x => x.AchFileExport)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return response is null ? null : Map(response, false);
    }

    public async Task<CenitChamberResponsePage> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var clearingHouseId = await context.ClearingHouses.AsNoTracking()
            .Where(x => x.Code == CenitCode)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(ct);
        if (!clearingHouseId.HasValue) return new CenitChamberResponsePage([], 0, page, pageSize);

        var query = context.CenitChamberResponses.AsNoTracking().Include(x => x.AchFileExport);
        var responseTotal = await query.CountAsync(ct);
        var pendingQuery = context.AchFileExports.AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId.Value
                        && x.ExportKind == "NACHA"
                        && x.ChamberResponseState == CenitChamberResponseState.Pending);
        var pendingTotal = await pendingQuery.CountAsync(ct);
        var window = Math.Min(page * pageSize, 10_000);
        var entities = await query.OrderByDescending(x => x.ReceivedAtUtc)
            .ThenByDescending(x => x.Id)
            .Take(window)
            .ToListAsync(ct);
        var pending = await pendingQuery.OrderByDescending(x => x.GeneratedAtUtc)
            .Take(window)
            .Select(x => new CenitChamberResponseResult(
                Guid.Empty, false, string.Empty, string.Empty, string.Empty,
                CenitChamberResponseType.Unknown, CenitChamberResponseState.Pending,
                CenitChamberCorrelationOutcome.Pending, x.Id, x.FileName, x.AchCycleId,
                null, null, null, null, null, x.TransmissionReference,
                null, null, null, "Pendiente de respuesta de cámara CENIT.",
                x.GeneratedAtUtc, null, false, null, 1, 1))
            .ToListAsync(ct);
        var items = entities.Select(x => Map(x, false))
            .Concat(pending)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();
        return new CenitChamberResponsePage(items, responseTotal + pendingTotal, page, pageSize);
    }

    private static IReadOnlyList<ParsedResponse> Parse(string messageType, string content)
    {
        var normalized = messageType.Trim().Replace("_", string.Empty).Replace("-", string.Empty).ToUpperInvariant();
        if (normalized is "NOACTIVITY" or "SINACTIVIDAD")
        {
            return [string.IsNullOrWhiteSpace(content)
                ? new(CenitChamberResponseType.NoActivity, null, "Archivo de no actividad CENIT.", null, null, null, null, null, null, null, null)
                : ParsedResponse.Invalid("CENIT_NO_ACTIVITY_NOT_EMPTY")];
        }
        if (normalized is "RECONCILIATION" or "RECONCILIACION")
        {
            return [!string.IsNullOrWhiteSpace(content)
                ? new(CenitChamberResponseType.Reconciliation, null, "Archivo de reconciliación CENIT.", null, null, null, null, null, null, null, null)
                : ParsedResponse.Invalid("CENIT_RECONCILIATION_EMPTY")];
        }
        if (normalized != "XML") return [ParsedResponse.Invalid("CENIT_RESPONSE_NOT_RECOGNIZED")];

        try
        {
            using var stringReader = new StringReader(content);
            using var reader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1_000_000
            });
            var document = XDocument.Load(reader, LoadOptions.None);
            var root = document.Root;
            if (root is null) return [ParsedResponse.Invalid("CENIT_RESPONSE_XML_INVALID")];

            string? DescendantValue(XContainer container, string name)
                => container.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value.Trim() is { Length: > 0 } value
                    ? value
                    : null;
            var groupHeader = root.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("GroupHeader", StringComparison.OrdinalIgnoreCase));
            var additionalRefs = root.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("AdditionalRefs", StringComparison.OrdinalIgnoreCase));
            var xmlNamespace = root.Name.NamespaceName;
            var groupId = groupHeader is null ? null : DescendantValue(groupHeader, "GroupId");
            var status = groupHeader is null ? null : DescendantValue(groupHeader, "Status");
            var creationDate = ParseXmlDate(groupHeader is null ? null : DescendantValue(groupHeader, "CreationDate"));
            var relatedRef = additionalRefs is null ? null : DescendantValue(additionalRefs, "RelatedRef");
            var originator = additionalRefs is null ? null : DescendantValue(additionalRefs, "OrigSender");

            if (root.Name.LocalName.Equals("FileAck", StringComparison.OrdinalIgnoreCase))
            {
                return [new(CenitChamberResponseType.Ack, status, status, relatedRef, null, null,
                    xmlNamespace, groupId, status, creationDate, originator)];
            }
            if (!root.Name.LocalName.Equals("FileNack", StringComparison.OrdinalIgnoreCase))
                return [ParsedResponse.Invalid("CENIT_RESPONSE_NOT_RECOGNIZED")];

            var errors = root.Elements()
                .Where(x => x.Name.LocalName.Equals("FileErrorHandling", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (errors.Length == 0) return [ParsedResponse.Invalid("CENIT_FILE_NACK_ERRORS_REQUIRED")];
            return errors.Select(error =>
            {
                var trace = DescendantValue(error, "TraceNo");
                var batch = DescendantValue(error, "BatchNo");
                var itemStatus = DescendantValue(error, "Status") ?? status;
                var type = trace is not null || batch is not null
                    ? CenitChamberResponseType.OperatorRejected
                    : CenitChamberResponseType.Nack;
                return new ParsedResponse(type,
                    DescendantValue(error, "ErrorCode") ?? itemStatus,
                    DescendantValue(error, "AdditionalDesc") ?? itemStatus,
                    relatedRef,
                    trace,
                    null,
                    xmlNamespace,
                    groupId,
                    itemStatus,
                    creationDate,
                    originator);
            }).ToArray();
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            return [ParsedResponse.Invalid("CENIT_RESPONSE_XML_INVALID")];
        }
    }

    private static DateTime? ParseXmlDate(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed.UtcDateTime
            : null;

    private static bool CanTransition(CenitChamberResponseState current, CenitChamberResponseState target)
        => current == CenitChamberResponseState.Pending;

    private static CenitChamberResponseState ToState(CenitChamberResponseType type) => type switch
    {
        CenitChamberResponseType.Ack => CenitChamberResponseState.Accepted,
        CenitChamberResponseType.Nack => CenitChamberResponseState.Rejected,
        CenitChamberResponseType.OperatorRejected => CenitChamberResponseState.OperatorRejected,
        CenitChamberResponseType.Reconciliation => CenitChamberResponseState.Reconciliation,
        CenitChamberResponseType.NoActivity => CenitChamberResponseState.NoActivity,
        _ => CenitChamberResponseState.Pending
    };

    private static CenitChamberResponseResult Map(CenitChamberResponse x, bool duplicate, string? fileName = null)
        => new(x.Id, duplicate, x.SourceResponseId, x.SourceFileName, x.RawTechnicalReference,
            x.ResponseType, x.ResultingState, x.CorrelationOutcome, x.AchFileExportId,
            fileName ?? x.AchFileExport?.FileName ?? x.RelatedOutboundFileName, x.AchCycleId,
            x.XmlNamespace, x.MessageGroupId, x.MessageStatus, x.MessageCreatedAtUtc,
            x.OriginatingSender, x.RelatedReference, x.AchTransactionId,
            x.TransactionTraceNumber, x.ReasonCode, x.Description, x.ReceivedAtUtc,
            x.ProcessedAtUtc, x.IsApplied, x.ProblemCode, x.ItemSequence, x.ItemCount);

    private static CenitChamberResponseResult MapConflict(CenitChamberResponse x, string code)
        => Map(x, false) with { ProblemCode = code, IsApplied = false };

    private static void Validate(CenitChamberResponseImportCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourceResponseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.SourceFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.MessageType);
        if (command.ReceivedAtUtc == default) throw new ArgumentException("La respuesta requiere fecha de recepción.", nameof(command));
        if (!string.Equals(Path.GetFileName(command.SourceFileName.Trim()), command.SourceFileName.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("El nombre del archivo de respuesta no es válido.", nameof(command));
    }

    private static string? NormalizeOptionalFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (!string.Equals(Path.GetFileName(normalized), normalized, StringComparison.Ordinal))
            throw new ArgumentException("El nombre del archivo relacionado no es válido.", nameof(value));
        return Trim(normalized, 180);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
        => string.IsNullOrWhiteSpace(value) ? null : Trim(value, maxLength);

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Trim(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, maxLength)];
    }

    private sealed record ParsedResponse(
        CenitChamberResponseType Type,
        string? ReasonCode,
        string? Description,
        string? RelatedReference,
        string? TransactionTrace,
        string? ProblemCode,
        string? XmlNamespace,
        string? GroupId,
        string? Status,
        DateTime? CreationDateUtc,
        string? OriginatingSender)
    {
        public static ParsedResponse Invalid(string problemCode)
            => new(CenitChamberResponseType.Unknown, null, null, null, null, problemCode,
                null, null, null, null, null);
    }
}
