using System.ComponentModel.DataAnnotations.Schema;

using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchControl : AuditableEntity
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
    public int? BatchHeaderId { get; set; }

    [ForeignKey("NachaID")]
    public virtual NachaHeader? NachaHeader { get; set; }
    public virtual BatchHeader? BatchHeader { get; set; }
}
