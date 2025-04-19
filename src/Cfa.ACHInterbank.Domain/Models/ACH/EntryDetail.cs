namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class EntryDetail
{
    public int Id { get; set; }
    public string TransactionCode { get; set; }
    public string ReceivingDfiIdentification { get; set; }
    public string DfiAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string IndividualIdNumber { get; set; }
    public string IndividualName { get; set; }
    public string TraceNumber { get; set; }
}
