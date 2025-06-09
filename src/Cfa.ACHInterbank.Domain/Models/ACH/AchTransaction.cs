namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransaction
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; }
    public string Type { get; set; } // Credit/Debit, etc.

    public int AchCycleId { get; set; }
    public AchCycle AchCycle { get; set; }
}
