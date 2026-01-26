namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record RoleSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IEnumerable<string> Permissions { get; init; } = Enumerable.Empty<string>();
}

public record PermissionSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
