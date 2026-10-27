using System.Data;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I02: mueve una transacción no presentada dentro del mismo día/cámara.
/// Para proveedores relacionales la mutación y el evento comparten una transacción SERIALIZABLE.
/// ADVERTENCIA: exportadores que leyeron el ciclo con antelación deben adoptar el protocolo
/// de concurrencia en I05; requiere homologación relacional antes de habilitarse en producción.
/// </summary>
[Scoped]
public sealed class AchPendingCycleReassignmentService : IAchPendingCycleReassignmentService
{
    private const string AuditReason = "CYCLE_REASSIGNED";
    private readonly AchDbContext _context;
    private readonly IAchPendingCycleEligibilityService _eligibility;
    private readonly IAchBatchRepository _batches;
    private readonly ITransactionPersister _persister;
    private readonly TimeProvider _clock;

    public AchPendingCycleReassignmentService(
        AchDbContext context,
        IAchPendingCycleEligibilityService eligibility,
        IAchBatchRepository batches,
        ITransactionPersister persister,
        TimeProvider clock)
    {
        _context = context;
        _eligibility = eligibility;
        _batches = batches;
        _persister = persister;
        _clock = clock;
    }

    public async Task<AchPendingCycleReassignmentResult> ReassignAsync(int transactionId, CancellationToken ct = default)
    {
        if (transactionId <= 0)
            return new(transactionId, false, "INVALID_TRANSACTION_ID");

        // La base relacional debe confirmar ciclo/lote/evento juntos y serializar dos ejecutores I02.
        if (!_context.Database.IsRelational())
            return await ReassignCoreAsync(transactionId, ct); // Solo pruebas de unidad no relacionales.

        // El caso de uso se invoca desde un scope propio: nunca absorber cambios pendientes
        // de otro flujo cuando la estrategia de ejecución necesite reintentar.
        if (_context.ChangeTracker.HasChanges())
            return new(transactionId, false, "PENDING_CONTEXT_CHANGES");

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var result = await ReassignCoreAsync(transactionId, ct);
                await dbTransaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                // SaveChanges puede haber aceptado cambios antes del commit externo.
                // Restablecer tracking es necesario para una revalidación real al reintentar.
                _context.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private async Task<AchPendingCycleReassignmentResult> ReassignCoreAsync(int transactionId, CancellationToken ct)
    {
        // I01 se vuelve a ejecutar DENTRO de la transacción de escritura; un positivo previo no basta.
        var decision = await _eligibility.EvaluateAsync(transactionId, ct);
        if (!decision.IsEligible || string.IsNullOrWhiteSpace(decision.FromCycleId)
            || string.IsNullOrWhiteSpace(decision.ToCycleId))
            return new(transactionId, false, decision.ReasonCode, decision.FromCycleId, decision.ToCycleId);

        var transaction = await _context.AchTransactions
            .Include(x => x.AchBatch)
            .SingleOrDefaultAsync(x => x.Id == transactionId, ct);
        if (transaction is null || transaction.State != AchTransferStateEnum.Pending
            || transaction.AchCycleId != decision.FromCycleId
            || transaction.AchBatch is null || transaction.AchBatch.AchCycleId != decision.FromCycleId)
            return new(transactionId, false, "ASSIGNMENT_CHANGED", decision.FromCycleId, decision.ToCycleId);

        var sourceBatch = transaction.AchBatch;
        var targetCycle = await _context.AchCycles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == decision.ToCycleId, ct);
        if (targetCycle is null || targetCycle.ClearingHouseId != decision.ClearingHouseId
            || targetCycle.ProcessingDate.Date != transaction.EffectiveEntryDate.Date
            || sourceBatch.EffectiveEntryDate.Date != transaction.EffectiveEntryDate.Date)
            return new(transactionId, false, "TARGET_INCONSISTENT", decision.FromCycleId, decision.ToCycleId);

        var key = $"ACH-CR-001:{transactionId}:{decision.FromCycleId}:{decision.ToCycleId}";
        if (key.Length > 128)
            return new(transactionId, false, "AUDIT_KEY_TOO_LONG", decision.FromCycleId, decision.ToCycleId);

        if (await _context.AchTransactionStateEvents.AsNoTracking().AnyAsync(e =>
            e.AchTransactionId == transactionId && e.ReasonCode == AuditReason
            && e.IdempotencyKey == key, ct))
            return new(transactionId, false, "ALREADY_REASSIGNED", decision.FromCycleId, decision.ToCycleId);

        // El método compartido reproduce la llave de composición vigente usada por BatchResolver.
        var targetBatch = await _batches.FindForTransactionAsync(
            targetCycle.Id, sourceBatch.CompanyName, sourceBatch.CompanyIdentification,
            sourceBatch.CompanyEntryDescription, sourceBatch.EffectiveEntryDate, ct);
        if (targetBatch is not null && (
            targetBatch.ServiceClassCode != sourceBatch.ServiceClassCode
            || targetBatch.CompanyEntryDescriptionId != sourceBatch.CompanyEntryDescriptionId
            || targetBatch.OriginOrOdfi != sourceBatch.OriginOrOdfi))
            return new(transactionId, false, "TARGET_BATCH_INCOMPATIBLE", decision.FromCycleId, decision.ToCycleId);

        if (targetBatch is null)
        {
            targetBatch = new AchBatch
            {
                AchCycleId = targetCycle.Id,
                EffectiveEntryDate = sourceBatch.EffectiveEntryDate,
                CompanyName = sourceBatch.CompanyName,
                CompanyIdentification = sourceBatch.CompanyIdentification,
                CompanyEntryDescription = sourceBatch.CompanyEntryDescription,
                CompanyEntryDescriptionId = sourceBatch.CompanyEntryDescriptionId,
                OriginOrOdfi = sourceBatch.OriginOrOdfi,
                ServiceClassCode = sourceBatch.ServiceClassCode,
                BatchSequenceNumber = 0
            };
            await _batches.AddAsync(targetBatch, ct);
            await _context.SaveChangesAsync(ct); // La transacción externa sigue abierta.
        }

        var sourceCycleId = transaction.AchCycleId;
        var sourceBatchId = sourceBatch.Id;
        transaction.AchCycleId = targetCycle.Id;
        transaction.AchBatchId = targetBatch.Id;
        transaction.AchBatch = targetBatch;
        // No modificar StateChangedAtUtc: el estado financiero Pending NO ha cambiado.
        var occurredAtUtc = _clock.GetUtcNow().UtcDateTime;
        _context.AchTransactionStateEvents.Add(new AchTransactionStateEvent
        {
            AchTransactionId = transaction.Id,
            FromState = AchTransferStateEnum.Pending,
            ToState = AchTransferStateEnum.Pending,
            Source = AchStateEventSourceEnum.System,
            ReasonCode = AuditReason,
            IdempotencyKey = key,
            ClearingHouseId = decision.ClearingHouseId,
            OccurredAtUtc = occurredAtUtc,
            PayloadJson = JsonSerializer.Serialize(new
            {
                fromCycleId = sourceCycleId,
                toCycleId = targetCycle.Id,
                fromBatchId = sourceBatchId,
                toBatchId = targetBatch.Id,
                effectiveEntryDate = transaction.EffectiveEntryDate.Date,
                changedAtUtc = occurredAtUtc,
                reason = "PENDING_NOT_PRESENTED"
            })
        });
        await _context.SaveChangesAsync(ct); // FKs de transacción y auditoría dentro del mismo commit.

        // Mismas reglas de cómputo existentes de originación: no mantener totales obsoletos.
        await _persister.UpdateBatchTotalsAsync(sourceBatch, ct);
        await _persister.UpdateBatchTotalsAsync(targetBatch, ct);
        await _persister.UpdateBatchServiceClassCodeAsync(sourceBatch, ct);
        await _persister.UpdateBatchServiceClassCodeAsync(targetBatch, ct);
        await _context.SaveChangesAsync(ct);

        return new(transactionId, true, "MOVED", sourceCycleId, targetCycle.Id, sourceBatchId, targetBatch.Id);
    }
}
