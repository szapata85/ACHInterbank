using Cfa.ACHInterbank.Application.ACH.Claims.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Claims.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Claims;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ClaimCaseService : IClaimCaseService
{
    private const int DevBusinessDayDeadline = 60;
    private const int MaxPageSize = 100;

    private readonly AchDbContext _context;
    private readonly IOperationalTimeSnapshotProvider _operationalTime;
    private readonly IOperationalCalendarService _calendar;

    public ClaimCaseService(
        AchDbContext context,
        IOperationalTimeSnapshotProvider operationalTime,
        IOperationalCalendarService calendar)
    {
        _context = context;
        _operationalTime = operationalTime;
        _calendar = calendar;
    }

    public async Task<ClaimCaseDetailDto> CreateDev14Async(
        CreateDev14ClaimCaseRequest request,
        string? idempotencyKey,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transactionIds = NormalizeTransactionIds(request.TransactionIds);
        var normalizedExternalReference = NormalizeOptional(request.ExternalReference, 128, "ExternalReference");
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);

        if (normalizedIdempotencyKey is not null)
        {
            var existing = await LoadByCreationIdempotencyKeyAsync(normalizedIdempotencyKey, tracking: false, ct);
            if (existing is not null)
            {
                EnsureSameCreateRequest(existing, transactionIds, normalizedExternalReference);
                return ToDetail(existing);
            }
        }

        var transactions = await _context.AchTransactions
            .Include(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.SourceInstitution)
            .Include(x => x.DestinationInstitution)
            .Where(x => transactionIds.Contains(x.Id))
            .ToListAsync(ct);

        var byId = transactions.ToDictionary(x => x.Id);
        var missing = transactionIds.Where(id => !byId.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
        {
            throw new KeyNotFoundException($"No existen las transacciones ACH: {string.Join(", ", missing)}.");
        }

        var orderedTransactions = transactionIds.Select(id => byId[id]).ToArray();
        var first = orderedTransactions[0];
        var clearingHouse = first.AchCycle?.ClearingHouse
            ?? throw new KeyNotFoundException($"No existe evidencia de cámara para la transacción ACH {first.Id}.");
        var requestingParticipant = first.DestinationInstitution
            ?? throw new KeyNotFoundException($"No existe el participante receptor/destino de la transacción ACH {first.Id}.");
        var respondingParticipant = first.SourceInstitution
            ?? throw new KeyNotFoundException($"No existe el participante originador/origen de la transacción ACH {first.Id}.");

        var snapshot = _operationalTime.CaptureNow();
        var filedAt = ToOperationalTimestamp(snapshot);
        var deadlineDate = await _calendar.ShiftBusinessDaysAsync(
            snapshot.OperationalDate,
            DevBusinessDayDeadline,
            clearingHouse.Id,
            ct);

        // Convención 3C: el día hábil 60 conserva la misma hora local de Bogotá de la radicación.
        // No se inventa una hora de cierre de ACH Colombia que no esté definida por una autoridad del sistema.
        var deadlineLocal = deadlineDate.ToDateTime(TimeOnly.FromDateTime(snapshot.BogotaTimestamp));
        var deadlineAt = new DateTimeOffset(deadlineLocal, filedAt.Offset);

        var claimCase = ClaimCase.Create(
            clearingHouse,
            ClaimCauseCode.DEV14,
            requestingParticipant,
            respondingParticipant,
            filedAt,
            normalizedExternalReference,
            normalizedIdempotencyKey,
            deadlineAt);

        foreach (var transaction in orderedTransactions)
        {
            // ClaimCase conserva la autoridad sobre los invariantes DEV14 por transacción.
            claimCase.AddTransaction(transaction);
        }

        _context.ClaimCases.Add(claimCase);
        try
        {
            await _context.SaveChangesAsync(ct);
            return ToDetail(claimCase);
        }
        catch (DbUpdateException ex) when (normalizedIdempotencyKey is not null
                                           && RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
        {
            _context.ChangeTracker.Clear();
            var existing = await LoadByCreationIdempotencyKeyAsync(normalizedIdempotencyKey, tracking: false, ct);
            if (existing is null)
            {
                throw;
            }

            EnsureSameCreateRequest(existing, transactionIds, normalizedExternalReference);
            return ToDetail(existing);
        }
    }

    public async Task<ClaimCaseDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (id <= 0)
        {
            return null;
        }

        var claimCase = await DetailQuery(tracking: false)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return claimCase is null ? null : ToDetail(claimCase);
    }

    public async Task<ClaimCaseQueryResult> QueryAsync(ClaimCaseQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidateQuery(query);

        var cases = _context.ClaimCases
            .AsNoTracking()
            .AsQueryable();

        if (query.Status.HasValue)
        {
            cases = cases.Where(x => x.Status == query.Status.Value);
        }

        if (query.CauseCode.HasValue)
        {
            cases = cases.Where(x => x.CauseCode == query.CauseCode.Value);
        }

        if (query.TransactionId.HasValue)
        {
            cases = cases.Where(x => x.Transactions.Any(t => t.AchTransactionId == query.TransactionId.Value));
        }

        if (query.ParticipantId.HasValue)
        {
            cases = cases.Where(x => x.RequestingParticipantId == query.ParticipantId.Value
                                  || x.RespondingParticipantId == query.ParticipantId.Value);
        }

        if (query.From.HasValue)
        {
            cases = cases.Where(x => x.FiledAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            cases = cases.Where(x => x.FiledAt <= query.To.Value);
        }

        var totalItems = await cases.CountAsync(ct);
        var items = await cases
            .OrderByDescending(x => x.FiledAt)
            .ThenByDescending(x => x.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new ClaimCaseListItemDto(
                x.Id,
                x.CauseCode,
                x.Status,
                x.FiledAt,
                x.DeadlineAt,
                x.ClosedAt,
                x.ExternalReference,
                new ClaimParticipantDto(
                    x.RequestingParticipant.Id,
                    x.RequestingParticipant.Name,
                    x.RequestingParticipant.RoutingNumber,
                    x.RequestingParticipant.TransitCode),
                new ClaimParticipantDto(
                    x.RespondingParticipant.Id,
                    x.RespondingParticipant.Name,
                    x.RespondingParticipant.RoutingNumber,
                    x.RespondingParticipant.TransitCode),
                x.Transactions.Count))
            .ToListAsync(ct);

        return new ClaimCaseQueryResult(items, query.PageNumber, query.PageSize, totalItems);
    }

    public async Task<IReadOnlyList<ClaimCaseListItemDto>> GetByTransactionAsync(
        int transactionId,
        CancellationToken ct = default)
    {
        if (transactionId <= 0)
        {
            throw new KeyNotFoundException($"No existe la transacción ACH con id {transactionId}.");
        }

        var transactionExists = await _context.AchTransactions
            .AsNoTracking()
            .AnyAsync(x => x.Id == transactionId, ct);
        if (!transactionExists)
        {
            throw new KeyNotFoundException($"No existe la transacción ACH con id {transactionId}.");
        }

        return await _context.ClaimCases
            .AsNoTracking()
            .Where(x => x.Transactions.Any(t => t.AchTransactionId == transactionId))
            .OrderByDescending(x => x.FiledAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new ClaimCaseListItemDto(
                x.Id,
                x.CauseCode,
                x.Status,
                x.FiledAt,
                x.DeadlineAt,
                x.ClosedAt,
                x.ExternalReference,
                new ClaimParticipantDto(
                    x.RequestingParticipant.Id,
                    x.RequestingParticipant.Name,
                    x.RequestingParticipant.RoutingNumber,
                    x.RequestingParticipant.TransitCode),
                new ClaimParticipantDto(
                    x.RespondingParticipant.Id,
                    x.RespondingParticipant.Name,
                    x.RespondingParticipant.RoutingNumber,
                    x.RespondingParticipant.TransitCode),
                x.Transactions.Count))
            .ToListAsync(ct);
    }

    public Task<ClaimCaseDetailDto> RecordProgressAsync(
        int id,
        RecordClaimProgressRequest request,
        string actor,
        string? idempotencyKey,
        CancellationToken ct = default)
        => MutateAsync(
            id,
            ClaimCaseEventType.Progress,
            NormalizeOptional(request?.Observation, 2000, "Observation"),
            actor,
            idempotencyKey,
            static (claimCase, occurredAt, text, normalizedActor, key) =>
                claimCase.RecordProgress(occurredAt, text, normalizedActor, key),
            ct);

    public Task<ClaimCaseDetailDto> ResolveAsync(
        int id,
        ResolveClaimCaseRequest request,
        string actor,
        string? idempotencyKey,
        CancellationToken ct = default)
        => MutateAsync(
            id,
            ClaimCaseEventType.Resolved,
            NormalizeOptional(request?.ResolutionNotes, 2000, "ResolutionNotes"),
            actor,
            idempotencyKey,
            static (claimCase, occurredAt, text, normalizedActor, key) =>
                claimCase.CloseResolved(occurredAt, text, normalizedActor, key),
            ct);

    public Task<ClaimCaseDetailDto> ExpireAsync(
        int id,
        ExpireClaimCaseRequest request,
        string actor,
        string? idempotencyKey,
        CancellationToken ct = default)
        => MutateAsync(
            id,
            ClaimCaseEventType.Expired,
            NormalizeOptional(request?.Observation, 2000, "Observation"),
            actor,
            idempotencyKey,
            static (claimCase, occurredAt, text, normalizedActor, key) =>
                claimCase.CloseExpired(occurredAt, text, normalizedActor, key),
            ct);

    private async Task<ClaimCaseDetailDto> MutateAsync(
        int id,
        ClaimCaseEventType expectedEventType,
        string? text,
        string actor,
        string? idempotencyKey,
        Func<ClaimCase, DateTimeOffset, string?, string, string?, ClaimCaseEvent> mutation,
        CancellationToken ct)
    {
        if (id <= 0)
        {
            throw new KeyNotFoundException($"No existe el caso Claims con id {id}.");
        }

        var normalizedActor = NormalizeActor(actor);
        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        var claimCase = await DetailQuery(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe el caso Claims con id {id}.");

        if (normalizedKey is not null)
        {
            var existingEvent = claimCase.Events.FirstOrDefault(x =>
                string.Equals(x.IdempotencyKey, normalizedKey, StringComparison.Ordinal));
            if (existingEvent is not null)
            {
                EnsureSameEventRequest(existingEvent, expectedEventType, text, normalizedActor);
                return ToDetail(claimCase);
            }
        }

        var occurredAt = ToOperationalTimestamp(_operationalTime.CaptureNow());
        mutation(claimCase, occurredAt, text, normalizedActor, normalizedKey);

        try
        {
            await _context.SaveChangesAsync(ct);
            return ToDetail(claimCase);
        }
        catch (DbUpdateException ex) when (normalizedKey is not null
                                           && RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
        {
            _context.ChangeTracker.Clear();
            var reloaded = await DetailQuery(tracking: false)
                .FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new KeyNotFoundException($"No existe el caso Claims con id {id}.");
            var existingEvent = reloaded.Events.FirstOrDefault(x =>
                string.Equals(x.IdempotencyKey, normalizedKey, StringComparison.Ordinal));
            if (existingEvent is null)
            {
                throw;
            }

            EnsureSameEventRequest(existingEvent, expectedEventType, text, normalizedActor);
            return ToDetail(reloaded);
        }
    }

    private IQueryable<ClaimCase> DetailQuery(bool tracking)
    {
        IQueryable<ClaimCase> query = _context.ClaimCases;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .Include(x => x.RequestingParticipant)
            .Include(x => x.RespondingParticipant)
            .Include(x => x.Transactions)
                .ThenInclude(x => x.AchTransaction)
                    .ThenInclude(x => x.SourceInstitution)
            .Include(x => x.Transactions)
                .ThenInclude(x => x.AchTransaction)
                    .ThenInclude(x => x.DestinationInstitution)
            .Include(x => x.Events);
    }

    private Task<ClaimCase?> LoadByCreationIdempotencyKeyAsync(
        string idempotencyKey,
        bool tracking,
        CancellationToken ct)
        => DetailQuery(tracking)
            .FirstOrDefaultAsync(x => x.CreationIdempotencyKey == idempotencyKey, ct);

    private static void EnsureSameCreateRequest(
        ClaimCase existing,
        IReadOnlyList<int> requestedTransactionIds,
        string? externalReference)
    {
        var existingIds = existing.Transactions
            .Select(x => x.AchTransactionId)
            .OrderBy(x => x)
            .ToArray();

        if (existing.CauseCode != ClaimCauseCode.DEV14
            || !existingIds.SequenceEqual(requestedTransactionIds.OrderBy(x => x))
            || !string.Equals(existing.ExternalReference, externalReference, StringComparison.Ordinal))
        {
            throw new ClaimCaseConflictException(
                "La Idempotency-Key de creación ya corresponde a una solicitud Claims diferente.");
        }
    }

    private static void EnsureSameEventRequest(
        ClaimCaseEvent existing,
        ClaimCaseEventType expectedType,
        string? text,
        string actor)
    {
        if (existing.EventType != expectedType
            || !string.Equals(existing.Observation, text, StringComparison.Ordinal)
            || !string.Equals(existing.Actor, actor, StringComparison.Ordinal))
        {
            throw new ClaimCaseConflictException(
                "La Idempotency-Key del caso ya corresponde a una operación Claims diferente.");
        }
    }

    private static ClaimCaseDetailDto ToDetail(ClaimCase claimCase)
        => new(
            claimCase.Id,
            claimCase.CauseCode,
            claimCase.Status,
            claimCase.FiledAt,
            claimCase.DeadlineAt,
            claimCase.ClosedAt,
            claimCase.ExternalReference,
            ToParticipant(claimCase.RequestingParticipant),
            ToParticipant(claimCase.RespondingParticipant),
            claimCase.Transactions
                .OrderBy(x => x.Id)
                .Select(x => new ClaimTransactionDto(
                    x.AchTransaction.Id,
                    x.AchTransaction.TraceNumber,
                    x.AchTransaction.TransactionExternalId,
                    x.AchTransaction.Amount,
                    x.AchTransaction.Type,
                    ToParticipant(x.AchTransaction.SourceInstitution),
                    ToParticipant(x.AchTransaction.DestinationInstitution)))
                .ToArray(),
            claimCase.Events
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.Id)
                .Select(x => new ClaimCaseEventDto(
                    x.Id,
                    x.EventType,
                    x.OccurredAt,
                    x.Observation,
                    x.Actor))
                .ToArray(),
            claimCase.ResolutionNotes);

    private static ClaimParticipantDto ToParticipant(FinancialInstitution institution)
        => new(institution.Id, institution.Name, institution.RoutingNumber, institution.TransitCode);

    private static IReadOnlyList<int> NormalizeTransactionIds(IReadOnlyCollection<int>? transactionIds)
    {
        if (transactionIds is null || transactionIds.Count == 0)
        {
            throw new ArgumentException("Debe indicar al menos una transacción ACH para crear DEV14.", nameof(transactionIds));
        }

        if (transactionIds.Any(x => x <= 0))
        {
            throw new ArgumentException("Todos los ids de transacción deben ser mayores a cero.", nameof(transactionIds));
        }

        var distinct = transactionIds.Distinct().ToArray();
        if (distinct.Length != transactionIds.Count)
        {
            throw new ArgumentException("La solicitud DEV14 no puede repetir transactionIds.", nameof(transactionIds));
        }

        return distinct;
    }

    private static void ValidateQuery(ClaimCaseQuery query)
    {
        if (query.PageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageNumber), "PageNumber debe ser mayor a cero.");
        }

        if (query.PageSize <= 0 || query.PageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(query.PageSize), $"PageSize debe estar entre 1 y {MaxPageSize}.");
        }

        if (query.TransactionId.HasValue && query.TransactionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.TransactionId), "TransactionId debe ser mayor a cero.");
        }

        if (query.ParticipantId.HasValue && query.ParticipantId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.ParticipantId), "ParticipantId debe ser mayor a cero.");
        }

        if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value)
        {
            throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");
        }
    }

    private static string NormalizeActor(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("El actor autenticado es obligatorio.", nameof(actor));
        }

        var normalized = actor.Trim();
        if (normalized.Length > 200)
        {
            throw new ArgumentException("El actor excede la longitud permitida.", nameof(actor));
        }

        return normalized;
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var normalized = idempotencyKey.Trim().ToLowerInvariant();
        if (normalized.Length > 128)
        {
            throw new ArgumentException("Idempotency-Key excede la longitud permitida.", nameof(idempotencyKey));
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{fieldName} excede la longitud permitida.", fieldName);
        }

        return normalized;
    }

    private static DateTimeOffset ToOperationalTimestamp(OperationalTimeSnapshot snapshot)
    {
        var capturedUtc = DateTime.SpecifyKind(snapshot.CapturedAtUtc, DateTimeKind.Utc);
        var local = DateTime.SpecifyKind(snapshot.BogotaTimestamp, DateTimeKind.Unspecified);
        var offset = local - DateTime.SpecifyKind(capturedUtc, DateTimeKind.Unspecified);
        if (offset < TimeSpan.FromHours(-14) || offset > TimeSpan.FromHours(14))
        {
            throw new InvalidOperationException("El snapshot de tiempo operacional contiene un offset inválido.");
        }

        return new DateTimeOffset(local, offset);
    }
}
