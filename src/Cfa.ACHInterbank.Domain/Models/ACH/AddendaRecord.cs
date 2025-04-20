namespace Cfa.ACHInterbank.Domain.Models.ACH;
public class AddendaRecord
{
    public int Id { get; set; }
    public string PaymentRelatedInformation { get; set; }
    public string AddendaSequenceNumber { get; set; }
    public string EntryDetailSequenceNumber { get; set; }

    public int EntryDetailId { get; set; }
    public EntryDetail EntryDetail { get; set; }
}
