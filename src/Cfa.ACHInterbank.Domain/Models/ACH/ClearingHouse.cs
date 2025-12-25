using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string OriginCode { get; set; } = null!;

    public int ClearingHouseId { get; set; }

    [ForeignKey("ClearingHouseId")]
    public virtual ClearingHouseConfig ClearingHouseConfig { get; set; } = null!;

    public virtual ICollection<AchCycle> AchCycles { get; set; } = new List<AchCycle>();
    public virtual ICollection<FinancialInstitution> FinancialInstitutions { get; set; } = new List<FinancialInstitution>();
    public virtual ICollection<ClearingHouseSpecialDate> SpecialDates { get; set; } = new List<ClearingHouseSpecialDate>();
}
