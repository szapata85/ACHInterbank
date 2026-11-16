using System.Text.Json;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

// Resolve the raw parent of an incoming CENIT return. State events refer to the
// original payment, so matching just (payment, reason) is not an identity.
internal static class CenitParentReturnEvidenceResolver
{
    internal const string LinkIdProperty = "incomingReturnLinkId";

    internal sealed record Result(IncomingNachaTransactionLink? Artifact, string? Code, string? Message)
    {
        internal bool IsResolved => Artifact is not null;
    }

    internal static async Task<Result> ResolveAsync(
        AchDbContext context,
        AchTransactionStateEvent parentEvent,
        AchTransaction original,
        CancellationToken ct)
    {
        if (parentEvent.AchTransactionId != original.Id || string.IsNullOrWhiteSpace(parentEvent.ReasonCode))
            return Failure("CENIT_ROR_SOURCE_RETURN_CORRELATION_INVALID", "El evento no corresponde a la transaccion original.");

        var (pinned, pinError) = ReadPinnedLinkId(parentEvent.PayloadJson);
        if (pinError)
            return Failure("CENIT_ROR_SOURCE_RETURN_LINK_INVALID", "El evento padre contiene un identificador de enlace NACHA invalido.");

        var links = await context.IncomingNachaTransactionLinks.AsNoTracking()
            .Include(x => x.EntryDetail).ThenInclude(x => x!.BatchHeader)
            .Include(x => x.AddendaRecord)
            .Where(x => x.AchTransactionId == original.Id && x.IsFinal && x.AddendaRecord != null)
            .ToListAsync(ct);

        var candidates = links.Where(x =>
            x.EntryDetail is not null && x.AddendaRecord is not null
            && x.EntryDetailId == x.EntryDetail.EntryDetailID
            && x.AddendaRecordId == x.AddendaRecord.AddendaID
            && x.AddendaRecord.EntryDetailId == x.EntryDetail.EntryDetailID
            && string.Equals(x.EntryDetail.NachaID, x.AddendaRecord.NachaID, StringComparison.Ordinal)
            && string.Equals(x.AddendaRecord.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.Ordinal)
            && string.Equals(x.AddendaRecord.ReturnReasonCode?.Trim(), parentEvent.ReasonCode.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.AddendaRecord.OriginalTraceNumber?.Trim(), original.TraceNumber?.Trim(), StringComparison.Ordinal)
            && x.EntryDetail.Amount == original.Amount
            && !string.IsNullOrWhiteSpace(x.EntryDetail.SequenceNumber))
            .ToList();

        if (pinned.HasValue)
        {
            var matched = candidates.SingleOrDefault(x => x.Id == pinned.Value);
            return matched is null
                ? Failure("CENIT_ROR_SOURCE_RETURN_LINK_MISMATCH", "El enlace raw registrado en el evento padre no coincide con la devolucion CENIT.")
                : new(matched, null, null);
        }

        // Old events have no immutable link identity. Never choose the newest
        // return when more than one candidate shares payment and reason.
        if (candidates.Count > 1)
            return Failure("CENIT_ROR_SOURCE_RETURN_AMBIGUOUS", "Hay varias devoluciones raw compatibles con el evento sin enlace explicito.");
        return candidates.Count == 1
            ? new(candidates[0], null, null)
            : Failure("CENIT_ROR_SOURCE_RETURN_RAW_NOT_FOUND", "No hay evidencia raw final y coherente para la devolucion padre.");
    }

    private static (Guid? Id, bool Invalid) ReadPinnedLinkId(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return (null, false);
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(LinkIdProperty, out var element))
                return (null, false);
            if (element.ValueKind != JsonValueKind.String
                || !Guid.TryParse(element.GetString(), out var id)
                || id == Guid.Empty)
                return (null, true);
            return (id, false);
        }
        catch (JsonException)
        {
            // Historical free-form audit payloads have no reliable pin.
            return (null, false);
        }
    }

    private static Result Failure(string code, string message) => new(null, code, message);
}
