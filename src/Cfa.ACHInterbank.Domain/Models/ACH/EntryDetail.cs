using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class EntryDetail
{
    public int Id { get; set; }
    public string? TransactionCode { get; set; }
    public string? ReceivingParticipantEntityCode { get; set; }
    public string? CheckDigit { get; set; }
    public string? AccountNumber { get; set; }
    public string? Amount { get; set; }
    public string? RecipIdNumber { get; set; }
    public string? RecipUserName { get; set; }
    public string? DiscreData { get; set; }
    public string? AddendumIndicator { get; set; }
    public string? SequenceNumber { get; set; }

    public int BatchHeaderId { get; set; }
    public BatchHeader BatchHeader { get; set; }

    public ICollection<AddendaRecord> AddendaRecords { get; set; } = new List<AddendaRecord>();
}
