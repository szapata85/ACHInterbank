using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaDispatchPlanner : IIncomingNachaDispatchPlanner
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaDispatchEligibilityPolicy _eligibility;
    private readonly IPaymentRailContextService? _paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService;
    private readonly ILogger<IncomingNachaDispatchPlanner> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration? _configuration;

    public IncomingNachaDispatchPlanner(
        AchDbContext context,
        IIncomingNachaDispatchEligibilityPolicy eligibility,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        ILogger<IncomingNachaDispatchPlanner>? logger = null,
        TimeProvider? timeProvider = null,
        IConfiguration? configuration = null)
    {
        _context = context;
        _eligibility = eligibility;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
        _shadowCompareService = shadowCompareService;
        _logger = logger ?? NullLogger<IncomingNachaDispatchPlanner>.Instance;
        _timeProvider = timeProvider ?? OperationalTimeProvider.SystemBogota;
        _configuration = configuration;
    }

    public async Task<int> PlanForIngestionAsync(Guid ingestionId, string plannedBy, CancellationToken ct = default)
    {
        var ingestion = await _context.IncomingNachaFileIngestions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ingestionId, ct)
            ?? throw new InvalidOperationException($"No existe ingesta {ingestionId}.");

        // F3.3: ACHCOL file-based work is planned from the parsed entry itself.
        // No internal cycle or artificial AchTransaction / TransactionLink is required.
        // Its rows stay Blocked until the dedicated eligibility/dispatch stages (F4/F5).
        if (string.Equals(ingestion.ResolutionMode, "AchColFileBased", StringComparison.Ordinal))
        {
            return IsFileBasedIngestion(ingestion)
                && _configuration?.GetValue<bool>("AchCol:AllowCycleIndependentInboundDispatch") == true
                ? await PlanFileBasedAsync(ingestion, ct)
                : 0;
        }

        var candidates = await (
            from c in _context.IncomingNachaEntryClassifications
            join l in _context.IncomingNachaTransactionLinks on
                new { c.IncomingNachaFileIngestionId, EntryId = (int?)c.EntryDetailId, c.AddendaRecordId }
                equals new { l.IncomingNachaFileIngestionId, EntryId = l.EntryDetailId, l.AddendaRecordId }
            where c.IncomingNachaFileIngestionId == ingestionId && l.AchTransactionId != null
            select new { Classification = c, Link = l, AchTransactionId = l.AchTransactionId!.Value }
        ).ToListAsync(ct);

        var transactionIds = candidates.Select(x => x.AchTransactionId).Distinct().ToArray();
        var txMap = await _context.AchTransactions
            .Include(x => x.AchCycle)
            .Where(x => transactionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var clearingHouseIds = txMap.Values
            .Select(x => x.AchCycle.ClearingHouseId)
            .Distinct()
            .ToArray();
        var clearingHouseCodes = await _context.ClearingHouses
            .AsNoTracking()
            .Where(clearingHouse => clearingHouseIds.Contains(clearingHouse.ClearingHouseId))
            .ToDictionaryAsync(clearingHouse => clearingHouse.ClearingHouseId, clearingHouse => clearingHouse.Code, ct);
        var paymentRailCodes = await _context.ClearingHouseConfigs
            .AsNoTracking()
            .Where(config => clearingHouseIds.Contains(config.ClearingHouseId))
            .ToDictionaryAsync(config => config.ClearingHouseId, config => config.PaymentRailCode, ct);

        var created = 0;
        var nowUtcOffset = _timeProvider.GetUtcNow();
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtcOffset, _timeProvider.LocalTimeZone).DateTime;
        foreach (var candidate in candidates)
        {
            if (!txMap.TryGetValue(candidate.AchTransactionId, out var tx))
            {
                continue;
            }

            var evaluation = await _eligibility.EvaluateAsync(
                ingestion,
                candidate.Classification,
                candidate.Link,
                tx,
                nowLocal: nowLocal,
                ct);

            var dispatchKey = BuildIdempotencyKey(ingestion.Id, tx.Id, tx.AchCycleId, candidate.Classification.Id);
            var existing = await _context.IncomingNachaDispatchQueue
                .FirstOrDefaultAsync(x => x.IdempotencyDispatchKey == dispatchKey, ct);
            if (existing is not null)
            {
                continue;
            }

            var status = evaluation.IsEligible
                ? IncomingNachaDispatchQueueStatus.Queued
                : evaluation.IsWaitingWindow
                    ? IncomingNachaDispatchQueueStatus.WaitingWindow
                    : IncomingNachaDispatchQueueStatus.Blocked;

            var nextAttemptAtUtc = status == IncomingNachaDispatchQueueStatus.Queued
                ? nowUtcOffset.UtcDateTime
                : (DateTime?)null;
            var lastErrorCode = status == IncomingNachaDispatchQueueStatus.Blocked
                ? "POLICY_BLOCKED"
                : string.Empty;
            var queueReason = evaluation.Reason;

            if (status == IncomingNachaDispatchQueueStatus.WaitingWindow)
            {
                var window = IncomingNachaDispatchWindowCalculator.Evaluate(
                    tx.AchCycle,
                    nowLocal,
                    _timeProvider.LocalTimeZone);

                switch (window.Position)
                {
                    case IncomingNachaDispatchWindowPosition.Before:
                        nextAttemptAtUtc = window.NextEligibleAtUtc;
                        break;
                    case IncomingNachaDispatchWindowPosition.Expired:
                        status = IncomingNachaDispatchQueueStatus.Blocked;
                        lastErrorCode = "WINDOW_EXPIRED";
                        queueReason = "La ventana operativa del ciclo ya expiró; no se permite despacho automático.";
                        break;
                    case IncomingNachaDispatchWindowPosition.Invalid:
                        status = IncomingNachaDispatchQueueStatus.Blocked;
                        lastErrorCode = "WINDOW_SCHEDULE_INVALID";
                        queueReason = "No fue posible calcular de forma determinística la próxima ventana operativa.";
                        break;
                    default:
                        status = IncomingNachaDispatchQueueStatus.Blocked;
                        lastErrorCode = "WINDOW_POLICY_INCONSISTENT";
                        queueReason = "La evaluación de ventana operativa resultó inconsistente; se bloqueó de forma segura.";
                        break;
                }
            }

            CompareDispatchShadow(
                tx,
                ingestion,
                clearingHouseCodes.GetValueOrDefault(tx.AchCycle.ClearingHouseId),
                paymentRailCodes.GetValueOrDefault(tx.AchCycle.ClearingHouseId),
                status,
                status == IncomingNachaDispatchQueueStatus.Queued,
                status == IncomingNachaDispatchQueueStatus.WaitingWindow,
                evaluation.Priority);

            _context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
            {
                SourceKind = IncomingNachaDispatchSourceKinds.CycleBound,
                IncomingNachaFileIngestionId = ingestion.Id,
                IncomingNachaEntryClassificationId = candidate.Classification.Id,
                IncomingNachaTransactionLinkId = candidate.Link.Id,
                AchTransactionId = tx.Id,
                AchCycleId = tx.AchCycleId,
                ClearingHouseId = tx.AchCycle.ClearingHouseId,
                OperationalDate = ingestion.OperationalDate?.Date ?? tx.AchCycle.ProcessingDate.Date,
                QueueStatus = status,
                Priority = evaluation.Priority,
                IdempotencyDispatchKey = dispatchKey,
                NextAttemptAtUtc = nextAttemptAtUtc,
                LastErrorCode = lastErrorCode,
                LastErrorMessage = queueReason
            });
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return created;
    }

    private static bool IsFileBasedIngestion(IncomingNachaFileIngestion ingestion) =>
        string.Equals(ingestion.ResolutionMode, "AchColFileBased", StringComparison.Ordinal)
        && ingestion.CycleResolutionStatus == IncomingNachaCycleResolutionStatus.ResueltoSinCiclo
        && ingestion.ResolvedAchCycleId is null;

    private async Task<int> PlanFileBasedAsync(IncomingNachaFileIngestion ingestion, CancellationToken ct)
    {
        // Defense in depth: the entry context must be resolved and admitted. A legacy
        // path must never create file-based work, even when the flag is enabled.
        if (ingestion.IngestionStatus != IncomingNachaIngestionStatus.Completado
            || ingestion.ResolvedClearingHouseId is null
            || ingestion.OperationalDate is null
            || ingestion.FileSequence is null
            || ingestion.ExternalCycleNumber is null
            || string.IsNullOrWhiteSpace(ingestion.CanonicalPlainFileName)
            || string.IsNullOrWhiteSpace(ingestion.FileIdModifier))
        {
            return 0;
        }

        var clearingHouse = await _context.ClearingHouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClearingHouseId == ingestion.ResolvedClearingHouseId.Value, ct);
        if (clearingHouse?.IsActive != true
            || !string.Equals(clearingHouse.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        // Classifications and entries must belong to the *same* ingested NACHA
        // header. No matching on trace across unrelated files or other chambers.
        var candidates = await (
            from c in _context.IncomingNachaEntryClassifications.AsNoTracking()
            join e in _context.EntryDetails.AsNoTracking() on c.EntryDetailId equals e.EntryDetailID
            join h in _context.NachaHeaders.AsNoTracking() on e.NachaID equals h.NachaID
            where c.IncomingNachaFileIngestionId == ingestion.Id
                && h.IncomingNachaFileIngestionId == ingestion.Id
                && (c.FunctionalClass == IncomingNachaFunctionalClass.CreditoEntrante
                    || c.FunctionalClass == IncomingNachaFunctionalClass.DebitoEntrante)
            orderby e.EntryDetailID
            select new { Classification = c, Entry = e, Header = h }
        ).ToListAsync(ct);

        // A stable, domain-separated fingerprint is independent of ingestion GUID,
        // external cycle, internal cycle and envelope encryption randomness.
        var localKeys = new HashSet<string>(StringComparer.Ordinal);
        var created = 0;
        foreach (var candidate in candidates)
        {
            // Explicit F4.1 policy is evaluated even if the F3.3 query prefilters
            // functional class. A positive decision never queues or sends a payment.
            var eligibility = await _eligibility.EvaluateFileBasedAsync(
                ingestion, candidate.Classification, candidate.Header, candidate.Entry,
                clearingHouse, featureEnabled: true, ct: ct);
            if (!eligibility.IsEligible || eligibility.IsBlocked || eligibility.IsWaitingWindow
                || !IncomingNachaFileBasedDispatchIdentity.TryBuild(
                    ingestion.ResolvedClearingHouseId.Value,
                    ingestion.OperationalDate.Value,
                    candidate.Header,
                    candidate.Entry,
                    out var dispatchKey))
            {
                // Invalid, return/prenote, ambiguous or incomplete entries remain
                // outside automatic monetary dispatch. Never manufacture a key.
                _logger.LogWarning(
                    "ACHCOL_F41_INELIGIBLE_ENTRY_NOT_PLANNED|IngestionId={IngestionId}|EntryDetailId={EntryDetailId}|Reason={Reason}",
                    ingestion.Id, candidate.Entry.EntryDetailID, eligibility.Reason);
                continue;
            }

            if (!localKeys.Add(dispatchKey)
                || await _context.IncomingNachaDispatchQueue.AsNoTracking()
                    .AnyAsync(x => x.IdempotencyDispatchKey == dispatchKey, ct))
            {
                _logger.LogInformation(
                    "ACHCOL_F33_DUPLICATE_DISPATCH_SUPPRESSED|IngestionId={IngestionId}|EntryDetailId={EntryDetailId}",
                    ingestion.Id, candidate.Entry.EntryDetailID);
                continue;
            }

            _context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
            {
                SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased,
                IncomingNachaFileIngestionId = ingestion.Id,
                IncomingNachaEntryClassificationId = candidate.Classification.Id,
                IncomingNachaTransactionLinkId = null,
                EntryDetailId = candidate.Entry.EntryDetailID,
                AchTransactionId = null,
                AchCycleId = null,
                ClearingHouseId = ingestion.ResolvedClearingHouseId.Value,
                OperationalDate = ingestion.OperationalDate.Value.Date,
                IdempotencyDispatchKey = dispatchKey,
                Priority = eligibility.Priority,
                // No Queued/RetryPending entries yet: existing work handlers are
                // cycle-bound. F4 must explicitly opt in after eligibility checks.
                QueueStatus = IncomingNachaDispatchQueueStatus.Blocked,
                LastErrorCode = "ACHCOL_DISPATCH_NOT_ENABLED_F33",
                LastErrorMessage = "F4.1 elegible; pendiente de F4.2/F5 y autorización de transporte CFA."
            });
            created++;
        }

        if (created > 0)
        {
            // The unique DB index is the last line of protection under concurrency.
            // A genuine unique-key conflict fails this unit of work rather than
            // silently retrying a potentially monetary transaction.
            await _context.SaveChangesAsync(ct);
        }

        return created;
    }

    private void CompareDispatchShadow(
        AchTransaction transaction,
        IncomingNachaFileIngestion ingestion,
        string? clearingHouseCode,
        string? paymentRailCode,
        IncomingNachaDispatchQueueStatus legacyStatus,
        bool legacyEligible,
        bool legacyWaitingWindow,
        int legacyPriority)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        var resolvedContext = _paymentRailContextService.ResolveContext(
            transaction.AchCycle.ClearingHouseId,
            clearingHouseCode,
            transaction.AchCycleId,
            ingestion.OperationalDate?.Date ?? transaction.AchCycle.ProcessingDate.Date,
            paymentRailCode);
        var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(
            transaction.AchCycle.ClearingHouseId,
            clearingHouseCode,
            paymentRailCode));
        var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
            resolvedContext.OperationalContext,
            PaymentRailCapabilityKind.Dispatch,
            legacyStatus.ToString()));
        var shadowResult = _shadowCompareService.CompareDispatchPlanning(
            resolvedContext,
            wrapperResult,
            legacyStatus.ToString(),
            legacyEligible,
            legacyWaitingWindow,
            legacyPriority);

        _logger.LogInformation(
            "PAYMENT_RAIL_SHADOW_COMPARE_DISPATCH|TxId={TxId}|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
            transaction.Id,
            shadowResult.RailCode,
            shadowResult.LegacyDecisionCode,
            shadowResult.WrapperDecisionCode,
            shadowResult.IsEquivalent,
            shadowResult.ComparisonCode);
    }

    private static string BuildIdempotencyKey(Guid ingestionId, int transactionId, string cycleId, Guid classificationId)
    {
        var raw = $"{ingestionId:N}|{transactionId}|{cycleId}|{classificationId:N}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}

internal enum IncomingNachaDispatchWindowPosition
{
    Before,
    Open,
    Expired,
    Invalid
}

internal readonly record struct IncomingNachaDispatchWindowEvaluation(
    IncomingNachaDispatchWindowPosition Position,
    DateTime? NextEligibleAtUtc);

internal static class IncomingNachaDispatchWindowCalculator
{
    public static IncomingNachaDispatchWindowEvaluation Evaluate(
        AchCycle cycle,
        DateTime nowLocal,
        TimeZoneInfo localTimeZone)
    {
        var processingDate = cycle.ProcessingDate.Date;
        var startLocal = cycle.StartTime <= cycle.EndTime
            ? processingDate + cycle.StartTime
            : processingDate.AddDays(-1) + cycle.StartTime;
        var endLocal = processingDate + cycle.EndTime;
        var comparableNow = DateTime.SpecifyKind(nowLocal, DateTimeKind.Unspecified);
        startLocal = DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified);
        endLocal = DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified);

        if (comparableNow > endLocal)
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Expired,
                null);
        }

        if (comparableNow >= startLocal)
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Open,
                null);
        }

        if (localTimeZone.IsInvalidTime(startLocal) || localTimeZone.IsAmbiguousTime(startLocal))
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Invalid,
                null);
        }

        try
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Before,
                TimeZoneInfo.ConvertTimeToUtc(startLocal, localTimeZone));
        }
        catch (ArgumentException)
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Invalid,
                null);
        }
    }
}
