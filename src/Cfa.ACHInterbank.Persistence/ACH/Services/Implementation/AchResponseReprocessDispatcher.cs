using Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchResponseReprocessDispatcher : IAchResponseReprocessDispatcher
{
    private const string SystemActor = "system:ach-response-reprocess-dispatcher";
    private readonly AchDbContext _db;
    private readonly IServiceScopeFactory _scopeFactory;

    public AchResponseReprocessDispatcher(AchDbContext db, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _scopeFactory = scopeFactory;
    }

    public async Task<AchResponseReprocessDispatchResult> DispatchAsync(int batchSize, TimeSpan leaseDuration,
        string instanceId, CancellationToken cancellationToken = default)
    {
        batchSize = Math.Clamp(batchSize, 1, 500);
        if (leaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("La instancia es obligatoria.", nameof(instanceId));
        var now = DateTime.UtcNow;
        var candidates = await _db.AchResponseReprocessAttempts.AsNoTracking()
            .Where(x => x.Status == AchResponseReprocessAttemptStatuses.Pending
                || x.Status == AchResponseReprocessAttemptStatuses.Running && x.LeaseExpiresAtUtc < now)
            .OrderBy(x => x.RequestedAtUtc).ThenBy(x => x.Id).Take(batchSize)
            .Select(x => new Candidate(x.Id, x.Status)).ToListAsync(cancellationToken);
        var claimed = 0;
        var completed = 0;
        var functional = 0;
        var technical = 0;
        var skipped = 0;
        foreach (var candidate in candidates)
        {
            if (!await ClaimAsync(candidate, instanceId, leaseDuration, cancellationToken)) { skipped++; continue; }
            claimed++;
            AchResponseReprocessExecutionResult result;
            try
            {
                await RenewLeaseAsync(candidate.Id, instanceId, leaseDuration, cancellationToken);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<IAchResponseReprocessPipeline>();
                var attempt = await scope.ServiceProvider.GetRequiredService<AchDbContext>().AchResponseReprocessAttempts
                    .AsNoTracking().SingleAsync(x => x.Id == candidate.Id, cancellationToken);
                result = await pipeline.ExecuteAsync(attempt.AchResponseId, candidate.Id, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result = new(AchResponseReprocessResultCode.TechnicalFailure, "El pipeline terminó con error técnico.", Sanitize(ex.Message));
            }

            if (!await FinalizeAsync(candidate.Id, instanceId, result, cancellationToken)) { skipped++; continue; }
            if (result.IsTechnicalFailure) technical++;
            else if (result.RequiresManualReview) functional++;
            else completed++;
        }
        return new(candidates.Count, claimed, completed, functional, technical, skipped);
    }

    private async Task<bool> ClaimAsync(Candidate candidate, string instanceId, TimeSpan lease, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var version = Guid.NewGuid();
        var affected = await _db.AchResponseReprocessAttempts
            .Where(x => x.Id == candidate.Id && (x.Status == AchResponseReprocessAttemptStatuses.Pending
                || x.Status == AchResponseReprocessAttemptStatuses.Running && x.LeaseExpiresAtUtc < now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, AchResponseReprocessAttemptStatuses.Running)
                .SetProperty(x => x.ClaimedBy, instanceId)
                .SetProperty(x => x.ClaimedAtUtc, now)
                .SetProperty(x => x.StartedAtUtc, x => x.StartedAtUtc ?? now)
                .SetProperty(x => x.LastHeartbeatAtUtc, now)
                .SetProperty(x => x.LeaseExpiresAtUtc, now.Add(lease))
                .SetProperty(x => x.Version, version), ct);
        if (affected != 1) return false;

        var attempt = await _db.AchResponseReprocessAttempts.Include(x => x.AchResponse).SingleAsync(x => x.Id == candidate.Id, ct);
        var response = attempt.AchResponse;
        var previous = response.EstadoProcesamiento;
        AchResponseStatePolicy.EnsureTransition(previous, AchResponseProcessingStatus.Reprocesando,
            SystemActor, attempt.Reason, attempt.CorrelationId);
        response.EstadoProcesamiento = AchResponseProcessingStatus.Reprocesando;
        response.FechaActualizacion = now;
        AddAudit(response, previous, AchResponseProcessingStatus.Reprocesando,
            candidate.Status == AchResponseReprocessAttemptStatuses.Running ? "ReprocessLeaseRecovered" : "ReprocessClaimed",
            attempt, instanceId, now);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private Task<int> RenewLeaseAsync(long attemptId, string instanceId, TimeSpan lease, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return _db.AchResponseReprocessAttempts.Where(x => x.Id == attemptId && x.Status == AchResponseReprocessAttemptStatuses.Running
                && x.ClaimedBy == instanceId && x.LeaseExpiresAtUtc >= now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastHeartbeatAtUtc, now)
                .SetProperty(x => x.LeaseExpiresAtUtc, now.Add(lease)), ct);
    }

    private async Task<bool> FinalizeAsync(long attemptId, string instanceId, AchResponseReprocessExecutionResult result, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var attempt = await _db.AchResponseReprocessAttempts.Include(x => x.AchResponse).SingleOrDefaultAsync(x => x.Id == attemptId
            && x.Status == AchResponseReprocessAttemptStatuses.Running && x.ClaimedBy == instanceId && x.LeaseExpiresAtUtc >= now, ct);
        if (attempt is null) return false;
        var terminal = result.IsTechnicalFailure ? AchResponseReprocessAttemptStatuses.FailedTechnical
            : result.RequiresManualReview ? AchResponseReprocessAttemptStatuses.FailedFunctional
            : AchResponseReprocessAttemptStatuses.Completed;
        var target = result.IsTechnicalFailure ? AchResponseProcessingStatus.ErrorTecnico
            : result.RequiresManualReview ? AchResponseProcessingStatus.RequiereRevisionManual
            : AchResponseProcessingStatus.Reprocesada;
        var previous = attempt.AchResponse.EstadoProcesamiento;
        AchResponseStatePolicy.EnsureTransition(previous, target, SystemActor, attempt.Reason, attempt.CorrelationId);
        attempt.Status = terminal;
        attempt.CompletedAtUtc = now;
        attempt.ResultCode = result.Code.ToString();
        attempt.Result = result.Result;
        attempt.ErrorType = result.IsTechnicalFailure ? "TechnicalFailure" : null;
        attempt.ErrorDetailSanitized = result.ErrorDetailSanitized;
        attempt.LeaseExpiresAtUtc = null;
        attempt.LastHeartbeatAtUtc = now;
        attempt.AchResponse.EstadoProcesamiento = target;
        attempt.AchResponse.FechaActualizacion = now;
        AddAudit(attempt.AchResponse, previous, target, "ReprocessCompleted", attempt, instanceId, now);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static void AddAudit(AchResponse response, AchResponseProcessingStatus previous, AchResponseProcessingStatus next,
        string action, AchResponseReprocessAttempt attempt, string instance, DateTime at)
        => response.AuditEntries.Add(new AchResponseAudit
        {
            EntityType = nameof(AchResponse), EntityId = response.Id.ToString(), AchResponseId = response.Id,
            Action = action, PreviousState = previous.ToString(), NewState = next.ToString(), Actor = SystemActor,
            Reason = attempt.Reason, CorrelationId = attempt.CorrelationId, OccurredAtUtc = at,
            SanitizedMetadata = $"attemptId={attempt.Id}; instance={instance}"
        });

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Error técnico no detallado.";
        var sanitized = value.Replace("\r", " ").Replace("\n", " ");
        return sanitized[..Math.Min(sanitized.Length, 500)];
    }

    private sealed record Candidate(long Id, string Status);
}
