using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>NPT-08/09: persistent claim, fenced finish and independent recovery.</summary>
public sealed class AxonNotificationClaimStore : IAxonNotificationClaimStore
{
    private readonly AchDbContext _db;
    public AxonNotificationClaimStore(AchDbContext db) => _db = db;

    // The old preflight branches saved tracked snapshots unconditionally. A race
    // with another notification worker could overwrite its InFlight claim or
    // manual quarantine. Finish preflight using the same SQL CAS discipline.
    public async Task<bool> TryFinishPreflightAsync(long attemptId, Guid responseId,
        AxonNotificationCompletion completion, DateTime nowUtc, CancellationToken ct)
    {
        if (!_db.Database.IsRelational()) return false;
        await using var trx = await _db.Database.BeginTransactionAsync(ct);
        var claimed = await _db.AchResponseNotificationAttempts
            .Where(x => x.Id == attemptId && x.AchResponseId == responseId
                && (x.EstadoNotificacion == AchResponseNotificationStatus.Pendiente
                    || x.EstadoNotificacion == AchResponseNotificationStatus.PendienteReintento)
                && x.AchResponse.EstadoProcesamiento != AchResponseProcessingStatus.RequiereRevisionManual
                && x.AchResponse.EstadoProcesamiento != AchResponseProcessingStatus.Notificada)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.EstadoNotificacion, completion.AttemptStatus)
                .SetProperty(x => x.ExisteError, completion.HasError)
                .SetProperty(x => x.CodigoError, completion.ErrorCode)
                .SetProperty(x => x.DescripcionError, completion.ErrorDescription)
                .SetProperty(x => x.ErrorTecnico, completion.TechnicalError)
                .SetProperty(x => x.FechaEnvio, (DateTime?)nowUtc), ct);
        if (claimed != 1)
        {
            await trx.RollbackAsync(ct);
            DetachTrackedSnapshots(attemptId, responseId);
            return false;
        }
        var nextVersion = Guid.NewGuid();
        var updatedParent = await _db.AchResponses
            .Where(x => x.Id == responseId
                && x.EstadoProcesamiento != AchResponseProcessingStatus.RequiereRevisionManual
                && x.EstadoProcesamiento != AchResponseProcessingStatus.Notificada)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.EstadoProcesamiento, completion.ResponseStatus)
                .SetProperty(x => x.Version, nextVersion)
                .SetProperty(x => x.FechaActualizacion, (DateTime?)nowUtc), ct);
        if (updatedParent != 1)
        {
            await trx.RollbackAsync(ct);
            DetachTrackedSnapshots(attemptId, responseId);
            return false;
        }
        DetachTrackedSnapshots(attemptId, responseId);
        _db.AchResponseAudits.Add(new AchResponseAudit
        {
            EntityType = "AchResponseNotificationAttempt", EntityId = attemptId.ToString(),
            AchResponseId = responseId, Action = "AxonPreflightFenced", PreviousState = "Pendiente",
            NewState = completion.AttemptStatus.ToString(), Actor = "axon.notification.preflight",
            Reason = "Fallo de prevalidacion sin transporte; finalizacion SQL atomica.",
            OccurredAtUtc = nowUtc, SanitizedMetadata = JsonSerializer.Serialize(new { code = completion.ErrorCode })
        });
        await _db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> TryClaimAsync(long attemptId, string token, DateTime nowUtc, CancellationToken ct)
    {
        if (!_db.Database.IsRelational()) return false;
        var deadline = nowUtc.AddMinutes(30);
        return 1 == await _db.AchResponseNotificationAttempts
            .Where(x => x.Id == attemptId && (x.EstadoNotificacion == AchResponseNotificationStatus.Pendiente
                || x.EstadoNotificacion == AchResponseNotificationStatus.PendienteReintento)
                && x.AchResponse.EstadoProcesamiento != AchResponseProcessingStatus.RequiereRevisionManual
                && x.AchResponse.EstadoProcesamiento != AchResponseProcessingStatus.Notificada)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.EstadoNotificacion, AchResponseNotificationStatus.InFlight)
                .SetProperty(x => x.ClaimToken, token)
                .SetProperty(x => x.LeaseExpiresAtUtc, deadline)
                .SetProperty(x => x.TransportStartedAtUtc, (DateTime?)null), ct);
    }

    public async Task<bool> MarkTransportIntentAsync(long attemptId, string token, DateTime nowUtc, CancellationToken ct)
    {
        if (!_db.Database.IsRelational()) return false;
        return 1 == await _db.AchResponseNotificationAttempts
            .Where(x => x.Id == attemptId && x.EstadoNotificacion == AchResponseNotificationStatus.InFlight
                && x.ClaimToken == token && x.LeaseExpiresAtUtc > nowUtc
                && x.AchResponse.EstadoProcesamiento != AchResponseProcessingStatus.RequiereRevisionManual)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.TransportStartedAtUtc, nowUtc), ct);
    }

    public async Task<bool> TryFinalizeAsync(long attemptId, Guid responseId, string token,
        AxonNotificationCompletion completion, DateTime nowUtc, CancellationToken ct)
    {
        if (!_db.Database.IsRelational()) return false;
        await using var trx = await _db.Database.BeginTransactionAsync(ct);
        var updatedAttempt = await _db.AchResponseNotificationAttempts
            .Where(x => x.Id == attemptId && x.AchResponseId == responseId
                && x.EstadoNotificacion == AchResponseNotificationStatus.InFlight
                && x.ClaimToken == token && x.LeaseExpiresAtUtc > nowUtc
                && x.AchResponse.EstadoProcesamiento != AchResponseProcessingStatus.RequiereRevisionManual
                && x.AchResponse.EstadoProcesamiento != AchResponseProcessingStatus.Notificada)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.EstadoNotificacion, completion.AttemptStatus)
                .SetProperty(x => x.ExisteError, completion.HasError)
                .SetProperty(x => x.CodigoError, completion.ErrorCode)
                .SetProperty(x => x.DescripcionError, completion.ErrorDescription)
                .SetProperty(x => x.ErrorTecnico, completion.TechnicalError)
                .SetProperty(x => x.RequestPayload, completion.RequestPayload)
                .SetProperty(x => x.ResponsePayload, completion.ResponsePayload)
                .SetProperty(x => x.FechaEnvio, (DateTime?)nowUtc), ct);
        if (updatedAttempt != 1)
        {
            await trx.RollbackAsync(ct);
            DetachTrackedSnapshots(attemptId, responseId);
            return false;
        }
        var versionAfterFinalize = Guid.NewGuid();
        var updatedResponse = await _db.AchResponses
            .Where(x => x.Id == responseId
                && x.EstadoProcesamiento != AchResponseProcessingStatus.RequiereRevisionManual
                && x.EstadoProcesamiento != AchResponseProcessingStatus.Notificada)
            .ExecuteUpdateAsync(set => set
                .SetProperty(x => x.EstadoProcesamiento, completion.ResponseStatus)
                .SetProperty(x => x.Version, versionAfterFinalize)
                .SetProperty(x => x.FechaActualizacion, (DateTime?)nowUtc), ct);
        if (updatedResponse != 1)
        {
            await trx.RollbackAsync(ct);
            DetachTrackedSnapshots(attemptId, responseId);
            return false;
        }
        // ExecuteUpdate bypasses EF tracking. Detach old snapshots before persisting
        // audit; otherwise a caller's tracked response might overwrite the CAS.
        DetachTrackedSnapshots(attemptId, responseId);
        _db.AchResponseAudits.Add(new AchResponseAudit
        {
            EntityType = "AchResponseNotificationAttempt", EntityId = attemptId.ToString(),
            AchResponseId = responseId, Action = "AxonFencedFinish", PreviousState = "InFlight",
            NewState = completion.AttemptStatus.ToString(), Actor = "axon.notification.claim",
            Reason = "Finalizacion bajo token y lease; sin SOAP dentro de transaccion.",
            CorrelationId = token, OccurredAtUtc = nowUtc,
            SanitizedMetadata = JsonSerializer.Serialize(new { responseState = completion.ResponseStatus.ToString(),
                code = completion.ErrorCode, payloadSha256 = Sha256(completion.ResponsePayload) })
        });
        await _db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);
        return true;
    }

    public async Task RecordLateEvidenceAsync(long attemptId, Guid responseId, string token,
        string evidenceCode, string? responsePayload, CancellationToken ct)
    {
        if (!_db.Database.IsRelational()) return;
        // Do not flush any outdated tracked attempt/response state. Keep immutable
        // evidence in a fresh context, with a hash rather than raw bank payload.
        await using var auditDb = new AchDbContext(
            _db.GetService<IDbContextOptions>() as DbContextOptions<AchDbContext>
            ?? throw new InvalidOperationException("NPT_AXON_EVIDENCE_CONTEXT_OPTIONS_MISSING"));
        auditDb.AchResponseAudits.Add(new AchResponseAudit
        {
            EntityType = "AchResponseNotificationAttempt", EntityId = attemptId.ToString(),
            AchResponseId = responseId, Action = "AxonLateOutcomeFenced",
            PreviousState = "InFlight", NewState = "RequiereRevisionManual",
            Actor = "axon.notification.claim",
            Reason = "Respuesta tardia: lease/token perdidos; resultado requiere conciliacion.",
            CorrelationId = token, OccurredAtUtc = DateTime.UtcNow,
            SanitizedMetadata = JsonSerializer.Serialize(new { code = evidenceCode,
                payloadSha256 = Sha256(responsePayload) })
        });
        await auditDb.SaveChangesAsync(ct);
    }

    public async Task<int> QuarantineExpiredAsync(DateTime nowUtc, CancellationToken ct)
    {
        if (!_db.Database.IsRelational()) return 0;
        var candidates = await _db.AchResponseNotificationAttempts.AsNoTracking()
            .Where(x => x.EstadoNotificacion == AchResponseNotificationStatus.InFlight
                && (!x.LeaseExpiresAtUtc.HasValue || x.LeaseExpiresAtUtc <= nowUtc))
            .OrderBy(x => x.Id).Take(100)
            .Select(x => new { x.Id, x.AchResponseId, x.ClaimToken, x.TransportStartedAtUtc })
            .ToListAsync(ct);
        var changed = 0;
        foreach (var candidate in candidates)
        {
            await using var trx = await _db.Database.BeginTransactionAsync(ct);
            var affected = await _db.AchResponseNotificationAttempts
                .Where(x => x.Id == candidate.Id && x.EstadoNotificacion == AchResponseNotificationStatus.InFlight
                    && x.ClaimToken == candidate.ClaimToken
                    && (!x.LeaseExpiresAtUtc.HasValue || x.LeaseExpiresAtUtc <= nowUtc))
                .ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.EstadoNotificacion, AchResponseNotificationStatus.RequiereRevisionManual)
                    .SetProperty(x => x.CodigoError, "AXON_OUTCOME_UNKNOWN_RECONCILE")
                    .SetProperty(x => x.DescripcionError, "Lease expirado; conciliacion obligatoria sin retransmision."), ct);
            if (affected == 1)
            {
                var versionAfterQuarantine = Guid.NewGuid();
                await _db.AchResponses.Where(x => x.Id == candidate.AchResponseId
                    && x.EstadoProcesamiento != AchResponseProcessingStatus.Notificada)
                    .ExecuteUpdateAsync(set => set
                        .SetProperty(x => x.EstadoProcesamiento, AchResponseProcessingStatus.RequiereRevisionManual)
                        .SetProperty(x => x.Version, versionAfterQuarantine)
                        .SetProperty(x => x.FechaActualizacion, (DateTime?)nowUtc), ct);
                // The watchdog is independent and works with a fresh DbContext per scan.
                _db.AchResponseAudits.Add(new AchResponseAudit
                {
                    EntityType = "AchResponseNotificationAttempt", EntityId = candidate.Id.ToString(),
                    AchResponseId = candidate.AchResponseId, Action = "AxonLeaseExpired",
                    PreviousState = "InFlight", NewState = "RequiereRevisionManual",
                    Actor = "axon.notification.watchdog",
                    Reason = "Lease vencido, no reenvio automatico incluso si no inicio transporte.",
                    CorrelationId = candidate.ClaimToken ?? string.Empty, OccurredAtUtc = nowUtc,
                    SanitizedMetadata = JsonSerializer.Serialize(new { transportMayHaveStarted = candidate.TransportStartedAtUtc.HasValue })
                });
                await _db.SaveChangesAsync(ct);
                changed++;
            }
            await trx.CommitAsync(ct);
        }
        return changed;
    }

    private void DetachTrackedSnapshots(long attemptId, Guid responseId)
    {
        foreach (var entry in _db.ChangeTracker.Entries<AchResponseNotificationAttempt>()
            .Where(e => e.Entity.Id == attemptId).ToArray()) entry.State = EntityState.Detached;
        foreach (var entry in _db.ChangeTracker.Entries<AchResponse>()
            .Where(e => e.Entity.Id == responseId).ToArray()) entry.State = EntityState.Detached;
    }

    private static string Sha256(string? value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)));
}
