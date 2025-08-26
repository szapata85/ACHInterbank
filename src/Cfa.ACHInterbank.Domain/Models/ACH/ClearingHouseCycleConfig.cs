namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouseCycleConfig
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string CycleName { get; set; } = null!;
    public TimeSpan CutoffTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public ClearingHouse ClearingHouse { get; set; } = null!;
}