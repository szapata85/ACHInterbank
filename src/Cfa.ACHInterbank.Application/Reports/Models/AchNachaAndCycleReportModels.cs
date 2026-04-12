namespace Cfa.ACHInterbank.Application.Reports.Models;

public sealed class AchNachaFileReportFilter
{
    public DateTime? Date { get; init; }
    public int? ClearingHouseId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class AchNachaFileReportRowDto
{
    public string FileName { get; init; } = string.Empty;
    public DateTime GeneratedAtUtc { get; init; }
    public string ClearingHouseName { get; init; } = string.Empty;
    public string ExportKind { get; init; } = string.Empty;
    public int TotalRecords { get; init; }
    public int TotalTransactions { get; init; }
}

public sealed class AchNachaFileReportTotalsDto
{
    public int TotalFiles { get; init; }
    public int TotalRecords { get; init; }
    public int TotalTransactions { get; init; }
}

public sealed class AchNachaFileReportResponseDto
{
    public IReadOnlyList<AchNachaFileReportRowDto> Items { get; init; } = [];
    public AchNachaFileReportTotalsDto Totals { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}

public sealed class AchCycleReportFilter
{
    public DateTime? Date { get; init; }
    public int? ClearingHouseId { get; init; }
    public string? Name { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class AchCycleReportRowDto
{
    public string CycleId { get; init; } = string.Empty;
    public string CycleName { get; init; } = string.Empty;
    public DateTime ProcessingDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public TimeSpan CutoffTime { get; init; }
    public string Schedule => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public string Status { get; set; } = string.Empty;
    public string ClearingHouseName { get; init; } = string.Empty;
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class AchCycleReportTotalsDto
{
    public int TotalCycles { get; init; }
    public int TotalTransactions { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class AchCycleReportResponseDto
{
    public IReadOnlyList<AchCycleReportRowDto> Items { get; init; } = [];
    public AchCycleReportTotalsDto Totals { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
}
