using System.Data;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I05: previsualizacion con tiempo virtual exclusivamente read-only.
/// La aplicacion usa SOLAMENTE la hora real del servidor y el motor canonico de estados.
/// No altera fecha de BD, vencimientos, Quartz ni fabrica un archivo de aprobacion.
/// </summary>
public sealed partial class NachaInboundSimulationService
{
    public async Task<PrenotificationTacitAcceptancePreviewResponse> PreviewTacitAcceptanceAsync(
        PrenotificationTacitAcceptancePreviewRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Contract);
        var realNow = _timeProvider.GetUtcNow();
        var evaluationTime = request.EvaluationTimeUtc?.ToUniversalTime() ?? realNow;
        var problems = new List<PrenotificationSimulationContractViolation>();
        var shape = PrenotificationSimulationContract.Validate(request.Contract);
        problems.AddRange(shape.Violations);
        if (request.Contract.Outcome != PrenotificationSimulationOutcome.NoReturnByDeadline)
            problems.Add(new("TACIT_OUTCOME_REQUIRED", "Outcome", "La aceptacion tacita no representa una devolucion."));
        if ((evaluationTime - realNow).Duration() > TimeSpan.FromDays(366))
            problems.Add(new("SIMULATED_TIME_OUT_OF_RANGE", "EvaluationTimeUtc", "El reloj virtual debe estar dentro de 366 dias de la hora real."));
        if (!_options.IsUatLike() || !_options.DifferentialResponsesEnabled)
            problems.Add(new("SIMULATOR_UNAVAILABLE", "Outcome", "Solo se permite en simulador UAT/local configurado."));
        if (!shape.ShapeValid || problems.Count > 0)
            return new(evaluationTime, realNow, request.EvaluationTimeUtc.HasValue, true, false, [], problems);

        var preflight = await PreviewPrenotificationAsync(request.Contract, ct);
        // El preflight I02 usa reloj REAL: en la vista virtual solamente se sustituye
        // PRENOTIFICATION_DEADLINE_NOT_REACHED, nunca un control de seguridad/correlacion.
        problems.AddRange(preflight.Violations.Where(v => v.Code != "PRENOTIFICATION_DEADLINE_NOT_REACHED"));
        if (preflight.Candidates.Any(x => x.OriginalBusinessDate != request.Contract.BusinessDate))
            problems.Add(new("TACIT_BUSINESS_DATE_MISMATCH", "BusinessDate", "La fecha indicada no corresponde a la prenotificacion original."));
        var ids = request.Contract.OriginalPrenotificationIds.ToArray();
        var rules = await _context.PrenotificationValidationAttempts.AsNoTracking()
            .Where(x => x.AchTransactionId.HasValue && ids.Contains(x.AchTransactionId.Value)
                        && x.ThirdPartyAccountValidation.CurrentAttemptId == x.Id)
            .Select(x => new
            {
                TransactionId = x.AchTransactionId!.Value,
                Enabled = x.ThirdPartyAccountValidation.PolicyRule != null
                          && x.ThirdPartyAccountValidation.PolicyRule.TacitAcceptanceEnabled
            }).ToListAsync(ct);
        var priorReturnIds = await _context.NachaInboundSimulationEntries.AsNoTracking()
            .Where(x => x.TransactionId.HasValue && ids.Contains(x.TransactionId.Value)
                        && x.Simulation.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationReturn
                        && x.Simulation.Status != NachaInboundSimulationStatus.Failed
                        && x.Simulation.Status != NachaInboundSimulationStatus.Blocked)
            .Select(x => x.TransactionId!.Value).Distinct().ToListAsync(ct);
        var priorReturns = priorReturnIds.ToHashSet();
        var items = new List<PrenotificationTacitAcceptanceItem>();
        foreach (var id in ids)
        {
            var candidate = preflight.Candidates.SingleOrDefault(x => x.OriginalPrenotificationId == id);
            var issues = candidate?.Violations.ToList() ?? new List<PrenotificationSimulationContractViolation>();
            if (candidate is null)
                issues.Add(new("PRENOTIFICATION_NOT_FOUND", "OriginalPrenotificationIds", $"Prenotificacion {id} no encontrada."));
            if (!rules.Any(x => x.TransactionId == id && x.Enabled))
                issues.Add(new("TACIT_POLICY_DISABLED", "OriginalPrenotificationIds", $"La politica canonica vigente no permite aceptacion tacita para {id}."));
            if (priorReturns.Contains(id))
                issues.Add(new("RETURN_SIMULATION_ALREADY_GENERATED", "OriginalPrenotificationIds", $"Existe devolucion simulada pendiente o generada para {id}."));
            var deadline = candidate?.ResponseDeadlineAtUtc;
            var structurallyValid = issues.Count == 0 && problems.Count == 0 && deadline.HasValue;
            var virtualDue = PrenotificationTacitAcceptanceDecision.IsDue(
                deadline, evaluationTime, issues.Count == 0 && problems.Count == 0,
                rules.Any(x => x.TransactionId == id && x.Enabled), priorReturns.Contains(id));
            var realDue = PrenotificationTacitAcceptanceDecision.IsDue(
                deadline, realNow, issues.Count == 0 && problems.Count == 0,
                rules.Any(x => x.TransactionId == id && x.Enabled), priorReturns.Contains(id));
            items.Add(new(id, deadline, virtualDue, realDue,
                !structurallyValid ? "Blocked" : virtualDue ? "WouldApply" : "DeadlineNotReached", issues));
        }
        return new(evaluationTime, realNow, request.EvaluationTimeUtc.HasValue, true,
            _hostEnvironment is not null && !_hostEnvironment.IsProduction()
                && _options.IsUatLike() && _options.DifferentialResponsesEnabled
                && !_options.AllowExternalTransmission && !_options.AllowAutoImport
                && _stateTransitionService is not null, items, problems);
    }

    public async Task<PrenotificationTacitAcceptanceApplyResponse> ApplyDueTacitAcceptanceAsync(
        PrenotificationSimulationContractRequest request, string actor, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_hostEnvironment is null || _hostEnvironment.IsProduction()
            || !_options.IsUatLike() || !_options.DifferentialResponsesEnabled
            || _options.AllowExternalTransmission || _options.AllowAutoImport
            || _stateTransitionService is null)
            throw new InvalidOperationException("TACIT_EXECUTION_DISABLED: Solo es posible aplicar vencimientos reales en UAT/local con el motor canonico disponible.");
        var shape = PrenotificationSimulationContract.Validate(request);
        if (!shape.ShapeValid || request.Outcome != PrenotificationSimulationOutcome.NoReturnByDeadline)
            throw new ArgumentException("TACIT_CONTRACT_INVALID: Solicitud de aceptacion tacita invalida.");
        if (request.OriginalPrenotificationIds.Count > _options.MaxEntriesPerSimulation)
            throw new InvalidOperationException("TACIT_BATCH_TOO_LARGE: Excede el maximo del simulador.");

        // SERIALIZABLE y la llave funcional por intento evitan aplicar el mismo resultado
        // dos veces desde el simulador. El job normal sigue siendo la fuente productiva.
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var ids = request.OriginalPrenotificationIds.ToArray();
        var originalRows = await _context.AchTransactions.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new
            {
                x.Id, x.State, x.IsPrenotification, x.AchCycleId, x.DestinationInstitutionId,
                ClearingHouseCode = x.AchCycle.ClearingHouse.Code,
                x.Direction, x.Origin,
                OriginDefault = x.SourceInstitution.IsDefaultSource,
                DestinationExternal = !x.DestinationInstitution.IsDefaultSource
            }).ToListAsync(ct);
        if (originalRows.Count != ids.Length || originalRows.Any(x =>
            !x.IsPrenotification || !x.OriginDefault || !x.DestinationExternal
            || x.Direction == AchTransactionDirection.Incoming || x.Origin == AchTransactionOrigin.ExternalInstitution
            || x.AchCycleId != request.OriginalCycleId
            || x.DestinationInstitutionId != request.RespondingFinancialInstitutionId
            || !x.ClearingHouseCode.Equals(request.ClearingHouseCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("TACIT_ORIGINAL_MISMATCH: Camara, banco, origen o ciclo original no coinciden.");
        var alreadyApplied = originalRows.Where(x => x.State == AchTransferStateEnum.AppliedTacitly)
            .Select(x => x.Id).ToHashSet();
        if (originalRows.Any(x => x.State != AchTransferStateEnum.Pending
                                  && x.State != AchTransferStateEnum.AppliedTacitly))
            throw new InvalidOperationException("TACIT_ORIGINAL_TERMINAL: La seleccion incluye estados incompatibles.");

        if (alreadyApplied.Count > 0 && alreadyApplied.Count != ids.Length)
            throw new InvalidOperationException("TACIT_MIXED_TERMINAL_STATES: No mezclar prenotificaciones ya aplicadas con pendientes.");
        var pendingIds = ids.Where(x => !alreadyApplied.Contains(x)).ToArray();
        var items = new List<PrenotificationTacitAcceptanceItem>();
        // apply-due nunca toma el reloj virtual/injectable: solo el reloj real del host.
        var now = DateTimeOffset.UtcNow;
        if (pendingIds.Length > 0)
        {
            // Vista previa por TODA la seleccion mientras se conserva la misma transaccion.
            // La elegibilidad se vuelve a calcular justo antes de la transicion.
            var preview = await PreviewTacitAcceptanceAsync(new()
            {
                Contract = request
            }, ct);
            if (preview.Violations.Count != 0 || preview.Items.Any(x =>
                    pendingIds.Contains(x.OriginalPrenotificationId)
                    && (!x.EligibleAtRealTime || !x.DeadlineAtUtc.HasValue || x.DeadlineAtUtc.Value > now)))
                throw new InvalidOperationException("TACIT_NOT_DUE_OR_INELIGIBLE: " +
                    string.Join(",", preview.Violations.Select(x => x.Code)
                        .Concat(preview.Items.Where(x => pendingIds.Contains(x.OriginalPrenotificationId))
                            .SelectMany(x => x.Violations.Select(v => v.Code))).Distinct()));
            foreach (var id in pendingIds)
            {
                var item = preview.Items.Single(x => x.OriginalPrenotificationId == id);
                var attempt = await _context.PrenotificationValidationAttempts.AsNoTracking()
                    .Where(x => x.AchTransactionId == id
                                && x.ThirdPartyAccountValidation.CurrentAttemptId == x.Id)
                    .Select(x => x.Id).SingleAsync(ct);
                var clearingHouseId = await _context.AchTransactions.AsNoTracking()
                    .Where(x => x.Id == id).Select(x => x.AchCycle.ClearingHouseId).SingleAsync(ct);
                var result = await _stateTransitionService.TransitionAsync(new AchStateTransitionRequest(
                    id, AchTransferStateEnum.AppliedTacitly, AchStateEventSourceEnum.System,
                    PayloadJson: JsonSerializer.Serialize(new
                    {
                        origin = "uat-prenotification-tacit-acceptance",
                        actor = string.IsNullOrWhiteSpace(actor) ? "uat-local" : actor,
                        deadlineAuthority = "clearing-house-policy",
                        realNowUtc = now,
                        originalPrenotificationId = id
                    }),
                    ChangedAtUtc: now.UtcDateTime,
                    IdempotencyKey: $"uat-prenotification-tacit:{id}:{attempt}",
                    ClearingHouseId: clearingHouseId), ct);
                items.Add(item with { Decision = result.Applied ? "Applied" : "AlreadyApplied" });
            }
        }
        foreach (var id in alreadyApplied)
            items.Add(new(id, null, false, false, "AlreadyApplied", []));
        await tx.CommitAsync(ct);
        return new(now, items.Count(x => x.Decision == "Applied"),
            items.Count(x => x.Decision == "AlreadyApplied"), items, false, false);
    }
}
