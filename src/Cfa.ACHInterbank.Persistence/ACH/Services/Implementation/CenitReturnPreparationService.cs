using System.Data;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>Conflicto de precondición (409); nunca reintentar una preparación automáticamente.</summary>
public sealed class CenitReturnPreparationConflictException(string message) : Exception(message);

/// <summary>
/// I03-CENIT: preparación explícita, individual y transaccional. El modo AUTOMATICO no
/// ejecuta tareas ni habilita generación; DESACTIVADO nunca escribe una reserva.
/// No comparte la tabla, política de ventanas ni el motor de ACH Colombia.
/// </summary>
public sealed class CenitReturnPreparationService(AchDbContext context, TimeProvider clock)
    : ICenitReturnPreparationService
{
    public const string Prepared = "PREPARADA_SIN_ENVIO";

    public async Task<CenitReturnPreparationItem> PrepareAsync(
        PrepareCenitReturnRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || request.OriginalTransactionId <= 0 || request.ExpectedExecutionId == Guid.Empty)
            throw new ArgumentException("Se requieren la transacción y su ejecución comprobada de I02.", nameof(request));

        // Serializable + índices únicos en BD: lectura de la última evidencia, modo y
        // reserva ocurren en una transacción. No afirmar protección distribuida con InMemory.
        if (context.Database.IsRelational())
            return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);
                var item = await PrepareAttemptAsync(request, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return item;
            });
        return await PrepareAttemptAsync(request, cancellationToken);
    }

    private async Task<CenitReturnPreparationItem> PrepareAttemptAsync(
        PrepareCenitReturnRequest request, CancellationToken ct)
    {
        var houseId = await context.ClearingHouses.AsNoTracking()
            .Where(x => x.Code == RegulatoryCycleScheduleCatalog.CenitCode)
            .Select(x => x.Id).SingleOrDefaultAsync(ct);
        if (houseId <= 0)
            throw new CenitReturnPreparationConflictException("CENIT no está configurada.");

        var settings = await new CenitAutomaticReturnSettingsService(context).GetAsync(ct);
        if (settings.Mode is not (CenitAutomaticReturnSettingsService.WithApproval
            or CenitAutomaticReturnSettingsService.Automatic))
            throw new CenitReturnPreparationConflictException("La preparación CENIT está desactivada.");

        if (await context.CenitReturnPreparations.AsNoTracking()
                .AnyAsync(x => x.OriginalTransactionId == request.OriginalTransactionId, ct)
            || await context.AchReturnsGenerated.AsNoTracking()
                .AnyAsync(x => x.OriginalTransactionId == request.OriginalTransactionId, ct))
            throw new CenitReturnPreparationConflictException(
                "La transacción ya tiene una reserva o una devolución generada.");

        // No confiar en el resultado GET del cliente. Recuperar la última ejecución, incluso
        // si es exitosa o técnica; de ese modo no revivir un rechazo histórico superado.
        var execution = await context.IncomingNachaIntegrationExecution.AsNoTracking()
            .Where(x => x.ClearingHouseId == houseId
                && x.DispatchQueue.AchTransactionId == request.OriginalTransactionId)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.TransactionLink)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.Classification)
            .Include(x => x.DispatchQueue).ThenInclude(q => q.AchTransaction)
                .ThenInclude(t => t!.AchCycle).ThenInclude(c => c!.ClearingHouse)
            .OrderByDescending(x => x.StartedAtUtc).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
        if (execution is null || execution.Id != request.ExpectedExecutionId)
            throw new CenitReturnPreparationConflictException(
                "La ejecución de referencia ya no es el último resultado vigente.");

        var codes = await context.AchReturnCodes.AsNoTracking()
            .Where(x => x.ClearingHouseId == houseId && x.IsActive).ToListAsync(ct);
        var finding = CenitPassiveReturnDetector.Evaluate(execution, houseId, codes);
        if (finding is null || finding.Classification != "INDICIO_CAUSAL_CENIT"
            || string.IsNullOrWhiteSpace(finding.VerifiedReasonCode))
            throw new CenitReturnPreparationConflictException(
                "La evidencia no habilita una preparación ordinaria CENIT.");

        var original = execution.DispatchQueue.AchTransaction!;
        var originalCycle = original.AchCycle!;
        var now = clock.GetUtcNow();
        if (!execution.ProcessedAtUtc.HasValue || execution.ProcessedAtUtc.Value > now.UtcDateTime
            || original.Amount <= 0 || original.IsPrenotification
            || original.Type is not (TransactionTypeEnum.Credit or TransactionTypeEnum.Debit))
            throw new CenitReturnPreparationConflictException("La entrada original requiere revisión.");

        var localNow = TimeZoneInfo.ConvertTime(now,
            TimeZoneInfo.FindSystemTimeZoneById(RegulatoryCycleScheduleCatalog.BogotaTimeZoneId)).DateTime;
        if (localNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            || await context.BankHolidays.AsNoTracking().AnyAsync(h => h.CountryCode == "CO"
                && h.Date == DateOnly.FromDateTime(localNow), ct))
            throw new CenitReturnPreparationConflictException("No es un día hábil operativo CENIT.");

        // Solo ciclo inmediatamente siguiente del MISMO día/Fecha Valor, presente
        // y habilitado en la BD; no inferir días festivos, R23 ni excepciones tardías.
        var cycles = await context.AchCycles.AsNoTracking()
            .Include(c => c.ClearingHouseCycleConfig)
            .Where(c => c.ClearingHouseId == houseId && c.ProcessingDate == originalCycle.ProcessingDate)
            .ToListAsync(ct);
        var target = CenitReturnPreparationWindow.Resolve(original, cycles, localNow);
        if (target is null)
            throw new CenitReturnPreparationConflictException(
                "No hay siguiente ciclo ordinario CENIT verificable con Fecha Valor vigente.");

        var preparation = new CenitReturnPreparation
        {
            Id = Guid.NewGuid(), OriginalTransactionId = original.Id,
            SourceExecutionId = execution.Id, SourceQueueId = execution.DispatchQueueId,
            OriginalCycleId = originalCycle.Id, SuggestedReturnCycleId = target.Id,
            ReasonCode = finding.VerifiedReasonCode, OriginalValueDate = original.EffectiveEntryDate.Date,
            SuggestedValueDate = target.ProcessingDate.Date, PreparedAtUtc = now.UtcDateTime,
            Status = Prepared
        };
        context.CenitReturnPreparations.Add(preparation);
        try
        {
            // SaveChanges genera AuditLogs con usuario/contexto de petición (infraestructura actual).
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
        {
            context.Entry(preparation).State = EntityState.Detached;
            throw new CenitReturnPreparationConflictException(
                "Otra operación reservó la devolución de esta transacción.");
        }
        return ToItem(preparation);
    }

    public async Task<IReadOnlyList<CenitReturnPreparationItem>> ListAsync(
        int maxItems = 100, CancellationToken cancellationToken = default)
    {
        if (maxItems is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(maxItems), "Límite permitido: 1 a 100.");
        // Join obligatorio: incluso si el identificador se conoce, no exponer registros ACH.
        var items = await context.CenitReturnPreparations.AsNoTracking()
            .Where(x => x.OriginalTransaction.AchCycle.ClearingHouse!.Code
                == RegulatoryCycleScheduleCatalog.CenitCode)
            .OrderByDescending(x => x.PreparedAtUtc).ThenByDescending(x => x.Id)
            .Take(maxItems).ToListAsync(cancellationToken);
        return items.Select(ToItem).ToArray();
    }

    private static CenitReturnPreparationItem ToItem(CenitReturnPreparation p) => new(
        p.Id, p.OriginalTransactionId, p.SourceExecutionId, p.SourceQueueId,
        p.OriginalCycleId, p.SuggestedReturnCycleId, p.ReasonCode,
        p.OriginalValueDate, p.SuggestedValueDate, p.PreparedAtUtc, p.Status,
        ProcessingConnected: false);
}

/// <summary>Selección estrictamente ordinaria CENIT (DSP-152): no usa las ventanas ACH.</summary>
internal static class CenitReturnPreparationWindow
{
    internal static AchCycle? Resolve(AchTransaction original,
        IReadOnlyCollection<AchCycle> persistedCycles, DateTime localNow)
    {
        var source = original.AchCycle;
        if (source is null || source.ClearingHouse?.Code != RegulatoryCycleScheduleCatalog.CenitCode
            || source.OperationalStatus != AchCycleOperationalStatus.Closed
            || original.EffectiveEntryDate == default
            || original.EffectiveEntryDate.Date != source.ProcessingDate.Date
            || source.ProcessingDate.Date != localNow.Date)
            return null;

        var resolver = new CycleNumberResolver();
        var number = resolver.Resolve(source.CycleName);
        if (!number.HasValue || number.Value is < 1 or > 4)
            return null;
        var standard = RegulatoryCycleScheduleCatalog.GetRequired(RegulatoryCycleScheduleCatalog.CenitCode);
        var sourceReference = standard.Single(x => x.CycleNumber == number.Value);
        var targetReference = standard.Single(x => x.CycleNumber == number.Value + 1);
        if (source.StartTime != sourceReference.StartTime
            || source.EndTime != sourceReference.EndTime
            || localNow.TimeOfDay < source.EndTime)
            return null;

        var successors = persistedCycles.Where(c => c.Id != source.Id
            && c.ClearingHouseId == source.ClearingHouseId
            && c.ProcessingDate.Date == source.ProcessingDate.Date
            && resolver.Resolve(c.CycleName) == number.Value + 1).ToArray();
        if (successors.Length != 1)
            return null;
        var target = successors[0];
        var policy = target.ClearingHouseCycleConfig;
        if (target.OperationalStatus is not (AchCycleOperationalStatus.Scheduled or AchCycleOperationalStatus.Open)
            || target.StartTime != targetReference.StartTime
            || target.EndTime != targetReference.EndTime
            || target.CutoffTime != targetReference.CutoffTime
            || localNow.TimeOfDay >= target.CutoffTime
            || policy is null || !policy.IsActive || !policy.AllowsReturn
            || policy.ClearingHouseId != source.ClearingHouseId
            || policy.EffectiveFrom.Date > target.ProcessingDate.Date
            || policy.EffectiveTo.HasValue && policy.EffectiveTo.Value.Date < target.ProcessingDate.Date)
            return null;
        return target;
    }
}
