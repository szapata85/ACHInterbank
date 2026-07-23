using Cfa.ACHInterbank.Application.ACH.Responses.Operations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchResponseOperationsService : IAchResponseOperationsService
{
    private readonly AchDbContext _db;
    public AchResponseOperationsService(AchDbContext db) => _db = db;

    public async Task<IReadOnlyList<AchResponseMappingModel>> ListMappingsAsync(int? clearingHouseId, bool? active, CancellationToken ct = default)
    {
        var query = _db.AchResponseStatusMappings.AsNoTracking().Include(x => x.ClearingHouse).AsQueryable();
        if (clearingHouseId.HasValue) query = query.Where(x => x.ClearingHouseId == clearingHouseId.Value);
        if (active.HasValue) query = query.Where(x => x.Activo == active.Value);
        var rows = await query.OrderBy(x => x.ClearingHouse!.Code).ThenBy(x => x.TipoRespuesta)
            .ThenBy(x => x.CodigoEstadoExterno).ThenByDescending(x => x.Priority).ToListAsync(ct);
        return rows.Select(x => Map(x)).ToList();
    }

    public async Task<AchResponseMappingModel?> GetMappingAsync(int id, CancellationToken ct = default)
    {
        var row = await _db.AchResponseStatusMappings.AsNoTracking().Include(x => x.ClearingHouse)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? null : Map(row);
    }

    public async Task<AchResponseMappingModel> CreateMappingAsync(AchResponseMappingCommand command, string actor,
        string correlationId, CancellationToken ct = default)
    {
        Validate(command, actor, correlationId);
        var house = await GetHouse(command.ClearingHouseId, ct);
        var type = ParseType(command.ResponseType);
        await EnsureNoOverlap(null, command, type, ct);
        var now = DateTime.UtcNow;
        var row = new AchResponseStatusMapping
        {
            ClearingHouseId = house.Id, CodigoCamaraCompensacion = house.Code, TipoRespuesta = type,
            CodigoEstadoExterno = Normalize(command.ExternalCode), CodigoCausalExterna = NormalizeNullable(command.ExternalCause),
            IdEstadoInterno = command.InternalStatusId, IdEstadoServicioExterno = command.ExternalServiceStatusId,
            EstadoInternoNombre = command.InternalStatusName.Trim(), CausalNormalizada = NormalizeNullable(command.NormalizedCause),
            DescripcionCausalNormalizada = command.NormalizedDescription?.Trim(), RequiereCausal = command.RequiresCause,
            PermiteNotificacion = command.AllowsNotification, Priority = command.Priority, Activo = command.IsActive,
            FechaInicioVigencia = AsUtc(command.EffectiveFrom), FechaFinVigencia = command.EffectiveTo.HasValue ? AsUtc(command.EffectiveTo.Value) : null,
            FechaCreacion = now, Version = Guid.NewGuid()
        };
        _db.AchResponseStatusMappings.Add(row);
        await Save(ct);
        AddAudit(nameof(AchResponseStatusMapping), row.Id.ToString(), null, row.Activo ? "Active" : "Inactive", "MappingCreated",
            actor, command.Reason, correlationId, now, $"priority={row.Priority}");
        await Save(ct);
        return Map(row, house.Code);
    }

    public async Task<AchResponseMappingModel> UpdateMappingAsync(int id, AchResponseMappingCommand command,
        string actor, string correlationId, CancellationToken ct = default)
    {
        Validate(command, actor, correlationId);
        if (!command.ExpectedVersion.HasValue) throw new AchResponseConflictException("ExpectedVersion es obligatorio.");
        var row = await _db.AchResponseStatusMappings.Include(x => x.ClearingHouse).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AchResponseNotFoundException("Mapping no encontrado.");
        EnsureVersion(row.Version, command.ExpectedVersion.Value);
        var house = await GetHouse(command.ClearingHouseId, ct);
        var type = ParseType(command.ResponseType);
        await EnsureNoOverlap(id, command, type, ct);
        var previous = row.Activo ? "Active" : "Inactive";
        row.ClearingHouseId = house.Id;
        row.ClearingHouse = house;
        row.CodigoCamaraCompensacion = house.Code;
        row.TipoRespuesta = type;
        row.CodigoEstadoExterno = Normalize(command.ExternalCode);
        row.CodigoCausalExterna = NormalizeNullable(command.ExternalCause);
        row.IdEstadoInterno = command.InternalStatusId;
        row.IdEstadoServicioExterno = command.ExternalServiceStatusId;
        row.EstadoInternoNombre = command.InternalStatusName.Trim();
        row.CausalNormalizada = NormalizeNullable(command.NormalizedCause);
        row.DescripcionCausalNormalizada = command.NormalizedDescription?.Trim();
        row.RequiereCausal = command.RequiresCause;
        row.PermiteNotificacion = command.AllowsNotification;
        row.Priority = command.Priority;
        row.Activo = command.IsActive;
        row.FechaInicioVigencia = AsUtc(command.EffectiveFrom);
        row.FechaFinVigencia = command.EffectiveTo.HasValue ? AsUtc(command.EffectiveTo.Value) : null;
        row.FechaActualizacion = DateTime.UtcNow;
        AddAudit(nameof(AchResponseStatusMapping), id.ToString(), previous, row.Activo ? "Active" : "Inactive",
            "MappingUpdated", actor, command.Reason, correlationId, DateTime.UtcNow, $"priority={row.Priority}");
        await Save(ct);
        return Map(row, house.Code);
    }

    public async Task<AchResponseMappingModel> SetMappingActiveAsync(int id, bool active, Guid expectedVersion,
        string reason, string actor, string correlationId, CancellationToken ct = default)
    {
        RequireContext(actor, reason, correlationId);
        var row = await _db.AchResponseStatusMappings.Include(x => x.ClearingHouse).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AchResponseNotFoundException("Mapping no encontrado.");
        EnsureVersion(row.Version, expectedVersion);
        if (row.Activo == active) return Map(row);
        if (active)
        {
            var command = ToCommand(row, expectedVersion, reason, true);
            await EnsureNoOverlap(id, command, row.TipoRespuesta, ct);
        }
        var previous = row.Activo ? "Active" : "Inactive";
        row.Activo = active;
        row.FechaActualizacion = DateTime.UtcNow;
        AddAudit(nameof(AchResponseStatusMapping), id.ToString(), previous, active ? "Active" : "Inactive",
            active ? "MappingActivated" : "MappingDeactivated", actor, reason, correlationId, DateTime.UtcNow, null);
        await Save(ct);
        return Map(row);
    }

    public async Task<IReadOnlyList<AchResponseAuditModel>> GetAuditAsync(string entityType, string entityId, CancellationToken ct = default)
        => await _db.AchResponseAudits.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new AchResponseAuditModel(x.Id, x.EntityType, x.EntityId, x.Action, x.PreviousState,
                x.NewState, x.Actor, x.Reason, x.CorrelationId, x.OccurredAtUtc, x.SanitizedMetadata))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AchResponseOrphanModel>> ListOrphansAsync(int? clearingHouseId, string? status, CancellationToken ct = default)
    {
        var query = _db.AchResponseOrphans.AsNoTracking().AsQueryable();
        if (clearingHouseId.HasValue) query = query.Where(x => x.ClearingHouseId == clearingHouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.ResolutionStatus == status.Trim());
        var rows = await query.OrderByDescending(x => x.ReceivedAtUtc).Take(500).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<AchResponseOrphanModel> CreateOrphanAsync(Guid responseId, string reason, string? candidates,
        string actor, string correlationId, CancellationToken ct = default)
    {
        RequireContext(actor, reason, correlationId);
        var existing = await _db.AchResponseOrphans.AsNoTracking().SingleOrDefaultAsync(x => x.AchResponseId == responseId, ct);
        if (existing is not null) return Map(existing);
        var response = await _db.AchResponses.SingleOrDefaultAsync(x => x.Id == responseId, ct)
            ?? throw new AchResponseNotFoundException("Respuesta no encontrada.");
        Transition(response, AchResponseProcessingStatus.Huerfana, actor, reason, correlationId, "OrphanCreated");
        var orphan = new AchResponseOrphan
        {
            Id = Guid.NewGuid(), AchResponseId = response.Id, ClearingHouseId = response.ClearingHouseId
                ?? throw new AchResponseConflictException("La respuesta legacy no tiene cámara canónica asociada."),
            ResponseType = response.TipoRespuesta.ToString(), ExternalIdentifiers = $"transaction:{response.IdTransaccion}",
            ExternalCode = response.CodigoEstadoExterno, ReceivedAtUtc = response.FechaRecepcion,
            OperationalDate = response.OperationalDate, CanonicalPayloadHash = response.CanonicalPayloadHash,
            CorrelationId = correlationId, OrphanReason = reason, CandidateReferences = candidates,
            ResolutionStatus = "Pending", Version = Guid.NewGuid()
        };
        _db.AchResponseOrphans.Add(orphan);
        var reconciliation = new AchResponseReconciliationCase
        {
            Id = Guid.NewGuid(), ClearingHouseId = orphan.ClearingHouseId, AchResponseId = response.Id,
            ExceptionType = "ResponseWithoutTransaction", Status = "Open",
            Reference = response.IdTransaccion, Details = "Respuesta sin correlación funcional inequívoca.",
            DetectedAtUtc = DateTime.UtcNow, CorrelationId = correlationId, Version = Guid.NewGuid()
        };
        _db.AchResponseReconciliationCases.Add(reconciliation);
        AddAudit(nameof(AchResponseReconciliationCase), reconciliation.Id.ToString(), null, "Open",
            "ReconciliationDetected", actor, reason, correlationId, reconciliation.DetectedAtUtc,
            "exception=ResponseWithoutTransaction", response.Id);
        await Save(ct);
        return Map(orphan);
    }

    public async Task<AchResponseOrphanModel> BeginReviewAsync(Guid orphanId, Guid expectedVersion, string reason,
        string actor, string correlationId, CancellationToken ct = default)
    {
        RequireContext(actor, reason, correlationId);
        var orphan = await _db.AchResponseOrphans.Include(x => x.AchResponse).SingleOrDefaultAsync(x => x.Id == orphanId, ct)
            ?? throw new AchResponseNotFoundException("Respuesta huérfana no encontrada.");
        EnsureVersion(orphan.Version, expectedVersion);
        if (orphan.ResolutionStatus == "InReview") return Map(orphan);
        if (orphan.ResolutionStatus != "Pending") throw new AchResponseConflictException("La huérfana ya fue resuelta.", orphan.Version);
        orphan.ResolutionStatus = "InReview";
        Transition(orphan.AchResponse, AchResponseProcessingStatus.EnRevision, actor, reason, correlationId, "ManualReviewStarted");
        AddAudit(nameof(AchResponseOrphan), orphan.Id.ToString(), "Pending", "InReview", "ManualReviewStarted",
            actor, reason, correlationId, DateTime.UtcNow, null);
        await Save(ct);
        return Map(orphan);
    }

    public async Task<AchResponseOrphanModel> ResolveOrphanAsync(Guid orphanId, ManualResolutionCommand command,
        string actor, CancellationToken ct = default)
    {
        RequireContext(actor, command.Reason, command.CorrelationId);
        var orphan = await _db.AchResponseOrphans.Include(x => x.AchResponse).SingleOrDefaultAsync(x => x.Id == orphanId, ct)
            ?? throw new AchResponseNotFoundException("Respuesta huérfana no encontrada.");
        EnsureVersion(orphan.Version, command.ExpectedVersion);
        if (orphan.ResolutionStatus is "Resolved" or "Rejected") return Map(orphan);
        if (orphan.ResolutionStatus != "InReview") throw new AchResponseConflictException("Debe iniciar la revisión antes de resolver.", orphan.Version);

        if (!command.Reject)
        {
            if (string.IsNullOrWhiteSpace(command.FunctionalReference))
                throw new AchResponseOperationException("La referencia funcional es obligatoria para asociar.");
            var reference = command.FunctionalReference.Trim();
            var candidates = await _db.AchTransactions.AsNoTracking()
                .Where(x => x.TransactionExternalId == reference && x.AchCycle.ClearingHouseId == orphan.ClearingHouseId)
                .Select(x => x.TransactionExternalId).Take(2).ToListAsync(ct);
            if (candidates.Count != 1)
                throw new AchResponseConflictException(candidates.Count == 0
                    ? "No existe una transacción inequívoca en la misma cámara."
                    : "La referencia corresponde a más de una transacción.", orphan.Version);
            orphan.ResolvedReference = reference;
        }

        var now = DateTime.UtcNow;
        orphan.ResolutionStatus = command.Reject ? "Rejected" : "Resolved";
        orphan.ResolvedAtUtc = now;
        orphan.ResolvedBy = actor;
        orphan.ResolutionReason = command.Reason;
        Transition(orphan.AchResponse, command.Reject ? AchResponseProcessingStatus.Rechazada : AchResponseProcessingStatus.Resuelta,
            actor, command.Reason, command.CorrelationId, command.Reject ? "ManualReviewRejected" : "ManualAssociationResolved");
        AddAudit(nameof(AchResponseOrphan), orphan.Id.ToString(), "InReview", orphan.ResolutionStatus,
            "ManualReviewResolved", actor, command.Reason, command.CorrelationId, now,
            orphan.ResolvedReference is null ? null : "association=exact");
        await Save(ct);
        return Map(orphan);
    }

    public async Task<AchResponseReprocessModel> RequestReprocessAsync(Guid responseId, ReprocessCommand command,
        string actor, CancellationToken ct = default)
    {
        RequireContext(actor, command.Reason, command.CorrelationId);
        var sameCommand = await _db.AchResponseReprocessAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.CommandId == command.CommandId, ct);
        if (sameCommand is not null) return Map(sameCommand);
        var response = await _db.AchResponses.SingleOrDefaultAsync(x => x.Id == responseId, ct)
            ?? throw new AchResponseNotFoundException("Respuesta no encontrada.");
        EnsureVersion(response.Version, command.ExpectedVersion);
        if (await _db.AchResponseReprocessAttempts.AnyAsync(x => x.AchResponseId == responseId && (x.Status == "Pending" || x.Status == "Running"), ct))
            throw new AchResponseConflictException("Ya existe un reproceso activo.", response.Version);
        Transition(response, AchResponseProcessingStatus.PendienteReproceso, actor, command.Reason, command.CorrelationId, "ReprocessRequested");
        var attempt = new AchResponseReprocessAttempt
        {
            AchResponseId = responseId,
            AttemptNumber = await _db.AchResponseReprocessAttempts.CountAsync(x => x.AchResponseId == responseId, ct) + 1,
            Status = "Pending", RequestedBy = actor, Reason = command.Reason, CorrelationId = command.CorrelationId,
            RequestedAtUtc = DateTime.UtcNow, CommandId = command.CommandId, Version = Guid.NewGuid()
        };
        _db.AchResponseReprocessAttempts.Add(attempt);
        await Save(ct);
        return Map(attempt);
    }

    public async Task<IReadOnlyList<AchResponseReprocessModel>> ListReprocessAttemptsAsync(Guid responseId, CancellationToken ct = default)
        => (await _db.AchResponseReprocessAttempts.AsNoTracking().Where(x => x.AchResponseId == responseId)
            .OrderByDescending(x => x.AttemptNumber).ToListAsync(ct)).Select(Map).ToList();

    public async Task<AchResponseReprocessModel?> GetReprocessAttemptAsync(Guid responseId, long attemptId, CancellationToken ct = default)
    {
        var item = await _db.AchResponseReprocessAttempts.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AchResponseId == responseId && x.Id == attemptId, ct);
        return item is null ? null : Map(item);
    }

    public async Task<IReadOnlyList<AchResponseReconciliationCaseModel>> ListReconciliationCasesAsync(
        int? clearingHouseId, string? status, CancellationToken ct = default)
    {
        var query = _db.AchResponseReconciliationCases.AsNoTracking().AsQueryable();
        if (clearingHouseId.HasValue) query = query.Where(x => x.ClearingHouseId == clearingHouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim());
        var rows = await query.OrderByDescending(x => x.DetectedAtUtc).Take(500).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<AchResponseReconciliationCaseModel> ResolveReconciliationCaseAsync(Guid id,
        ReconciliationResolutionCommand command, string actor, CancellationToken ct = default)
    {
        RequireContext(actor, command.Reason, command.CorrelationId);
        var row = await _db.AchResponseReconciliationCases.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AchResponseNotFoundException("Excepción de conciliación no encontrada.");
        EnsureVersion(row.Version, command.ExpectedVersion);
        if (row.Status == "Resolved") return Map(row);
        var previous = row.Status;
        row.Status = "Resolved";
        row.Resolution = command.Resolution.Trim();
        row.ResolutionReason = command.Reason;
        row.ResolvedBy = actor;
        row.ResolvedAtUtc = DateTime.UtcNow;
        row.CorrelationId = command.CorrelationId;
        AddAudit(nameof(AchResponseReconciliationCase), row.Id.ToString(), previous, row.Status, "ReconciliationResolved",
            actor, command.Reason, command.CorrelationId, row.ResolvedAtUtc.Value, $"resolution={row.Resolution}");
        await Save(ct);
        return Map(row);
    }

    private async Task EnsureNoOverlap(int? excludedId, AchResponseMappingCommand command, TipoRespuestaAch type, CancellationToken ct)
    {
        if (!command.IsActive) return;
        var start = AsUtc(command.EffectiveFrom);
        var end = command.EffectiveTo.HasValue ? AsUtc(command.EffectiveTo.Value) : DateTime.MaxValue;
        var code = Normalize(command.ExternalCode);
        var cause = NormalizeNullable(command.ExternalCause);
        var overlap = await _db.AchResponseStatusMappings.AsNoTracking().AnyAsync(x =>
            (!excludedId.HasValue || x.Id != excludedId.Value) && x.Activo && x.ClearingHouseId == command.ClearingHouseId &&
            x.TipoRespuesta == type && x.CodigoEstadoExterno == code && x.CodigoCausalExterna == cause &&
            x.Priority == command.Priority && x.FechaInicioVigencia <= end &&
            (!x.FechaFinVigencia.HasValue || x.FechaFinVigencia.Value >= start), ct);
        if (overlap) throw new AchResponseConflictException("Existe un mapping activo con vigencia superpuesta para la misma clave y prioridad.");
    }

    private static void Validate(AchResponseMappingCommand command, string actor, string correlationId)
    {
        RequireContext(actor, command.Reason, correlationId);
        if (command.ClearingHouseId <= 0) throw new AchResponseOperationException("ClearingHouseId es obligatorio.");
        if (string.IsNullOrWhiteSpace(command.ExternalCode)) throw new AchResponseOperationException("ExternalCode es obligatorio.");
        if (string.IsNullOrWhiteSpace(command.InternalStatusName)) throw new AchResponseOperationException("InternalStatusName es obligatorio.");
        if (command.EffectiveTo.HasValue && command.EffectiveTo.Value < command.EffectiveFrom)
            throw new AchResponseOperationException("EffectiveTo no puede ser anterior a EffectiveFrom.");
    }

    private static void RequireContext(string actor, string reason, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new AchResponseOperationException("El actor es obligatorio.");
        if (string.IsNullOrWhiteSpace(reason)) throw new AchResponseOperationException("El motivo es obligatorio.");
        if (string.IsNullOrWhiteSpace(correlationId)) throw new AchResponseOperationException("El correlation ID es obligatorio.");
    }

    private async Task<ClearingHouse> GetHouse(int id, CancellationToken ct)
        => await _db.ClearingHouses.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct)
           ?? throw new AchResponseOperationException("La cámara compensadora no existe o está inactiva.");

    private static TipoRespuestaAch ParseType(string value)
        => Enum.TryParse<TipoRespuestaAch>(value?.Trim(), true, out var parsed)
            ? parsed : throw new AchResponseOperationException("ResponseType inválido.");

    private static void EnsureVersion(Guid current, Guid expected)
    {
        if (current != expected) throw new AchResponseConflictException("La entidad fue modificada por otro usuario.", current);
    }

    private void Transition(AchResponse response, AchResponseProcessingStatus target, string actor,
        string reason, string correlationId, string action)
    {
        var previous = response.EstadoProcesamiento;
        AchResponseStatePolicy.EnsureTransition(previous, target, actor, reason, correlationId);
        if (previous == target) return;
        response.EstadoProcesamiento = target;
        response.FechaActualizacion = DateTime.UtcNow;
        AddAudit(nameof(AchResponse), response.Id.ToString(), previous.ToString(), target.ToString(), action,
            actor, reason, correlationId, DateTime.UtcNow, null, response.Id);
    }

    private void AddAudit(string entityType, string entityId, string? previous, string? next, string action,
        string actor, string reason, string correlationId, DateTime at, string? metadata, Guid? responseId = null)
        => _db.AchResponseAudits.Add(new AchResponseAudit
        {
            EntityType = entityType, EntityId = entityId, AchResponseId = responseId, Action = action,
            PreviousState = previous, NewState = next, Actor = actor, Reason = reason,
            CorrelationId = correlationId, OccurredAtUtc = at, SanitizedMetadata = metadata
        });

    private async Task Save(CancellationToken ct)
    {
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new AchResponseConflictException("Conflicto de concurrencia; recargue la versión vigente."); }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                                           || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
        { throw new AchResponseConflictException("La operación compite con otra solicitud idempotente."); }
    }

    private static AchResponseMappingCommand ToCommand(AchResponseStatusMapping x, Guid version, string reason, bool active)
        => new(x.ClearingHouseId ?? 0, x.TipoRespuesta.ToString(), x.CodigoEstadoExterno, x.CodigoCausalExterna,
            x.IdEstadoInterno, x.IdEstadoServicioExterno, x.EstadoInternoNombre, x.CausalNormalizada,
            x.DescripcionCausalNormalizada, x.RequiereCausal, x.PermiteNotificacion, x.Priority,
            x.FechaInicioVigencia, x.FechaFinVigencia, active, version, reason);

    private static AchResponseMappingModel Map(AchResponseStatusMapping x, string? code = null)
        => new(x.Id, x.ClearingHouseId ?? 0, code ?? x.ClearingHouse!.Code, x.TipoRespuesta.ToString(), x.CodigoEstadoExterno,
            x.CodigoCausalExterna, x.IdEstadoInterno, x.IdEstadoServicioExterno, x.EstadoInternoNombre,
            x.CausalNormalizada, x.DescripcionCausalNormalizada, x.RequiereCausal, x.PermiteNotificacion,
            x.Priority, x.FechaInicioVigencia, x.FechaFinVigencia, x.Activo, x.Version);

    private static AchResponseOrphanModel Map(AchResponseOrphan x)
        => new(x.Id, x.AchResponseId, x.ClearingHouseId, x.ResponseType, x.ExternalIdentifiers, x.ExternalCode,
            x.ReceivedAtUtc, x.OperationalDate, x.CorrelationId, x.OrphanReason, x.CandidateReferences,
            x.ResolutionStatus, x.ResolvedReference, x.ResolvedAtUtc, x.Version);

    private static AchResponseReprocessModel Map(AchResponseReprocessAttempt x)
        => new(x.Id, x.AchResponseId, x.AttemptNumber, x.Status, x.RequestedBy, x.Reason, x.CorrelationId,
            x.RequestedAtUtc, x.CompletedAtUtc, x.Result, x.CommandId, x.ClaimedBy, x.StartedAtUtc,
            x.ResultCode, x.ErrorDetailSanitized);

    private static AchResponseReconciliationCaseModel Map(AchResponseReconciliationCase x)
        => new(x.Id, x.ClearingHouseId, x.AchResponseId, x.ExceptionType, x.Status, x.Reference, x.Details,
            x.DetectedAtUtc, x.Resolution, x.ResolutionReason, x.ResolvedBy, x.ResolvedAtUtc, x.CorrelationId, x.Version);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static DateTime AsUtc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
