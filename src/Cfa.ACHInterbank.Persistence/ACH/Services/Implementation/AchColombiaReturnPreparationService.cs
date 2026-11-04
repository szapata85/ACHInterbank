using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I03-ACH: prepara, pero NO genera ni envía devoluciones. La clave única en BD
/// asegura una sola preparación por original, incluso entre instancias.
/// </summary>
internal sealed class AchColombiaReturnPreparationService(AchDbContext context, TimeProvider clock)
{
    internal const string Prepared = "PREPARADA_SIN_ENVIO";
    internal const string LateCreditReview = "REVISION_TARDIA_CREDITO";

    internal async Task<int> PreparePendingAsync(int maxItems, CancellationToken ct)
    {
        // La parametrización I01 no activa el generador ni la transmisión.
        var houseId = await context.ClearingHouses.AsNoTracking()
            .Where(x => x.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode)
            .Select(x => x.Id).SingleOrDefaultAsync(ct);
        if (houseId <= 0 || !await context.ClearingHouseConfigs.AsNoTracking()
                .AnyAsync(x => x.ClearingHouseId == houseId &&
                    (x.AutomaticReturnMode == AchColombiaAutomaticReturnSettingsService.WithApproval
                        || x.AutomaticReturnMode == AchColombiaAutomaticReturnSettingsService.Automatic), ct))
            return 0;

        var transactionIds = await context.IncomingNachaProcessingEvents.AsNoTracking()
            .Where(x => x.EventType == AchColombiaApplicationFailureDetector.DetectedEvent
                && x.AchTransactionId.HasValue
                && !context.AchColombiaReturnPreparations.Any(p => p.OriginalTransactionId == x.AchTransactionId.Value))
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => x.AchTransactionId!.Value)
            .Distinct().Take(Math.Clamp(maxItems, 1, 100)).ToListAsync(ct);
        var prepared = 0;
        foreach (var transactionId in transactionIds)
        {
            if (await PrepareFromDetectedEventAsync(transactionId, ct)) prepared++;
        }
        return prepared;
    }

    internal async Task<bool> PrepareFromDetectedEventAsync(int originalTransactionId, CancellationToken ct)
    {
        var tx = await context.AchTransactions.AsNoTracking()
            .Include(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
            .SingleOrDefaultAsync(x => x.Id == originalTransactionId, ct);
        if (tx?.AchCycle?.ClearingHouse is null
            || !string.Equals(tx.AchCycle.ClearingHouse.Code,
                RegulatoryCycleScheduleCatalog.AchColombiaCode, StringComparison.OrdinalIgnoreCase)
            || tx.Type is not (TransactionTypeEnum.Credit or TransactionTypeEnum.Debit or TransactionTypeEnum.Prenotification))
            return false;

        var config = await context.ClearingHouseConfigs.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClearingHouseId == tx.AchCycle.ClearingHouseId, ct);
        if (config is null || config.Id != tx.AchCycle.ClearingHouseId
            || config.AutomaticReturnMode is not (AchColombiaAutomaticReturnSettingsService.WithApproval
                or AchColombiaAutomaticReturnSettingsService.Automatic))
            return false;

        if (await context.AchColombiaReturnPreparations.AsNoTracking()
                .AnyAsync(x => x.OriginalTransactionId == tx.Id, ct)
            || await context.AchReturnsGenerated.AsNoTracking()
                .AnyAsync(x => x.OriginalTransactionId == tx.Id, ct))
            return false;

        // Se acepta solamente el evento funcional confirmado por I02; nunca un texto de error,
        // una incidencia sin causal, ni una falla de formato, transporte o prevalidación.
        var eventRow = await context.IncomingNachaProcessingEvents.AsNoTracking()
            .Where(x => x.AchTransactionId == tx.Id
                && x.EventType == AchColombiaApplicationFailureDetector.DetectedEvent
                && x.RaisedBy == "achcol.application-failure.detector")
            .OrderBy(x => x.OccurredAtUtc).FirstOrDefaultAsync(ct);
        if (eventRow is null || !TryReadVerifiedEvidence(eventRow.EvidenceJson, tx.Id,
                out var reason, out var queueId, out var executionId))
            return false;

        // Corroborar contra la ejecución que produjo el evento; no basta confiar en JSON.
        var queue = await context.IncomingNachaDispatchQueue.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == queueId, ct);
        var execution = await context.IncomingNachaIntegrationExecution.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == executionId, ct);
        if (queue is null || execution is null
            || queue.AchTransactionId != tx.Id
            || queue.AchCycleId != tx.AchCycleId
            || queue.IncomingNachaFileIngestionId != eventRow.IncomingNachaFileIngestionId
            || !queue.IncomingNachaTransactionLinkId.HasValue
            || queue.ClearingHouseId != tx.AchCycle.ClearingHouseId
            || queue.SourceKind != IncomingNachaDispatchSourceKinds.CycleBound
            || execution.DispatchQueueId != queue.Id
            || execution.ClearingHouseId != queue.ClearingHouseId
            || execution.ResultCode.Trim().ToUpperInvariant() != reason
            || execution.SoapMethodName != "Proc_Transacciones"
            || !execution.ProcessedAtUtc.HasValue
            || !execution.AchReturnCodeId.HasValue
            || !execution.ResponseCatalogId.HasValue
            || execution.TransportStatus != IntegrationTransportStatus.Succeeded
            || execution.BusinessStatus != IntegrationResponseBusinessStatus.Rejected
            || !execution.IsFunctionalRejection || execution.IsTechnicalFailure
            || execution.IsSuccessful || execution.RetryAllowed || execution.RequiresManualReview
            || execution.ProcessingStatus != IncomingNachaIndividualProcessingStatus.Completed
            || !IncomingNachaPostProcessingMode.IsLive(execution.ExecutionMode))
            return false;

        if (!await context.ReturnReasons.AsNoTracking()
                .AnyAsync(x => x.Code == reason && x.IsForReturn, ct)
            || !await context.Set<AchReturnCode>().AsNoTracking().AnyAsync(x =>
                x.Id == execution.AchReturnCodeId && x.ClearingHouseId == queue.ClearingHouseId
                && x.Code == reason && x.IsActive
                && (tx.Type == TransactionTypeEnum.Credit && x.AppliesToCredit
                    || tx.Type == TransactionTypeEnum.Debit && x.AppliesToDebit
                    || tx.Type == TransactionTypeEnum.Prenotification && x.AppliesToPrenotification), ct))
            return false;

        var now = clock.GetUtcNow();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(RegulatoryCycleScheduleCatalog.BogotaTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone).DateTime;
        var cycles = await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouseCycleConfig)
            .Where(x => x.ClearingHouseId == tx.AchCycle.ClearingHouseId
                && x.ProcessingDate >= tx.AchCycle.ProcessingDate.Date
                && x.ProcessingDate <= localNow.Date.AddDays(1))
            .ToListAsync(ct);
        var decision = AchColombiaReturnPreparationWindow.Evaluate(tx, cycles, localNow);
        if (decision is null)
            return false;

        var candidate = new AchColombiaReturnPreparation
        {
            OriginalTransactionId = tx.Id,
            SourceEventId = eventRow.Id,
            OriginalCycleId = tx.AchCycleId,
            SuggestedReturnCycleId = decision.Value.CycleId,
            ReasonCode = reason,
            Status = decision.Value.LateCreditReview ? LateCreditReview : Prepared,
            PreparedAtUtc = now.UtcDateTime
        };
        context.AchColombiaReturnPreparations.Add(candidate);
        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
        {
            // Otra instancia reservó antes el mismo original. No volver a intentar
            // la inserción ni modificar el resultado original del core.
            context.Entry(candidate).State = EntityState.Detached;
            return false;
        }
    }

    private static bool TryReadVerifiedEvidence(string json, int transactionId,
        out string reason, out Guid queueId, out Guid executionId)
    {
        reason = string.Empty;
        queueId = Guid.Empty;
        executionId = Guid.Empty;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.GetProperty("camera").GetString() != RegulatoryCycleScheduleCatalog.AchColombiaCode
                || root.GetProperty("transactionId").GetInt32() != transactionId
                || root.GetProperty("origin").GetString() != "CORE_FUNCIONAL"
                || !root.GetProperty("causeVerified").GetBoolean()
                || root.GetProperty("returnGenerated").GetBoolean()
                || root.GetProperty("returnSent").GetBoolean())
                return false;
            reason = root.GetProperty("suggestedReturnReason").GetString() ?? string.Empty;
            if (reason.Length != 3 || reason[0] != 'R'
                || !char.IsAsciiDigit(reason[1]) || !char.IsAsciiDigit(reason[2])
                || root.GetProperty("responseCode").GetString() != reason)
                return false;
            queueId = root.GetProperty("queueId").GetGuid();
            executionId = root.GetProperty("executionId").GetGuid();
            return queueId != Guid.Empty && executionId != Guid.Empty;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
        {
            return false;
        }
    }
}

/// <summary>
/// Evalúa únicamente los ciclos persistidos ACH Colombia: no inventa una sesión ni
/// adopta los horarios de CENIT. La validación se repetirá antes de generar el archivo.
/// </summary>
internal static class AchColombiaReturnPreparationWindow
{
    internal readonly record struct Result(string? CycleId, bool LateCreditReview);

    internal static Result? Evaluate(AchTransaction original, IReadOnlyCollection<AchCycle> existingCycles,
        DateTime localNow)
    {
        var numberResolver = new CycleNumberResolver();
        // La secuencia operacional se ordena por número de ciclo, NO por hora
        // de apertura: el ciclo 1 de ACH Colombia cruza la medianoche.
        var numbered = existingCycles
            .Where(x => x.OperationalStatus != AchCycleOperationalStatus.Cancelled)
            .Select(x => (Cycle: x, Number: numberResolver.Resolve(x.CycleName)))
            .ToArray();
        if (numbered.Any(x => !x.Number.HasValue)
            || numbered.GroupBy(x => (x.Cycle.ProcessingDate.Date, x.Number))
                .Any(x => x.Count() > 1))
            return null;

        var ordered = numbered.OrderBy(x => x.Cycle.ProcessingDate.Date)
            .ThenBy(x => x.Number!.Value).ToArray();
        var originalIndex = Array.FindIndex(ordered, x => x.Cycle.Id == original.AchCycleId);
        if (originalIndex < 0 || original.EffectiveEntryDate.Date > localNow.Date)
            return null;

        var originalCycle = ordered[originalIndex];
        var following = ordered.Skip(originalIndex + 1).Take(4).ToArray();
        var sevenCycles = originalCycle.Cycle.ClearingHouseCycleConfig?.PolicyVersion
            == RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleConfirmedVersion;
        // Para la versión activa de siete ciclos, la tabla de fuentes permitidas
        // ya está registrada en RegulatoryCycleScheduleCatalog. Se aplica tal cual.
        var windows = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate.ReturnWindows;
        foreach (var candidate in following)
        {
            var cycle = candidate.Cycle;
            if (cycle.ClearingHouseCycleConfig?.AllowsReturn != true
                || cycle.OperationalStatus is not (AchCycleOperationalStatus.Open or AchCycleOperationalStatus.Scheduled)
                || cycle.ProcessingDate.Date.Add(cycle.EndTime) <= localNow
                || cycle.ProcessingDate.Date < original.EffectiveEntryDate.Date)
                continue;

            if (sevenCycles)
            {
                if (cycle.ClearingHouseCycleConfig.PolicyVersion
                    != RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleConfirmedVersion)
                    continue;
                var sourceDay = cycle.ProcessingDate.Date == originalCycle.Cycle.ProcessingDate.Date
                    ? RegulatoryCycleSourceDay.CurrentOperationalDay
                    : RegulatoryCycleSourceDay.PreviousOperationalDay;
                var window = windows.FirstOrDefault(x => x.ReturnCycleNumber == candidate.Number!.Value);
                if (window is null || !window.Allows(originalCycle.Number!.Value, sourceDay))
                    continue;
            }
            return new Result(cycle.Id, LateCreditReview: false);
        }

        // Se requiere evidencia completa de los cuatro ciclos siguientes para
        // calificar como tardía. Crédito pasa a revisión; débito/prenotificación
        // se bloquean. No aplicar el Esquema de Calidad automáticamente.
        if (following.Length == 4
            && following.All(x => x.Cycle.ProcessingDate.Date.Add(x.Cycle.EndTime) <= localNow)
            && original.Type == TransactionTypeEnum.Credit && !original.IsPrenotification)
            return new Result(null, LateCreditReview: true);
        return null;
    }
}
