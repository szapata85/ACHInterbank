using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouse
{
    public int Id { get; set; }
    public string Name { get; set; }     // e.g., "ACH Colombia"
    public string Code { get; set; }     // e.g., "ACHCOL"
    public int ClearingHouseId { get; set; }

    [ForeignKey("ClearingHouseId")]
    public virtual ClearingHouseConfig ClearingHouseConfig { get; set; }

    public virtual ICollection<AchCycle> AchCycles { get; set; }
}
