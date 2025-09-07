namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransactionAddenda
{
    public int Id { get; set; }

    public int AchTransactionId { get; set; }
    public AchTransaction? Transaction { get; set; }

    public string AddendaType { get; set; } = "05"; // usualmente "05"
    public string Information { get; set; } = null!;
}

