using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I02: consulta y vista previa sin efectos. El motor de generación I03 debe
/// volver a validar la elegibilidad en la misma transacción que reserve el intento.
/// </summary>
public sealed partial class NachaInboundSimulationService
{
    public async Task<PrenotificationCandidatePage> ListOriginatedPrenotificationsAsync(
        PrenotificationCandidateQuery request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        if (request.FromDate > request.ToDate)
            throw new ArgumentException("DATE_RANGE_INVALID: La fecha inicial no puede superar la final.");
        if (string.IsNullOrWhiteSpace(request.ClearingHouseCode))
            throw new ArgumentException("CLEARING_HOUSE_REQUIRED: Debe seleccionar una camara.");
        if (!_options.IsUatLike())
            return new PrenotificationCandidatePage([], page, pageSize, 0, false);
        var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);
        if (!_options.AllowedClearingHouses.Any(x => MatchesCode(clearingHouse, x)))
            return new PrenotificationCandidatePage([], page, pageSize, 0, false);

        // El filtro estructural evita mostrar operaciones de otra camara o banco de origen.
        // No se fuerza estado pending aqui: las excluidas deben mostrarse con causal.
        var query = _context.AchTransactions.AsNoTracking()
            .Where(x => x.IsPrenotification
                && x.Type == TransactionTypeEnum.Prenotification
                && x.SourceInstitution.IsDefaultSource
                && !x.DestinationInstitution.IsDefaultSource
                && x.AchCycle.ClearingHouseId == clearingHouse.Id);
        if (request.RespondingFinancialInstitutionId.HasValue)
            query = query.Where(x => x.DestinationInstitutionId == request.RespondingFinancialInstitutionId.Value);
        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.AchCycle.ProcessingDate >= from);
        }
        if (request.ToDate.HasValue)
        {
            var to = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.AchCycle.ProcessingDate < to);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.TraceNumber.Contains(search)
                || x.TransactionExternalId.Contains(search) || x.Reference.Contains(search));
        }
        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(x => x.AchCycle.ProcessingDate).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
            .Include(x => x.SourceInstitution)
            .Include(x => x.DestinationInstitution)
            .ToListAsync(ct);
        var items = await EvaluatePrenotificationsAsync(rows, clearingHouse.Code, ct);
        return new PrenotificationCandidatePage(items, page, pageSize, total, false);
    }

    public async Task<PrenotificationBusinessEligibilityPreviewResponse> PreviewPrenotificationAsync(
        PrenotificationSimulationContractRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var shape = PrenotificationSimulationContract.Validate(request);
        var violations = shape.Violations.ToList();
        if (!shape.ShapeValid)
            return new(false, false, false, PrenotificationSimulationContract.Version, violations, []);
        if (!_options.IsUatLike() || !_options.DifferentialResponsesEnabled)
        {
            violations.Add(new("SIMULATOR_UNAVAILABLE", "Outcome", "El simulador de respuestas diferenciales no esta habilitado con guardas seguras."));
            return new(true, false, false, PrenotificationSimulationContract.Version, violations, []);
        }

        var normalizedHouseCode = request.ClearingHouseCode.Trim().ToUpperInvariant();
        var house = await _context.ClearingHouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalizedHouseCode, ct);
        if (house is null || !_options.AllowedClearingHouses.Any(x => MatchesCode(house, x)))
        {
            violations.Add(new("CLEARING_HOUSE_NOT_FOUND", "ClearingHouseCode", "La camara no existe o no esta habilitada."));
            return new(true, false, false, PrenotificationSimulationContract.Version, violations, []);
        }
        var originalCycle = await _context.AchCycles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.OriginalCycleId && x.ClearingHouseId == house.Id, ct);
        if (originalCycle is null)
            violations.Add(new("ORIGINAL_CYCLE_NOT_FOUND", "OriginalCycleId", "Ciclo original inexistente o de otra camara."));

        if (request.Outcome == PrenotificationSimulationOutcome.Returned)
        {
            var responseCycle = await _context.AchCycles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ResponseCycleId && x.ClearingHouseId == house.Id, ct);
            if (responseCycle is null || DateOnly.FromDateTime(responseCycle.ProcessingDate) != request.BusinessDate
                || responseCycle.OperationalStatus == AchCycleOperationalStatus.Cancelled)
                violations.Add(new("RESPONSE_CYCLE_INVALID", "ResponseCycleId", "Ciclo de devolucion inexistente, cancelado, de otra camara o fecha diferente."));
            if (responseCycle?.Id == originalCycle?.Id)
                violations.Add(new("RESPONSE_CYCLE_EQUALS_ORIGINAL", "ResponseCycleId", "No se admite el mismo ciclo como devolucion de la prenotificacion."));
            if (responseCycle is not null && originalCycle is not null
                && (responseCycle.ProcessingDate < originalCycle.ProcessingDate
                    || (responseCycle.ProcessingDate == originalCycle.ProcessingDate
                        && responseCycle.StartTime <= originalCycle.StartTime)))
                violations.Add(new("RESPONSE_CYCLE_BEFORE_ORIGINAL", "ResponseCycleId", "El ciclo de respuesta debe ser posterior al de origen."));
            // CENIT exige que la devolucion suceda en la misma Fecha Valor.
            if (house.Code.Equals("CENIT", StringComparison.OrdinalIgnoreCase)
                && originalCycle is not null
                && DateOnly.FromDateTime(originalCycle.ProcessingDate) != request.BusinessDate)
                violations.Add(new("CENIT_RETURN_OUTSIDE_VALUE_DATE", "BusinessDate", "CENIT requiere devolucion en la Fecha Valor de la prenotificacion."));
        }

        var ids = request.OriginalPrenotificationIds.ToArray();
        var rows = await _context.AchTransactions.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Include(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
            .Include(x => x.SourceInstitution)
            .Include(x => x.DestinationInstitution)
            .ToListAsync(ct);
        var candidates = await EvaluatePrenotificationsAsync(rows, house.Code, ct);
        foreach (var id in ids.Where(id => rows.All(x => x.Id != id)))
            violations.Add(new("PRENOTIFICATION_NOT_FOUND", "OriginalPrenotificationIds", $"No se encontro prenotificacion {id}."));
        foreach (var candidate in candidates)
        {
            if (!candidate.ClearingHouseCode.Equals(house.Code, StringComparison.OrdinalIgnoreCase))
                violations.Add(new("PRENOTIFICATION_WRONG_CLEARING_HOUSE", "OriginalPrenotificationIds", $"La prenotificacion {candidate.OriginalPrenotificationId} pertenece a otra camara."));
            if (candidate.OriginalCycleId != request.OriginalCycleId)
                violations.Add(new("PRENOTIFICATION_ORIGINAL_CYCLE_MISMATCH", "OriginalCycleId", $"La prenotificacion {candidate.OriginalPrenotificationId} corresponde a otro ciclo."));
            if (candidate.RespondingFinancialInstitutionId != request.RespondingFinancialInstitutionId)
                violations.Add(new("PRENOTIFICATION_WRONG_RESPONDER", "RespondingFinancialInstitutionId", $"La prenotificacion {candidate.OriginalPrenotificationId} corresponde a otro banco."));
            violations.AddRange(candidate.Violations);
            // Aplicar la restriccion temporal del resultado seleccionado, no la del contrario.
            var deadline = candidate.ResponseDeadlineAtUtc;
            if (deadline is not null && request.Outcome == PrenotificationSimulationOutcome.Returned
                && _timeProvider.GetUtcNow() >= deadline.Value)
                violations.Add(new("RETURN_DEADLINE_EXPIRED", "BusinessDate", $"Plazo vencido para {candidate.OriginalPrenotificationId}."));
            if (deadline is not null && request.Outcome == PrenotificationSimulationOutcome.NoReturnByDeadline
                && _timeProvider.GetUtcNow() < deadline.Value)
                violations.Add(new("PRENOTIFICATION_DEADLINE_NOT_REACHED", "BusinessDate", $"Plazo no vencido para {candidate.OriginalPrenotificationId}."));
        }
        // Prevencion de falsas reservas: solo verificamos ahora; no se guarda la clave
        // hasta I03 (clave unica transaccional y reevaluacion atomica de elegibilidad).
        return new(true, violations.Count == 0, false,
            PrenotificationSimulationContract.Version, violations, candidates);
    }

    private async Task<IReadOnlyList<PrenotificationCandidateDto>> EvaluatePrenotificationsAsync(
        IReadOnlyList<AchTransaction> rows, string requestedClearingHouse, CancellationToken ct)
    {
        if (rows.Count == 0) return [];
        var ids = rows.Select(x => x.Id).ToArray();
        var attempts = await _context.PrenotificationValidationAttempts.AsNoTracking()
            .Include(x => x.ThirdPartyAccountValidation)
            .Where(x => x.AchTransactionId.HasValue && ids.Contains(x.AchTransactionId.Value))
            .ToListAsync(ct);
        var exported = await _context.AchFileExportTransactions.AsNoTracking()
            .Where(x => ids.Contains(x.AchTransactionId)
                && (x.AchFileExport.LifecycleStatus == AchFileExportLifecycleStatus.Transmitted
                    || x.AchFileExport.LifecycleStatus == AchFileExportLifecycleStatus.Acknowledged
                    || x.AchFileExport.LifecycleStatus == AchFileExportLifecycleStatus.Accepted)
                && x.AchFileExport.ClearingHouseId == x.AchTransaction.AchCycle.ClearingHouseId
                && x.AchFileExport.AchCycleId == x.AchTransaction.AchCycleId
                && x.AchCycleId == x.AchTransaction.AchCycleId
                && x.TraceNumber == x.AchTransaction.TraceNumber)
            .Select(x => x.AchTransactionId).Distinct().ToListAsync(ct);
        var sentIds = exported.ToHashSet();
        var correlations = rows.SelectMany(x => new[] { x.TransactionExternalId, x.Reference, x.TraceNumber })
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        var replies = await _context.AchResponses.AsNoTracking()
            .Where(x => (x.AchTransactionId.HasValue && ids.Contains(x.AchTransactionId.Value))
                || correlations.Contains(x.IdTransaccion))
            .Select(x => new { x.AchTransactionId, x.IdTransaccion }).ToListAsync(ct);
        var now = _timeProvider.GetUtcNow();
        return rows.Select(x =>
        {
            var linked = attempts.Where(a => a.AchTransactionId == x.Id)
                .OrderByDescending(a => a.AttemptNumber).ThenByDescending(a => a.Id).ToArray();
            var active = linked.Where(a => a.Status == PrenotificationValidationAttemptStatus.AwaitingResponse
                && a.Resolution == PrenotificationValidationResolution.None).ToArray();
            var attempt = linked.FirstOrDefault();
            var hasPrior = replies.Any(r => r.AchTransactionId == x.Id
                || r.IdTransaccion == x.TraceNumber || r.IdTransaccion == x.TransactionExternalId
                || r.IdTransaccion == x.Reference);
            var evidence = new PrenotificationSimulationEligibilityPolicy.Evidence(
                x.IsPrenotification && x.Type == TransactionTypeEnum.Prenotification,
                x.SourceInstitution?.IsDefaultSource == true,
                x.DestinationInstitution?.IsDefaultSource == false,
                x.Direction, x.Origin, x.State,
                x.TraceNumber, x.TransactionExternalId, x.AchCycleId,
                x.AchCycle.ClearingHouse?.Code ?? string.Empty,
                x.DestinationInstitutionId, sentIds.Contains(x.Id), active.Length,
                attempt is not null && attempt.ThirdPartyAccountValidation?.CurrentAttemptId == attempt.Id
                    && attempt.SourceCycleId == x.AchCycleId
                    && attempt.ThirdPartyAccountValidation.ClearingHouseId == x.AchCycle.ClearingHouseId,
                attempt?.Status,
                attempt?.ThirdPartyAccountValidation?.Status,
                attempt?.Resolution, attempt?.ResponseDeadlineAtUtc,
                hasPrior);
            var common = PrenotificationSimulationEligibilityPolicy.CheckCommon(evidence);
            var forReturn = PrenotificationSimulationEligibilityPolicy.CheckOutcome(evidence,
                PrenotificationSimulationOutcome.Returned, now);
            var forNoReturn = PrenotificationSimulationEligibilityPolicy.CheckOutcome(evidence,
                PrenotificationSimulationOutcome.NoReturnByDeadline, now);
            return new PrenotificationCandidateDto(
                x.Id, x.AchCycleId, x.AchCycle.ClearingHouse?.Code ?? string.Empty,
                x.DestinationInstitutionId, x.DestinationInstitution?.Name ?? "",
                x.TraceNumber, x.TransactionExternalId, MaskAccount(x.DestinationAccountNumber),
                DateOnly.FromDateTime(x.AchCycle.ProcessingDate), x.State.ToString(),
                attempt?.Id, attempt?.Status.ToString(), attempt?.ResponseDeadlineAtUtc,
                sentIds.Contains(x.Id), hasPrior, forReturn.Count == 0, forNoReturn.Count == 0, common);
        }).ToArray();
    }
}
