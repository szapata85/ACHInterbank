namespace Cfa.ACHInterbank.Application.NavigationLogs.Dtos;

public record NavigationLogDto
{
    public Guid Id { get; init; }
    public string? UserId { get; init; }
    public string Route { get; init; } = string.Empty;
    public DateTime VisitedAt { get; init; }
    public string? SessionId { get; init; }
    public int? DurationMs { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public record NavigationLogCreate
{
    public string Route { get; init; } = string.Empty;
    public DateTime? VisitedAt { get; init; }
    public string? SessionId { get; init; }
    public int? DurationMs { get; init; }
}

public record NavigationLogQuery
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? UserId { get; init; }
    public string? Route { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
