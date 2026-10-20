namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnGenerated
{
    public int Id { get; set; }
    public int OriginalTransactionId { get; set; }
    public AchTransaction OriginalTransaction { get; set; } = null!;

    public string ReturnCycleId { get; set; } = string.Empty;
    public AchCycle ReturnCycle { get; set; } = null!;

    public string ReturnReasonCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string NewSequenceNumber { get; set; } = string.Empty;
    public string OriginalSequenceNumber { get; set; } = string.Empty;
    public string ReceiverEntityCode { get; set; } = string.Empty;
    public string OriginatorEntityCode { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public DateOnly SequenceDate { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
