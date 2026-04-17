using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaTransactionLinker : IIncomingNachaTransactionLinker
{
    private readonly AchDbContext _context;

    public IncomingNachaTransactionLinker(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IncomingNachaLinkingResult> LinkAsync(EntryDetail entry, AddendaRecord? addenda, CancellationToken ct = default)
    {
        var trace = (entry.SequenceNumber ?? string.Empty).Trim();
        var originalTraceRef = (addenda?.OriginalTraceNumber ?? string.Empty).Trim();
        var externalId = (entry.RecipIdNumber ?? string.Empty).Trim();

        // 1) OriginalTraceRef exacto para devoluciones
        if (!string.IsNullOrWhiteSpace(originalTraceRef))
        {
            var candidates = await _context.AchTransactions.AsNoTracking()
                .Where(x => x.TraceNumber == originalTraceRef || x.OriginalTraceRef == originalTraceRef)
                .Select(x => x.Id)
                .ToListAsync(ct);
            if (candidates.Count == 1)
            {
                return Build(IncomingNachaLinkType.ExactOriginalTraceRef, candidates[0], true, 1.00m, false, false, "ExactOriginalTraceRef", candidates, trace, originalTraceRef, externalId);
            }

            if (candidates.Count > 1)
            {
                return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.30m, true, false, "AmbiguousOriginalTraceRef", candidates, trace, originalTraceRef, externalId);
            }
        }

        // 2) TraceNumber exacto y único
        if (!string.IsNullOrWhiteSpace(trace))
        {
            var candidates = await _context.AchTransactions.AsNoTracking()
                .Where(x => x.TraceNumber == trace)
                .Select(x => x.Id)
                .ToListAsync(ct);
            if (candidates.Count == 1)
            {
                return Build(IncomingNachaLinkType.ExactTrace15, candidates[0], true, 0.98m, false, false, "ExactTrace15", candidates, trace, originalTraceRef, externalId);
            }

            if (candidates.Count > 1)
            {
                return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.25m, true, false, "AmbiguousTrace15", candidates, trace, originalTraceRef, externalId);
            }
        }

        // 3) TransactionExternalId exacto
        if (!string.IsNullOrWhiteSpace(externalId))
        {
            var candidates = await _context.AchTransactions.AsNoTracking()
                .Where(x => x.TransactionExternalId == externalId)
                .Select(x => x.Id)
                .ToListAsync(ct);
            if (candidates.Count == 1)
            {
                return Build(IncomingNachaLinkType.ExactTransactionExternalId, candidates[0], true, 0.95m, false, false, "ExactTransactionExternalId", candidates, trace, originalTraceRef, externalId);
            }

            if (candidates.Count > 1)
            {
                return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.20m, true, false, "AmbiguousExternalId", candidates, trace, originalTraceRef, externalId);
            }
        }

        // 4) Composite business key exacta
        var compositeCandidates = await _context.AchTransactions.AsNoTracking()
            .Where(x => x.Amount == entry.Amount.GetValueOrDefault()
                        && x.DestinationAccountNumber == (entry.AccountNumber ?? string.Empty)
                        && x.RecipientIdNumber == (entry.RecipIdNumber ?? string.Empty))
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (compositeCandidates.Count == 1)
        {
            return Build(IncomingNachaLinkType.ExactCompositeBusinessKey, compositeCandidates[0], true, 0.85m, false, false, "ExactCompositeBusinessKey", compositeCandidates, trace, originalTraceRef, externalId);
        }

        if (compositeCandidates.Count > 1)
        {
            return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.15m, true, false, "AmbiguousCompositeBusinessKey", compositeCandidates, trace, originalTraceRef, externalId);
        }

        return Build(IncomingNachaLinkType.NotFound, null, false, 0.0m, false, true, "NotFound", [], trace, originalTraceRef, externalId);
    }

    private static IncomingNachaLinkingResult Build(
        IncomingNachaLinkType type,
        int? transactionId,
        bool final,
        decimal confidence,
        bool ambiguous,
        bool notFound,
        string criterion,
        IReadOnlyList<int> candidates,
        string trace,
        string originalTrace,
        string externalId)
    {
        return new IncomingNachaLinkingResult
        {
            LinkType = type,
            AchTransactionId = transactionId,
            IsFinal = final,
            ConfidenceScore = confidence,
            IsAmbiguous = ambiguous,
            IsNotFound = notFound,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                criterion,
                candidates,
                trace,
                originalTrace,
                externalId
            })
        };
    }
}
