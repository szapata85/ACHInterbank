using System.Data;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>Precondición de revisión no satisfecha; nunca se reintenta una aprobación de oficio.</summary>
public sealed class CenitReturnReviewConflictException(string message) : Exception(message);

/// <summary>
/// I04-CENIT: segunda persona y evidencia actual para RESOLVER una preparación de I03.
/// No genera archivos, secuencias, movimientos, asientos, devoluciones ni transmisiones.
/// R06/R23/R34, prenotificaciones y devolución de devolución quedan fuera de esta vía.
/// </summary>
public sealed class CenitReturnReviewService(AchDbContext context, TimeProvider clock)
    : ICenitReturnReviewService
{
    public const string Approved = "APROBADA_SIN_ENVIO";
    public const string Discarded = "DESCARTADA";

    public async Task<IReadOnlyList<CenitReturnReviewItem>> ListAsync(int maxItems = 100,
        CancellationToken ct = default)
    {
        if (maxItems is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maxItems), "Límite permitido: 1 a 100.");
        var rows = await context.CenitReturnPreparations.AsNoTracking()
            .Where(p => p.OriginalTransaction.AchCycle.ClearingHouse!.Code
                == RegulatoryCycleScheduleCatalog.CenitCode)
            .OrderByDescending(p => p.PreparedAtUtc).ThenByDescending(p => p.Id)
            .Take(maxItems).ToListAsync(ct);
        return rows.Select(ToItem).ToArray();
    }

    public async Task<CenitReturnReviewItem> DecideAsync(Guid preparationId,
        CenitReturnReviewDecision decision, string authenticatedActorId, CancellationToken ct = default)
    {
        if (preparationId == Guid.Empty || decision is null ||
            decision.Action is not ("APROBAR" or "DESCARTAR") ||
            decision.ExpectedStatus != CenitReturnPreparationService.Prepared ||
            string.IsNullOrWhiteSpace(decision.Justification) ||
            decision.Justification.Trim().Length is < 15 or > 500 ||
            (decision.EvidenceReference is not null &&
                (decision.EvidenceReference.Length is < 4 or > 120 ||
                 !decision.EvidenceReference.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '/' or '.' or ':'))) ||
            (decision.Action == "APROBAR" && string.IsNullOrWhiteSpace(decision.EvidenceReference)))
            throw new ArgumentException("Se requiere decisión, estado esperado, justificación y referencia de soporte para aprobar.", nameof(decision));
        if (string.IsNullOrWhiteSpace(authenticatedActorId) || authenticatedActorId.Length > 128 ||
            authenticatedActorId.Equals("system", StringComparison.OrdinalIgnoreCase))
            throw new CenitReturnReviewConflictException("No se pudo acreditar la identidad del revisor.");

        // Leer modo, identidad de preparación, evidencia y estado bajo una transacción relacional.
        if (context.Database.IsRelational())
            return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);
                var result = await DecideAttemptAsync(preparationId, decision, authenticatedActorId, ct);
                await transaction.CommitAsync(ct);
                return result;
            });
        return await DecideAttemptAsync(preparationId, decision, authenticatedActorId, ct);
    }

    private async Task<CenitReturnReviewItem> DecideAttemptAsync(Guid id,
        CenitReturnReviewDecision decision, string actorId, CancellationToken ct)
    {
        var candidate = await context.CenitReturnPreparations
            .Include(p => p.OriginalTransaction).ThenInclude(t => t.AchCycle).ThenInclude(c => c.ClearingHouse)
            .SingleOrDefaultAsync(p => p.Id == id, ct);
        if (candidate?.OriginalTransaction?.AchCycle?.ClearingHouse?.Code
            != RegulatoryCycleScheduleCatalog.CenitCode)
            throw new CenitReturnReviewConflictException("La preparación CENIT no existe.");
        if (candidate.Status != CenitReturnPreparationService.Prepared ||
            candidate.Status != decision.ExpectedStatus)
            throw new CenitReturnReviewConflictException("La preparación ya fue resuelta o cambió de estado.");

        // La evidencia inmutable del INSERT de I03 identifica a la primera persona.
        // Las reservas previas sin identidad demostrable NO son aprobables (fail-closed).
        var audit = await context.AuditLogs.AsNoTracking()
            .Where(x => x.EntityName == nameof(CenitReturnPreparation)
                && x.EntityId == candidate.Id.ToString() && x.BeforeJson == null
                && x.AfterJson != null)
            .OrderBy(x => x.ChangedAt).FirstOrDefaultAsync(ct);
        if (audit is null || string.IsNullOrWhiteSpace(audit.ChangedBy) ||
            audit.ChangedBy.Equals("system", StringComparison.OrdinalIgnoreCase))
            throw new CenitReturnReviewConflictException(
                "No se acredita quién preparó la reserva; requiere gestión manual.");
        if (string.Equals(actorId, audit.ChangedBy, StringComparison.OrdinalIgnoreCase))
            throw new CenitReturnReviewConflictException(
                "La persona que preparó la reserva no puede resolverla.");

        // Descartar siempre es posible para cerrar reservas inseguras, aun si se desactivó el modo.
        // Aprobar exige, además, verificaciones ACTUALES independientes de la consulta previa.
        if (decision.Action == "APROBAR")
            await ValidateApprovalAsync(candidate, ct);

        candidate.Status = decision.Action == "APROBAR" ? Approved : Discarded;
        candidate.ReviewedByUserId = actorId;
        candidate.ReviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        candidate.DecisionJustification = decision.Justification.Trim();
        candidate.DecisionEvidenceReference = decision.EvidenceReference;
        try
        {
            // SaveChanges genera AuditLogs. Status es token de concurrencia EF.
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CenitReturnReviewConflictException(
                "Otra persona modificó la preparación. Actualiza la consulta.");
        }
        return ToItem(candidate);
    }

    internal async Task ValidateApprovalAsync(CenitReturnPreparation p, CancellationToken ct)
    {
        var settings = await new CenitAutomaticReturnSettingsService(context).GetAsync(ct);
        if (settings.Mode is not (CenitAutomaticReturnSettingsService.WithApproval
            or CenitAutomaticReturnSettingsService.Automatic))
            throw new CenitReturnReviewConflictException("CENIT está desactivado; no es posible aprobar.");

        var now = clock.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(now,
            TimeZoneInfo.FindSystemTimeZoneById(RegulatoryCycleScheduleCatalog.BogotaTimeZoneId)).DateTime;
        var original = p.OriginalTransaction;
        var originalCycle = original.AchCycle;
        var cenitId = originalCycle.ClearingHouseId;
        if (localNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
            await context.BankHolidays.AsNoTracking().AnyAsync(h => h.CountryCode == "CO"
                && h.Date == DateOnly.FromDateTime(localNow), ct) ||
            original.Amount <= 0 || original.IsPrenotification ||
            original.Type is not (TransactionTypeEnum.Credit or TransactionTypeEnum.Debit) ||
            p.ReasonCode is "R06" or "R23" or "R34" ||
            await context.AchReturnsGenerated.AsNoTracking()
                .AnyAsync(x => x.OriginalTransactionId == p.OriginalTransactionId, ct))
            throw new CenitReturnReviewConflictException("La operación requiere tratamiento manual o ya fue devuelta.");

        // Un resultado posterior, aunque exitoso o técnico, invalida la aprobación.
        var execution = await context.IncomingNachaIntegrationExecution.AsNoTracking()
            .Where(x => x.ClearingHouseId == cenitId
                && x.DispatchQueue.AchTransactionId == p.OriginalTransactionId)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.TransactionLink)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.Classification)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.AchTransaction)
                .ThenInclude(t => t!.AchCycle).ThenInclude(c => c!.ClearingHouse)
            .OrderByDescending(x => x.StartedAtUtc).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (execution is null || execution.Id != p.SourceExecutionId ||
            execution.DispatchQueueId != p.SourceQueueId ||
            !execution.ProcessedAtUtc.HasValue || execution.ProcessedAtUtc.Value > now.UtcDateTime)
            throw new CenitReturnReviewConflictException("La evidencia de origen cambió o no es vigente.");

        var codes = await context.AchReturnCodes.AsNoTracking()
            .Where(x => x.ClearingHouseId == cenitId && x.IsActive).ToListAsync(ct);
        var finding = CenitPassiveReturnDetector.Evaluate(execution, cenitId, codes);
        if (finding is null || finding.Classification != "INDICIO_CAUSAL_CENIT" ||
            finding.VerifiedReasonCode != p.ReasonCode ||
            finding.OriginalTransactionId != p.OriginalTransactionId)
            throw new CenitReturnReviewConflictException("La causal no está verificada para CENIT.");

        var cycles = await context.AchCycles.AsNoTracking()
            .Include(c => c.ClearingHouseCycleConfig)
            .Where(c => c.ClearingHouseId == cenitId
                && c.ProcessingDate == originalCycle.ProcessingDate).ToListAsync(ct);
        var target = CenitReturnPreparationWindow.Resolve(original, cycles, localNow);
        if (target is null || target.Id != p.SuggestedReturnCycleId ||
            p.OriginalCycleId != originalCycle.Id ||
            p.OriginalValueDate.Date != original.EffectiveEntryDate.Date ||
            p.SuggestedValueDate.Date != target.ProcessingDate.Date)
            throw new CenitReturnReviewConflictException(
                "La ventana ordinaria, el ciclo o la Fecha Valor ya no son válidos.");
    }

    private static CenitReturnReviewItem ToItem(CenitReturnPreparation p) => new(
        p.Id, p.OriginalTransactionId, p.SourceExecutionId, p.ReasonCode,
        p.OriginalCycleId, p.SuggestedReturnCycleId, p.Status,
        p.PreparedAtUtc, p.ReviewedByUserId, p.ReviewedAtUtc,
        p.DecisionJustification, p.DecisionEvidenceReference,
        ProcessingConnected: false);
}
