namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchControl
{
    public int Id { get; set; }
    public string ServiceClassCode { get; set; }
    public int EntryAddendaCount { get; set; }
    public decimal EntryHash { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public string CompanyId { get; set; }
    public string OdfiIdentification { get; set; }
}
