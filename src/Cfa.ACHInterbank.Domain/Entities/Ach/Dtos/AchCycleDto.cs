namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class AchCycleDto
{
    public string Id { get; set; } = null!;
    public string CycleName { get; set; } = null!;
    public DateTime ProcessingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public bool RescheduleOnHoliday { get; set; }
    public int ClearingHouseId { get; set; }
    public int? ClearingHouseCycleConfigId { get; set; }
    public string? ClearingHouseName { get; set; }
    public string OperationalStatus { get; set; } = string.Empty;
    public bool AcceptsTransactions { get; set; }
    public bool IsContingencyCycle { get; set; }
    public string WindowLabel { get; set; } = string.Empty;
}

public class AchCycleRequest
{
    public string CycleName { get; set; } = null!;
    public DateTime ProcessingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public bool RescheduleOnHoliday { get; set; }
    public int ClearingHouseId { get; set; }
    public int? ClearingHouseCycleConfigId { get; set; }
}

public class AchCycleExportDto
{
    public string Id { get; set; } = null!;
    public string CycleId { get; set; } = null!;
    public string CycleName { get; set; } = null!;
    public DateTime ProcessingDate { get; set; }
    public string? ClearingHouseName { get; set; }
    public int TransactionCount { get; set; }
    public bool IsExportable { get; set; } = true;
    public string? ExportUnavailableReason { get; set; }
    public string? ExportIdentifier { get; set; }
}

public sealed record AchCycleConfigurationLinkRepairItem(
    string CycleId,
    int? ClearingHouseCycleConfigId,
    string Status,
    string Detail);

public sealed record AchCycleConfigurationLinkRepairResult(
    bool Completed,
    int InspectedCount,
    int RepairedCount,
    int AmbiguousCount,
    int UnmatchedCount,
    IReadOnlyList<AchCycleConfigurationLinkRepairItem> Items);
