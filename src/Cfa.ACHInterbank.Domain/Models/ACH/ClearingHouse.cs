namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouse
{
    public int Id { get; set; }
    public string Name { get; set; }     // e.g., "ACH Colombia"
    public string Code { get; set; }     // e.g., "ACHCOL"

    public ICollection<AchCycle> AchCycles { get; set; }
}
