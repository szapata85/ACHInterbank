namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class ClearingHouseCycleConfigDto
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string? ClearingHouseName { get; set; }
    public string CycleName { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsCurrent { get; set; }
}

public class UpsertClearingHouseCycleConfigDto
{
    public int ClearingHouseId { get; set; }
    public string CycleName { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public DateTime EffectiveFrom { get; set; }
}

public class InactivateClearingHouseCycleConfigDto
{
    public DateTime EffectiveTo { get; set; }
}

public class ChangeClearingHouseCycleStatusDto
{
    public bool IsActive { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
