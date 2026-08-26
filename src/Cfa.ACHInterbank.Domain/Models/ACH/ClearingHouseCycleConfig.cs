using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouseCycleConfig
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string PolicyVersion { get; set; } = "LEGACY";
    public string CycleName { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public TimeSpan OutputReleaseTime { get; set; }
    public bool AllowsMonetaryCredit { get; set; } = true;
    public bool AllowsMonetaryDebit { get; set; } = true;
    public bool AllowsCreditPrenotification { get; set; } = true;
    public bool AllowsDebitPrenotification { get; set; } = true;
    public bool AllowsReturn { get; set; } = true;
    public bool AllowsReturnOfReturn { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public ClearingHouse ClearingHouse { get; set; } = null!;
    public ICollection<AchCycle> AchCycles { get; set; } = new List<AchCycle>();
}
