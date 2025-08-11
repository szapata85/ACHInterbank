using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchControl
{
    public int BatchControlID { get; set; }
    public string? BatchTranClassCode { get; set; }
    public int? EntryAddendaCount { get; set; }
    public int? TotalEntry { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public string? IdUserOrig { get; set; }
    public string? CodAutMessage { get; set; }
    public string? IdOrigEntity { get; set; }
    public string? BatchNumber { get; set; }
    public int NachaID { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
}
