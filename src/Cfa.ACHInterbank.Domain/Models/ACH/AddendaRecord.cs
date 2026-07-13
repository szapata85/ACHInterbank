using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;
public class AddendaRecord
{
    public int AddendaID { get; set; }
    public string? CodeTypeAddendumRecord { get; set; }
    public string? BusinessType { get; set; }
    public string? IdUserOrig { get; set; }
    public string? PurposeOfTransaction { get; set; }
    public string? InvoiceOrAccountNumber { get; set; }
    public string? InfofromOriginator { get; set; }
    public string? CollectorId { get; set; }
    public string? ReceiverCustomerCode { get; set; }
    public string? ServiceDescription { get; set; }
    public string? PaymentRelatedInformation { get; set; }
    public string? ReturnReasonCode { get; set; }
    public string? OriginalTraceNumber { get; set; }
    public string? NewTraceNumber { get; set; }
    public string? AddendumSequence { get; set; }
    public string? EntryDetailSequenceNumber { get; set; }
    public string? NachaID { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
}
