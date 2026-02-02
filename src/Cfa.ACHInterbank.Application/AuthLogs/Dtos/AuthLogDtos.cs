namespace Cfa.ACHInterbank.Application.AuthLogs.Dtos;

public record AuthLogDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime LoggedAt { get; init; }
}

public record AuthLogQuery
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Username { get; init; }
    public bool? Success { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public record AuthLogCreate
{
    public string Username { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
