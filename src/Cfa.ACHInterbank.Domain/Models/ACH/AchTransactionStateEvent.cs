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
}
