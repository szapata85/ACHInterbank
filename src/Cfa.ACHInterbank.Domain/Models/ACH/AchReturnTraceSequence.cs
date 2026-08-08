namespace Cfa.ACHInterbank.Domain.Models.ACH;

public sealed class AchReturnTraceSequence
{
    public int Id { get; set; }
    public string ParticipantDfi { get; set; } = string.Empty;
    public DateOnly SequenceDate { get; set; }
    public int LastAssignedValue { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
