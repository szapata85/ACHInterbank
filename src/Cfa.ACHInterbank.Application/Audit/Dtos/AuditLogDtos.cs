namespace Cfa.ACHInterbank.Application.Audit.Dtos;

public record AuditLogDto
{
    public Guid Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string? ChangedFields { get; init; }
}

public record AuditLogQuery
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? ChangedBy { get; init; }
    public string? Action { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
