namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class FileControl
{
    public int Id { get; set; }
    public int BatchCount { get; set; }
    public int BlockCount { get; set; }
    public int EntryAddendaCount { get; set; }
    public decimal EntryHash { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }
}
