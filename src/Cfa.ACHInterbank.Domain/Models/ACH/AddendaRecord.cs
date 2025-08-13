using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;
public class AddendaRecord
{
    public int AddendaID { get; set; }
    public string? CodeTypeAddendumRecord { get; set; }
    public string? IdUserOrig { get; set; }
    public string? PurposeOfTransaction { get; set; }
    public string? InvoiceOrAccountNumber { get; set; }
    public string? InfofromOriginator { get; set; }
    public string? AddendumSequence { get; set; }
    public string? EntryDetailSequenceNumber { get; set; }
    public string? NachaID { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
}
