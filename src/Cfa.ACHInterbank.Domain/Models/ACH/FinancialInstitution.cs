namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class FinancialInstitution
{
    public int Id { get; set; }
    public string AchCode { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ICollection<AchTransaction> SourceTransactions { get; set; } = new List<AchTransaction>();
    public ICollection<AchTransaction> DestinationTransactions { get; set; } = new List<AchTransaction>();
}

