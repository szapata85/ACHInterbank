using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransactionAddenda : AuditableEntity
{
    public int Id { get; set; }

    public int AchTransactionId { get; set; }
    public AchTransaction AchTransaction { get; set; } = null!;

    public string AddendaType { get; set; } = "05";
    public string Information { get; set; } = string.Empty;

    public int SequenceNumber { get; set; }
    public int EntryDetailSequenceNumber { get; set; }
}


