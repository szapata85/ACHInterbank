using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ContrapartidaDispatchPersistenceService : IContrapartidaDispatchPersistenceService
{
    private readonly AchDbContext _context;

    public ContrapartidaDispatchPersistenceService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<ContrapartidaDispatchItem> EnsurePendingDispatchAsync(
        AchTransaction transaction,
        int clearingHouseId,
        CancellationToken ct = default)
    {
        if (transaction.ClassificationStatus != AchTransactionClassificationStatus.Determined
            || transaction.Direction != AchTransactionDirection.Outgoing
            || transaction.Origin != AchTransactionOrigin.Cfa
            || transaction.MonetaryIntegrationRoute != AchMonetaryIntegrationRoute.ProcContrapartidas)
        {
            throw new InvalidOperationException(
                "La transacción no es elegible para el movimiento débito de contrapartidas y no será encolada.");
        }

        ContrapartidaDispatchItem? existing = null;
        if (transaction.Id > 0)
        {
            existing = await _context.ContrapartidaDispatchItems
                .FirstOrDefaultAsync(x => x.AchTransactionId == transaction.Id, ct);
        }
        else
        {
            existing = _context.ContrapartidaDispatchItems.Local
                .FirstOrDefault(x => ReferenceEquals(x.AchTransaction, transaction));
        }

        if (existing is not null)
        {
            return existing;
        }

        if (clearingHouseId <= 0)
        {
            throw new InvalidOperationException($"No se encontró cámara válida para el ciclo {transaction.AchCycleId}.");
        }

        var resolvedBatch = transaction.AchBatch;
        if (resolvedBatch is null && transaction.AchBatchId > 0)
        {
            resolvedBatch = _context.AchBatches.Local.FirstOrDefault(b => b.Id == transaction.AchBatchId)
                ?? await _context.AchBatches.FirstOrDefaultAsync(b => b.Id == transaction.AchBatchId, ct);
        }

        if (resolvedBatch is null)
        {
            throw new InvalidOperationException("No se pudo resolver el lote ACH asociado para el dispatch a contrapartida.");
        }

        var item = new ContrapartidaDispatchItem
        {
            AchTransaction = transaction,
            AchCycleId = transaction.AchCycleId,
            AchBatch = resolvedBatch,
            AchBatchId = resolvedBatch.Id > 0 ? resolvedBatch.Id : transaction.AchBatchId,
            ClearingHouseId = clearingHouseId,
            State = ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport,
            NextAttemptAtUtc = DateTime.UtcNow
        };

        await _context.ContrapartidaDispatchItems.AddAsync(item, ct);
        return item;
    }

    public async Task<ContrapartidaDispatchBatch> CreateBatchAsync(ContrapartidaDispatchBatchCreateRequest request, CancellationToken ct = default)
    {
        var batch = new ContrapartidaDispatchBatch
        {
            AchCycleId = request.AchCycleId,
            ClearingHouseId = request.ClearingHouseId,
            AchBatchId = request.AchBatchId,
            TriggerType = request.TriggerType,
            RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy.Trim(),
            JobId = request.JobId,
            TriggeredAtUtc = DateTime.UtcNow,
            RequestPayloadXml = request.RequestPayloadXml ?? string.Empty,
            Status = ContrapartidaDispatchBatchStatusEnum.Created
        };

        await _context.ContrapartidaDispatchBatches.AddAsync(batch, ct);
        return batch;
    }

    public async Task<ContrapartidaDispatchAttempt> RegisterAttemptAsync(ContrapartidaDispatchAttemptCreateRequest request, CancellationToken ct = default)
    {
        var item = await _context.ContrapartidaDispatchItems
            .FirstOrDefaultAsync(x => x.Id == request.DispatchItemId, ct)
            ?? throw new KeyNotFoundException($"No existe el dispatch item {request.DispatchItemId}.");

        var nextAttemptNumber = item.AttemptCount + 1;

        var attempt = new ContrapartidaDispatchAttempt
        {
            DispatchItemId = request.DispatchItemId,
            DispatchBatchId = request.DispatchBatchId,
            AttemptNumber = nextAttemptNumber,
            StartedAtUtc = request.StartedAtUtc,
            FinishedAtUtc = request.FinishedAtUtc,
            Result = request.Result,
            CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId.Trim(),
            TriggeredBy = string.IsNullOrWhiteSpace(request.TriggeredBy) ? "system" : request.TriggeredBy.Trim(),
            RetryEligible = request.RetryEligible,
            ExternalResponseCode = request.ExternalResponseCode?.Trim() ?? string.Empty,
            ExternalResponseMessage = request.ExternalResponseMessage?.Trim() ?? string.Empty,
            ErrorCode = request.ErrorCode?.Trim() ?? string.Empty,
            ErrorMessage = request.ErrorMessage?.Trim() ?? string.Empty,
            RequestPayloadXml = request.RequestPayloadXml ?? string.Empty,
            ResponsePayloadXml = request.ResponsePayloadXml ?? string.Empty,
            SoapMethodName = request.SoapMethodName?.Trim() ?? string.Empty,
            SoapEndpoint = request.SoapEndpoint?.Trim() ?? string.Empty,
            ExecutionMode = request.ExecutionMode?.Trim() ?? string.Empty,
            DurationMs = request.DurationMs,
            SoapResponseCode = request.SoapResponseCode?.Trim() ?? string.Empty,
            SoapResponseDescription = request.SoapResponseDescription?.Trim() ?? string.Empty,
            SoapTechnicalStatus = request.SoapTechnicalStatus?.Trim() ?? string.Empty,
            ResponseCatalogId = request.ResponseCatalogId,
            TransportStatus = request.TransportStatus,
            BusinessStatus = request.BusinessStatus,
            RetryAllowed = request.RetryAllowed,
            RequiresManualReview = request.RequiresManualReview,
            ProcessedAtUtc = request.ProcessedAtUtc,
            IsSuccessful = request.IsSuccessful,
            IsFunctionalRejection = request.IsFunctionalRejection,
            IsTechnicalFailure = request.IsTechnicalFailure,
            TechnicalException = request.TechnicalException?.Trim() ?? string.Empty
        };

        item.AttemptCount = nextAttemptNumber;
        item.LastAttemptAtUtc = request.FinishedAtUtc ?? request.StartedAtUtc;
        item.LastCorrelationId = attempt.CorrelationId;
        item.LastDispatchedBy = attempt.TriggeredBy;
        item.LastResponseCode = attempt.ExternalResponseCode;
        item.LastErrorCode = attempt.ErrorCode;
        item.LastErrorMessage = attempt.ErrorMessage;

        if (request.Result == ContrapartidaDispatchAttemptResultEnum.Success)
        {
            item.State = ContrapartidaDispatchItemStateEnum.ReportedToContrapartida;
            item.LastSuccessAtUtc = request.FinishedAtUtc ?? request.StartedAtUtc;
            item.NextAttemptAtUtc = null;
        }
        else
        {
            item.State = request.RetryEligible
                ? ContrapartidaDispatchItemStateEnum.RetryPending
                : ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed;
            item.NextAttemptAtUtc = request.RetryEligible ? DateTime.UtcNow : null;
        }

        await _context.ContrapartidaDispatchAttempts.AddAsync(attempt, ct);
        return attempt;
    }
}
