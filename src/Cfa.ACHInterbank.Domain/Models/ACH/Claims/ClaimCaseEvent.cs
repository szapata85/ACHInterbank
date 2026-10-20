using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH.Claims;

/// <summary>
/// Historial persistente propio de Claims. No reemplaza AchTransactionStateEvent.
/// </summary>
public sealed class ClaimCaseEvent : AuditableEntity
{
    private ClaimCaseEvent()
    {
    }

    public long Id { get; private set; }

    public int ClaimCaseId { get; private set; }
    public ClaimCase ClaimCase { get; private set; } = null!;

    public ClaimCaseEventType EventType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? Observation { get; private set; }
    public string? Actor { get; private set; }
    public string? IdempotencyKey { get; private set; }

    internal static ClaimCaseEvent Create(
        ClaimCase claimCase,
        ClaimCaseEventType eventType,
        DateTimeOffset occurredAt,
        string? observation = null,
        string? actor = null,
        string? idempotencyKey = null)
    {
        ArgumentNullException.ThrowIfNull(claimCase);

        if (occurredAt == default)
        {
            throw new ArgumentException("La fecha del evento Claims es obligatoria.", nameof(occurredAt));
        }

        return new ClaimCaseEvent
        {
            ClaimCase = claimCase,
            ClaimCaseId = claimCase.Id,
            EventType = eventType,
            OccurredAt = occurredAt,
            Observation = NormalizeOptional(observation),
            Actor = NormalizeOptional(actor),
            IdempotencyKey = NormalizeOptional(idempotencyKey)
        };
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
