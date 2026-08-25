namespace Cfa.ACHInterbank.Domain.Models.ACH;

public sealed class AchTransactionTraceSequence
{
    public int Id { get; set; }
    public string OriginatingDfi { get; set; } = string.Empty;
    public DateOnly SequenceDate { get; set; }
    public int LastAssignedValue { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
