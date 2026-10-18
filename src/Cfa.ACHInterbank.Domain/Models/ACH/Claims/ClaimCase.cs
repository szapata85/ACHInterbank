using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Domain.Models.ACH.Claims;

/// <summary>
/// Caso persistente del flujo Claims. Es independiente de ReturnOut y de la trazabilidad
/// de estados de una AchTransaction.
/// </summary>
public sealed class ClaimCase : AuditableEntity
{
    private const string AchColombiaClearingHouseCode = "ACHCOL";

    private ClaimCase()
    {
    }

    public int Id { get; private set; }

    public int ClearingHouseId { get; private set; }
    public ClearingHouse ClearingHouse { get; private set; } = null!;

    public ClaimCauseCode CauseCode { get; private set; }

    /// <summary>Participante que radica el caso. Para DEV14 corresponde a la EPR de la transacción débito.</summary>
    public int RequestingParticipantId { get; private set; }
    public FinancialInstitution RequestingParticipant { get; private set; } = null!;

    /// <summary>Participante al que se dirige el caso. Para DEV14 corresponde a la EPO de la transacción.</summary>
    public int RespondingParticipantId { get; private set; }
    public FinancialInstitution RespondingParticipant { get; private set; } = null!;

    /// <summary>
    /// Fecha de radicación/apertura funcional del caso.
    /// </summary>
    public DateTimeOffset FiledAt { get; private set; }

    /// <summary>
    /// Deadline funcional del caso Claims. Para DEV14 debe ser calculado por una capa superior
    /// usando el calendario de días hábiles de la cámara; no reutiliza SLA/ciclos de ReturnOut.
    /// </summary>
    public DateTimeOffset? DeadlineAt { get; private set; }

    /// <summary>
    /// Lifecycle interno mínimo de Claims. No corresponde a AchTransferStateEnum.
    /// </summary>
    public ClaimCaseStatus Status { get; private set; } = ClaimCaseStatus.Open;

    public DateTimeOffset? ClosedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// Referencia externa opcional. Su semántica oficial será definida cuando se integre
    /// el identificador del módulo Reclamos de ACH Colombia.
    /// </summary>
    public string? ExternalReference { get; private set; }

    /// <summary>
    /// Clave técnica opcional para deduplicar comandos de creación sin convertir DEV14 +
    /// TransactionId en una llave de negocio. No representa el número oficial del caso.
    /// </summary>
    public string? CreationIdempotencyKey { get; private set; }

    /// <summary>
    /// Asociaciones transaccionales del caso. La asociación directa a AchBatch se difiere hasta
    /// confirmar que el lote operativo local corresponde inequívocamente al lote del módulo Claims.
    /// </summary>
    public ICollection<ClaimTransaction> Transactions { get; private set; } = new List<ClaimTransaction>();
    public ICollection<ClaimCaseEvent> Events { get; private set; } = new List<ClaimCaseEvent>();

    public static ClaimCase Create(
        ClearingHouse clearingHouse,
        ClaimCauseCode causeCode,
        FinancialInstitution requestingParticipant,
        FinancialInstitution respondingParticipant,
        DateTimeOffset filedAt,
        string? externalReference = null,
        string? creationIdempotencyKey = null,
        DateTimeOffset? deadlineAt = null)
    {
        ArgumentNullException.ThrowIfNull(clearingHouse);
        ArgumentNullException.ThrowIfNull(requestingParticipant);
        ArgumentNullException.ThrowIfNull(respondingParticipant);

        if (clearingHouse.Id <= 0)
        {
            throw new ArgumentException("La cámara debe existir antes de crear un caso Claims.", nameof(clearingHouse));
        }

        if (!Enum.IsDefined(typeof(ClaimCauseCode), causeCode))
        {
            throw new ArgumentOutOfRangeException(nameof(causeCode), causeCode, "La causal Claims no es válida.");
        }

        if (causeCode == ClaimCauseCode.DEV14
            && !string.Equals(clearingHouse.Code?.Trim(), AchColombiaClearingHouseCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("DEV14 pertenece al flujo Claims de ACH Colombia.", nameof(clearingHouse));
        }

        if (requestingParticipant.Id <= 0)
        {
            throw new ArgumentException("El participante solicitante debe existir.", nameof(requestingParticipant));
        }

        if (respondingParticipant.Id <= 0)
        {
            throw new ArgumentException("El participante destinatario debe existir.", nameof(respondingParticipant));
        }

        if (requestingParticipant.Id == respondingParticipant.Id)
        {
            throw new ArgumentException("Los participantes solicitante y destinatario deben ser distintos.");
        }

        if (filedAt == default)
        {
            throw new ArgumentException("La fecha de radicación es obligatoria.", nameof(filedAt));
        }

        ValidateDeadline(filedAt, deadlineAt);

        var claimCase = new ClaimCase
        {
            ClearingHouseId = clearingHouse.Id,
            ClearingHouse = clearingHouse,
            CauseCode = causeCode,
            RequestingParticipantId = requestingParticipant.Id,
            RequestingParticipant = requestingParticipant,
            RespondingParticipantId = respondingParticipant.Id,
            RespondingParticipant = respondingParticipant,
            FiledAt = filedAt,
            DeadlineAt = deadlineAt,
            Status = ClaimCaseStatus.Open,
            ExternalReference = NormalizeOptional(externalReference),
            CreationIdempotencyKey = NormalizeOptional(creationIdempotencyKey)
        };

        claimCase.Events.Add(ClaimCaseEvent.Create(claimCase, ClaimCaseEventType.Opened, filedAt));
        return claimCase;
    }

    public ClaimTransaction AddTransaction(AchTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Id <= 0)
        {
            throw new ArgumentException("La transacción ACH debe existir antes de asociarla al caso.", nameof(transaction));
        }

        EnsureOpen();

        if (CauseCode == ClaimCauseCode.DEV14)
        {
            ValidateDev14Transaction(transaction);
        }

        if (Transactions.Any(x => x.AchTransactionId == transaction.Id))
        {
            throw new InvalidOperationException($"La transacción ACH {transaction.Id} ya está asociada al caso Claims.");
        }

        var association = ClaimTransaction.Create(this, transaction);
        Transactions.Add(association);
        return association;
    }

    public void SetDeadline(DateTimeOffset deadlineAt)
    {
        EnsureOpen();

        if (DeadlineAt.HasValue)
        {
            throw new InvalidOperationException("El deadline Claims ya fue establecido para el caso.");
        }

        ValidateDeadline(FiledAt, deadlineAt);
        DeadlineAt = deadlineAt;
    }

    public ClaimCaseEvent RecordProgress(
        DateTimeOffset occurredAt,
        string? observation = null,
        string? actor = null,
        string? idempotencyKey = null)
    {
        EnsureOpen();
        ValidateEventTime(occurredAt);

        if (DeadlineAt.HasValue && occurredAt > DeadlineAt.Value)
        {
            throw new InvalidOperationException("No se puede registrar un avance Claims posterior al deadline del caso.");
        }

        return AddEvent(ClaimCaseEventType.Progress, occurredAt, observation, actor, idempotencyKey);
    }

    public ClaimCaseEvent CloseResolved(
        DateTimeOffset occurredAt,
        string? resolutionNotes = null,
        string? actor = null,
        string? idempotencyKey = null)
    {
        EnsureOpen();
        ValidateEventTime(occurredAt);

        var normalizedResolution = NormalizeOptional(resolutionNotes);
        var claimEvent = AddEvent(ClaimCaseEventType.Resolved, occurredAt, normalizedResolution, actor, idempotencyKey);
        ResolutionNotes = normalizedResolution;
        Status = ClaimCaseStatus.Resolved;
        ClosedAt = occurredAt;
        return claimEvent;
    }

    public ClaimCaseEvent CloseExpired(
        DateTimeOffset occurredAt,
        string? observation = null,
        string? actor = null,
        string? idempotencyKey = null)
    {
        EnsureOpen();
        ValidateEventTime(occurredAt);

        if (!DeadlineAt.HasValue)
        {
            throw new InvalidOperationException("No se puede cerrar por expiración un caso Claims sin deadline establecido.");
        }

        if (occurredAt < DeadlineAt.Value)
        {
            throw new InvalidOperationException("No se puede cerrar por expiración antes del deadline Claims.");
        }

        var claimEvent = AddEvent(ClaimCaseEventType.Expired, occurredAt, observation, actor, idempotencyKey);
        Status = ClaimCaseStatus.Expired;
        ClosedAt = occurredAt;
        return claimEvent;
    }

    private void ValidateDev14Transaction(AchTransaction transaction)
    {
        if (transaction.Type != TransactionTypeEnum.Debit)
        {
            throw new ArgumentException("DEV14 solo puede asociarse a una transacción ACH débito.", nameof(transaction));
        }

        if (transaction.AchCycle is null || transaction.AchCycle.ClearingHouseId <= 0)
        {
            throw new ArgumentException(
                "No es posible verificar la cámara de la transacción DEV14 sin su AchCycle y ClearingHouseId.",
                nameof(transaction));
        }

        if (transaction.AchCycle.ClearingHouseId != ClearingHouseId)
        {
            throw new ArgumentException("La transacción DEV14 debe pertenecer a la misma cámara ACH Colombia del caso.", nameof(transaction));
        }

        if (transaction.DestinationInstitutionId != RequestingParticipantId)
        {
            throw new ArgumentException(
                "Para DEV14 el participante solicitante debe corresponder al participante receptor/destino de la transacción débito.",
                nameof(transaction));
        }

        if (transaction.SourceInstitutionId != RespondingParticipantId)
        {
            throw new ArgumentException(
                "Para DEV14 el participante destinatario debe corresponder al participante originador/origen de la transacción débito.",
                nameof(transaction));
        }
    }

    private ClaimCaseEvent AddEvent(
        ClaimCaseEventType eventType,
        DateTimeOffset occurredAt,
        string? observation,
        string? actor,
        string? idempotencyKey)
    {
        var normalizedKey = NormalizeOptional(idempotencyKey);
        if (normalizedKey is not null
            && Events.Any(x => string.Equals(x.IdempotencyKey, normalizedKey, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("La idempotency key del evento Claims ya fue utilizada en este caso.");
        }

        var claimEvent = ClaimCaseEvent.Create(this, eventType, occurredAt, observation, actor, normalizedKey);
        Events.Add(claimEvent);
        return claimEvent;
    }

    private void EnsureOpen()
    {
        if (Status != ClaimCaseStatus.Open)
        {
            throw new InvalidOperationException("El caso Claims está cerrado y no admite nuevas operaciones de lifecycle.");
        }
    }

    private void ValidateEventTime(DateTimeOffset occurredAt)
    {
        if (occurredAt == default)
        {
            throw new ArgumentException("La fecha del evento Claims es obligatoria.", nameof(occurredAt));
        }

        if (occurredAt < FiledAt)
        {
            throw new ArgumentException("La fecha del evento Claims no puede ser anterior a la radicación.", nameof(occurredAt));
        }
    }

    private static void ValidateDeadline(DateTimeOffset filedAt, DateTimeOffset? deadlineAt)
    {
        if (deadlineAt.HasValue && deadlineAt.Value <= filedAt)
        {
            throw new ArgumentException("El deadline Claims debe ser posterior a la fecha de radicación.", nameof(deadlineAt));
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
