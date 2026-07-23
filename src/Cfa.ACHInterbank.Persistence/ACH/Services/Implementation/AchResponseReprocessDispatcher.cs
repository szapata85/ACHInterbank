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
        if (string.IsNullOrWhiteSpace(instanceId) || string.Equals(instanceId, "AUTO", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Se requiere la identidad efectiva de la instancia Quartz.", nameof(instanceId));
        var now = DateTime.UtcNow;
        var candidates = await _db.AchResponseReprocessAttempts.AsNoTracking()
            .Where(x => x.Status == AchResponseReprocessAttemptStatuses.Pending
                || x.Status == AchResponseReprocessAttemptStatuses.Running && x.LeaseExpiresAtUtc < now)
            .OrderBy(x => x.RequestedAtUtc).ThenBy(x => x.Id).Take(batchSize)
            .Select(x => new Candidate(x.Id, x.Status)).ToListAsync(cancellationToken);
        var claimed = 0; var completed = 0; var functional = 0; var technical = 0; var skipped = 0;
        foreach (var candidate in candidates)
        {
            var claim = await ClaimAsync(candidate, instanceId, leaseDuration, cancellationToken);
            if (claim is null) { skipped++; continue; }
            claimed++;
            using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var heartbeat = RunHeartbeatAsync(claim, instanceId, leaseDuration, heartbeatCancellation.Token);
            AchResponseReprocessExecutionResult result;
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<IAchResponseReprocessPipeline>();
                result = await pipeline.ExecuteAsync(claim.ResponseId, claim.AttemptId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result = new(AchResponseReprocessResultCode.TechnicalFailure, "El pipeline terminó con error técnico.", Sanitize(ex.Message));
            }
            finally
            {
                heartbeatCancellation.Cancel();
                await AwaitHeartbeatAsync(heartbeat);
            }

            if (claim.LostOwnership || result.Code == AchResponseReprocessResultCode.LostOwnership
                || !await FinalizeAsync(claim, instanceId, result, cancellationToken)) { skipped++; continue; }
            if (result.IsTechnicalFailure) technical++;
            else if (result.RequiresManualReview) functional++;
            else completed++;
        }
        return new(candidates.Count, claimed, completed, functional, technical, skipped);
    }

    private async Task<Claim?> ClaimAsync(Candidate candidate, string instanceId, TimeSpan lease, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var affected = await _db.AchResponseReprocessAttempts.Where(x => x.Id == candidate.Id &&
                (x.Status == AchResponseReprocessAttemptStatuses.Pending || x.Status == AchResponseReprocessAttemptStatuses.Running && x.LeaseExpiresAtUtc < now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, AchResponseReprocessAttemptStatuses.Running)
                .SetProperty(x => x.ClaimedBy, instanceId).SetProperty(x => x.ClaimedAtUtc, now)
                .SetProperty(x => x.StartedAtUtc, x => x.StartedAtUtc ?? now).SetProperty(x => x.LastHeartbeatAtUtc, now)
                .SetProperty(x => x.LeaseExpiresAtUtc, now.Add(lease)).SetProperty(x => x.Version, Guid.NewGuid()), ct);
        if (affected != 1) { await transaction.RollbackAsync(ct); return null; }
        var attempt = await _db.AchResponseReprocessAttempts.Include(x => x.AchResponse).SingleAsync(x => x.Id == candidate.Id, ct);
        var response = attempt.AchResponse;
        var previous = response.EstadoProcesamiento;
        AchResponseStatePolicy.EnsureTransition(previous, AchResponseProcessingStatus.Reprocesando, SystemActor, attempt.Reason, attempt.CorrelationId);
        response.EstadoProcesamiento = AchResponseProcessingStatus.Reprocesando;
        response.FechaActualizacion = now;
        AddAudit(response, previous, AchResponseProcessingStatus.Reprocesando,
            candidate.Status == AchResponseReprocessAttemptStatuses.Running ? "ReprocessLeaseRecovered" : "ReprocessClaimed", attempt, instanceId, now);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new Claim(attempt.Id, attempt.AchResponseId, attempt.Version);
    }

    private async Task RunHeartbeatAsync(Claim claim, string instanceId, TimeSpan lease, CancellationToken ct)
    {
        var interval = TimeSpan.FromTicks(Math.Min(lease.Ticks / 3, TimeSpan.FromSeconds(30).Ticks));
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(interval, ct);
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
                var now = DateTime.UtcNow;
                var replacementVersion = Guid.NewGuid();
                var affected = await db.AchResponseReprocessAttempts.Where(x => x.Id == claim.AttemptId
                    && x.Status == AchResponseReprocessAttemptStatuses.Running && x.ClaimedBy == instanceId
                    && x.Version == claim.Version && x.LeaseExpiresAtUtc >= now)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastHeartbeatAtUtc, now)
                        .SetProperty(x => x.LeaseExpiresAtUtc, now.Add(lease)).SetProperty(x => x.Version, replacementVersion), ct);
                if (affected != 1) { claim.LostOwnership = true; return; }
                claim.Version = replacementVersion;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task<bool> FinalizeAsync(Claim claim, string instanceId, AchResponseReprocessExecutionResult result, CancellationToken ct)
    {
        if (result.Code == AchResponseReprocessResultCode.LostOwnership) return false;
        var now = DateTime.UtcNow;
        var terminal = result.IsTechnicalFailure ? AchResponseReprocessAttemptStatuses.FailedTechnical
            : result.RequiresManualReview ? AchResponseReprocessAttemptStatuses.FailedFunctional : AchResponseReprocessAttemptStatuses.Completed;
        var target = result.IsTechnicalFailure ? AchResponseProcessingStatus.ErrorTecnico
            : result.RequiresManualReview ? AchResponseProcessingStatus.RequiereRevisionManual : AchResponseProcessingStatus.Reprocesada;
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var affected = await _db.AchResponseReprocessAttempts.Where(x => x.Id == claim.AttemptId
            && x.Status == AchResponseReprocessAttemptStatuses.Running && x.ClaimedBy == instanceId && x.Version == claim.Version
            && x.LeaseExpiresAtUtc >= now).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, terminal)
                .SetProperty(x => x.CompletedAtUtc, now).SetProperty(x => x.ResultCode, result.Code.ToString())
                .SetProperty(x => x.Result, result.Result).SetProperty(x => x.ErrorType, result.IsTechnicalFailure ? "TechnicalFailure" : null)
                .SetProperty(x => x.ErrorDetailSanitized, result.ErrorDetailSanitized).SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(x => x.LastHeartbeatAtUtc, now).SetProperty(x => x.Version, Guid.NewGuid()), ct);
        if (affected != 1) { await transaction.RollbackAsync(ct); return false; }
        _db.ChangeTracker.Clear();
        var attempt = await _db.AchResponseReprocessAttempts.Include(x => x.AchResponse).SingleAsync(x => x.Id == claim.AttemptId, ct);
        var previous = attempt.AchResponse.EstadoProcesamiento;
        AchResponseStatePolicy.EnsureTransition(previous, target, SystemActor, attempt.Reason, attempt.CorrelationId);
        attempt.AchResponse.EstadoProcesamiento = target;
        attempt.AchResponse.FechaActualizacion = now;
        var action = result.Code == AchResponseReprocessResultCode.AlreadyApplied ? "ReprocessAlreadyApplied"
            : result.IsTechnicalFailure ? "ReprocessFailedTechnical" : result.RequiresManualReview ? "ReprocessFailedFunctional" : "ReprocessCompleted";
        AddAudit(attempt.AchResponse, previous, target, action, attempt, instanceId, now, result.Code);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static async Task AwaitHeartbeatAsync(Task heartbeat)
    {
        try { await heartbeat; } catch (OperationCanceledException) { }
    }

    private static void AddAudit(AchResponse response, AchResponseProcessingStatus previous, AchResponseProcessingStatus next,
        string action, AchResponseReprocessAttempt attempt, string instance, DateTime at, AchResponseReprocessResultCode? code = null)
        => response.AuditEntries.Add(new AchResponseAudit
        {
            EntityType = nameof(AchResponse), EntityId = response.Id.ToString(), AchResponseId = response.Id, Action = action,
            PreviousState = previous.ToString(), NewState = next.ToString(), Actor = SystemActor, Reason = attempt.Reason,
            CorrelationId = attempt.CorrelationId, OccurredAtUtc = at,
            SanitizedMetadata = $"attemptId={attempt.Id}; instanceId={instance}; resultCode={code?.ToString() ?? "Claimed"}"
        });

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Error técnico no detallado.";
        var sanitized = value.Replace("\r", " ").Replace("\n", " ");
        return sanitized[..Math.Min(sanitized.Length, 500)];
    }

    private sealed record Candidate(long Id, string Status);
    private sealed class Claim(long attemptId, Guid responseId, Guid version)
    {
        public long AttemptId { get; } = attemptId;
        public Guid ResponseId { get; } = responseId;
        public Guid Version { get; set; } = version;
        public bool LostOwnership { get; set; }
    }
}
