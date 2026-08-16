using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
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

    public async Task<IncomingNachaLinkingResult> LinkAsync(
        EntryDetail entry,
        AddendaRecord? addenda,
        IncomingNachaLinkingContext context,
        CancellationToken ct = default)
    {
        var trace = (entry.SequenceNumber ?? string.Empty).Trim();
        var originalTraceRef = ((context.FunctionalClass == IncomingNachaFunctionalClass.DevolucionDevolucion
            ? addenda?.NewTraceNumber
            : addenda?.OriginalTraceNumber) ?? string.Empty).Trim();
        var recipientIdentifier = (entry.RecipIdNumber ?? string.Empty).Trim();

        if (context.FunctionalClass == IncomingNachaFunctionalClass.DevolucionDevolucion
            && !string.IsNullOrWhiteSpace(originalTraceRef))
        {
            var parentReturns = await _context.Set<AchReturnGenerated>()
                .AsNoTracking()
                .Where(x => x.NewSequenceNumber == originalTraceRef)
                .Select(x => new { x.OriginalTransactionId, x.ReturnCycleId })
                .ToListAsync(ct);
            if (!string.IsNullOrWhiteSpace(context.ResolvedAchCycleId))
            {
                var sameCycle = parentReturns.Where(x => x.ReturnCycleId == context.ResolvedAchCycleId).ToList();
                if (sameCycle.Count > 0) parentReturns = sameCycle;
            }
            if (parentReturns.Count == 1)
                return Build(IncomingNachaLinkType.ExactOriginalTraceRef, parentReturns[0].OriginalTransactionId, true, 1m, false, false,
                    "ExactParentReturnTraceRef", [parentReturns[0].OriginalTransactionId], trace, originalTraceRef, recipientIdentifier);
            if (parentReturns.Count > 1)
                return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.3m, true, false,
                    "AmbiguousParentReturnTraceRef", parentReturns.Select(x => x.OriginalTransactionId).ToArray(), trace, originalTraceRef, recipientIdentifier);
            return Build(IncomingNachaLinkType.NotFound, null, false, 0m, false, true,
                "ParentReturnNotFound", [], trace, originalTraceRef, recipientIdentifier);
        }

        // 1) OriginalTraceRef exacto para devoluciones
        if (!string.IsNullOrWhiteSpace(originalTraceRef))
        {
            var exactQuery = _context.AchTransactions.AsNoTracking()
                .Where(x => x.TraceNumber == originalTraceRef || x.OriginalTraceRef == originalTraceRef);
            var (candidates, narrowedByCycle) = await ResolveExactCandidatesAsync(exactQuery, context, ct);
            if (candidates.Count == 1)
            {
                var criterion = narrowedByCycle ? "ExactOriginalTraceRef+ResolvedCycle" : "ExactOriginalTraceRef";
                return Build(IncomingNachaLinkType.ExactOriginalTraceRef, candidates[0], true, 1.00m, false, false, criterion, candidates, trace, originalTraceRef, recipientIdentifier);
            }

            if (candidates.Count > 1)
            {
                return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.30m, true, false, "AmbiguousOriginalTraceRef", candidates, trace, originalTraceRef, recipientIdentifier);
            }
        }

        // 2) TraceNumber exacto y único
        if (!string.IsNullOrWhiteSpace(trace))
        {
            var exactQuery = _context.AchTransactions.AsNoTracking()
                .Where(x => x.TraceNumber == trace);
            var (candidates, narrowedByCycle) = await ResolveExactCandidatesAsync(exactQuery, context, ct);
            if (candidates.Count == 1)
            {
                var criterion = narrowedByCycle ? "ExactTrace15+ResolvedCycle" : "ExactTrace15";
                return Build(IncomingNachaLinkType.ExactTrace15, candidates[0], true, 0.98m, false, false, criterion, candidates, trace, originalTraceRef, recipientIdentifier);
            }

            if (candidates.Count > 1)
            {
                return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.25m, true, false, "AmbiguousTrace15", candidates, trace, originalTraceRef, recipientIdentifier);
            }
        }

        // 3) Composite business key exacta. RecipIdNumber es una identificación del receptor;
        // no se interpreta como TransactionExternalId porque ese contrato no está demostrado.
        // monto + cuenta destino + identificación receptor + transactionCode
        // + dfi receptor + fecha operativa + ciclo/cámara cuando estén disponibles.
        var destinationAccount = (entry.AccountNumber ?? string.Empty).Trim();
        var recipientId = (entry.RecipIdNumber ?? string.Empty).Trim();
        var transactionCode = (entry.TransactionCode ?? string.Empty).Trim();
        var receivingDfiComposite = $"{(entry.ReceivingParticipantEntityCode ?? string.Empty).Trim()}{(entry.CheckDigit ?? string.Empty).Trim()}".Trim();
        var hasOperationalDate = context.OperationalDate.HasValue;
        var hasCycle = !string.IsNullOrWhiteSpace(context.ResolvedAchCycleId);

        var compositeQuery = _context.AchTransactions.AsNoTracking()
            .Include(x => x.AchCycle)
            .Where(x => x.Amount == entry.Amount.GetValueOrDefault()
                        && x.DestinationAccountNumber == destinationAccount
                        && x.TransactionCode == transactionCode);

        var expectedType = ResolveExpectedTransactionType(context.FunctionalClass);
        if (expectedType.HasValue)
        {
            compositeQuery = compositeQuery.Where(x => x.Type == expectedType.Value);
        }

        if (context.FunctionalClass == IncomingNachaFunctionalClass.Prenotificacion)
        {
            compositeQuery = compositeQuery.Where(x => x.IsPrenotification);
        }

        if (!string.IsNullOrWhiteSpace(recipientId))
        {
            compositeQuery = compositeQuery.Where(x => x.RecipientIdNumber == recipientId);
        }

        if (!string.IsNullOrWhiteSpace(receivingDfiComposite))
        {
            compositeQuery = compositeQuery.Where(x => x.ReceivingDFI == receivingDfiComposite || x.ReceivingDFI.StartsWith((entry.ReceivingParticipantEntityCode ?? string.Empty).Trim()));
        }

        var discretionaryData = (entry.DiscreData ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(discretionaryData))
        {
            compositeQuery = compositeQuery.Where(x => x.DiscretionaryData == discretionaryData);
        }

        if (hasOperationalDate)
        {
            var opDate = context.OperationalDate!.Value.Date;
            compositeQuery = compositeQuery.Where(x => x.EffectiveEntryDate.Date == opDate);
        }

        if (hasCycle)
        {
            compositeQuery = compositeQuery.Where(x => x.AchCycleId == context.ResolvedAchCycleId);
        }
        else if (context.ResolvedClearingHouseId.HasValue && hasOperationalDate)
        {
            var clearingHouseId = context.ResolvedClearingHouseId.Value;
            var opDate = context.OperationalDate!.Value.Date;
            compositeQuery = compositeQuery.Where(x => x.AchCycle.ClearingHouseId == clearingHouseId && x.AchCycle.ProcessingDate.Date == opDate);
        }

        var compositeCandidates = await compositeQuery
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (compositeCandidates.Count == 1)
        {
            return Build(IncomingNachaLinkType.ExactCompositeBusinessKey, compositeCandidates[0], true, 0.90m, false, false, "ExactCompositeBusinessKeyV2", compositeCandidates, trace, originalTraceRef, recipientIdentifier);
        }

        if (compositeCandidates.Count > 1)
        {
            return Build(IncomingNachaLinkType.Ambiguous, null, false, 0.15m, true, false, "AmbiguousCompositeBusinessKeyV2", compositeCandidates, trace, originalTraceRef, recipientIdentifier);
        }

        return Build(IncomingNachaLinkType.NotFound, null, false, 0.0m, false, true, "NotFound", [], trace, originalTraceRef, recipientIdentifier);
    }

    private static async Task<(IReadOnlyList<int> Candidates, bool NarrowedByCycle)> ResolveExactCandidatesAsync(
        IQueryable<AchTransaction> exactQuery,
        IncomingNachaLinkingContext context,
        CancellationToken ct)
    {
        var candidates = await exactQuery.Select(x => x.Id).ToListAsync(ct);
        if (candidates.Count <= 1 || string.IsNullOrWhiteSpace(context.ResolvedAchCycleId))
        {
            return (candidates, false);
        }

        var cycleCandidates = await exactQuery
            .Where(x => x.AchCycleId == context.ResolvedAchCycleId)
            .Select(x => x.Id)
            .ToListAsync(ct);
        return cycleCandidates.Count > 0
            ? (cycleCandidates, true)
            : (candidates, false);
    }

    private static TransactionTypeEnum? ResolveExpectedTransactionType(IncomingNachaFunctionalClass functionalClass)
    {
        return functionalClass switch
        {
            IncomingNachaFunctionalClass.CreditoEntrante => TransactionTypeEnum.Credit,
            IncomingNachaFunctionalClass.DebitoEntrante => TransactionTypeEnum.Debit,
            IncomingNachaFunctionalClass.Prenotificacion => TransactionTypeEnum.Credit,
            IncomingNachaFunctionalClass.Devolucion => null,
            IncomingNachaFunctionalClass.RechazadaOperador => null,
            IncomingNachaFunctionalClass.RetornoEpr => null,
            IncomingNachaFunctionalClass.DevolucionDevolucion => TransactionTypeEnum.Return,
            _ => null
        };
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
        string recipientIdentifier)
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
                recipientIdentifier
            })
        };
    }
}
