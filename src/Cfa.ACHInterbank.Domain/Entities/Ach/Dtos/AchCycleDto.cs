namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class AchCycleDto
{
    public int Id { get; set; }
    public string CycleName { get; set; } = null!;
    public DateTime ProcessingDate { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public bool RescheduleOnHoliday { get; set; }
    public int ClearingHouseId { get; set; }
    public string? ClearingHouseName { get; set; }
}

public class AchCycleRequest
{
    public string CycleName { get; set; } = null!;
    public DateTime ProcessingDate { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public bool RescheduleOnHoliday { get; set; }
    public int ClearingHouseId { get; set; }
}

public class AchCycleExportDto
{
    public int Id { get; set; }
    public string CycleName { get; set; } = null!;
    public DateTime ProcessingDate { get; set; }
    public string? ClearingHouseName { get; set; }
    public int TransactionCount { get; set; }
}
