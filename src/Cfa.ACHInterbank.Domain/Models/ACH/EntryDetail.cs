using Cfa.ACHInterbank.Domain.Entities.User;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class EntryDetail
{
    public int EntryDetailID { get; set; }
    public string? TransactionCode { get; set; }
    public string? ReceivingParticipantEntityCode { get; set; }
    public string? CheckDigit { get; set; }
    public string? AccountNumber { get; set; }
    public decimal? Amount { get; set; }
    public string? RecipIdNumber { get; set; }
    public string? RecipUserName { get; set; }
    public string? DiscreData { get; set; }
    public string? AddendumIndicator { get; set; }
    public string? SequenceNumber { get; set; }
    public string? NachaID { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
}
