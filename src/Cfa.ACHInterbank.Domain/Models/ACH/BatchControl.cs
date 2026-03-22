using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchControl
{
    public int BatchControlID { get; set; }
    public string? BatchTranClassCode { get; set; }
    public int? EntryAddendaCount { get; set; }
    public long? EntryHash { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public string? IdUserOrig { get; set; }
    public string? CodAutMessage { get; set; }
    public string? Reserved { get; set; }
    public string? IdOrigEntity { get; set; }
    public string? BatchNumber { get; set; }
    public string? NachaID { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
}
