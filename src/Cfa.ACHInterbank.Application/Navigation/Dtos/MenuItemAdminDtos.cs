namespace Cfa.ACHInterbank.Application.Navigation.Dtos;

public record MenuItemAdminDto
{
    public int Id { get; init; }
    public int? ParentId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int Order { get; init; }
    public bool Exact { get; init; }
    public bool IsActive { get; init; }
    public List<Guid> RoleIds { get; init; } = new();
    public List<Guid> PermissionIds { get; init; } = new();
    public List<MenuItemAdminDto> Children { get; init; } = new();
}

public record SaveMenuItemRequest
{
    public int? ParentId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int Order { get; init; }
    public bool Exact { get; init; }
    public bool IsActive { get; init; }
    public IEnumerable<Guid> RoleIds { get; init; } = Array.Empty<Guid>();
    public IEnumerable<Guid> PermissionIds { get; init; } = Array.Empty<Guid>();
}
