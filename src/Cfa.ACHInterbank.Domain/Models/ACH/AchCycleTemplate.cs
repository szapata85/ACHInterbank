namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchCycleTemplate
{
    public string CycleName { get; set; } = null!;
    public TimeSpan CutoffTime { get; set; }
    public bool RescheduleOnHoliday { get; set; }
}
