using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class FileControl
{
    public int FileControlID { get; set; }
    public int BatchCount { get; set; }
    public int BlockCount { get; set; }
    public int EntryAddendaCount { get; set; }
    public long EntryHash { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public string? Reserved { get; set; }
    public string? NachaID { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
}
