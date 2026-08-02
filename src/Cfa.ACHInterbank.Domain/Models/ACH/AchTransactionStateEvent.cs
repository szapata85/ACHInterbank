using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransactionStateEvent : AuditableEntity
{
    public long Id { get; set; }

    public int AchTransactionId { get; set; }
    public AchTransaction AchTransaction { get; set; } = null!;

    public AchTransferStateEnum FromState { get; set; }
    public AchTransferStateEnum ToState { get; set; }
    public AchStateEventSourceEnum Source { get; set; }

    public string? ReasonCode { get; set; }
    public string? PayloadJson { get; set; }
    public string? IdempotencyKey { get; set; }
    public int? ClearingHouseId { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }
    public int? AchReturnCodeId { get; set; }
    public AchReturnCode? AchReturnCode { get; set; }
    public string? ResolvedReasonDescription { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
