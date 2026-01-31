namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record UserSummaryDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public IEnumerable<RoleSummaryDto> Roles { get; init; } = Enumerable.Empty<RoleSummaryDto>();
    public bool IsActive { get; init; }
}

public record UserQueryRequest
{
    public string? Search { get; init; }
    public Guid? RoleId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public record SaveUserRequest
{
    public string? UserName { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Password { get; init; }
    public IEnumerable<Guid>? RoleIds { get; init; }
}

public record AssignRolesRequest
{
    public IEnumerable<Guid>? RoleIds { get; init; }
}

public enum UserOperationStatus
{
    Success,
    NotFound,
    Conflict,
    ValidationError
}

public record UserOperationResult
{
    public UserOperationStatus Status { get; init; }
    public string? Message { get; init; }
    public UserSummaryDto? User { get; init; }

    public static UserOperationResult ValidationError(string message) => new()
    {
        Status = UserOperationStatus.ValidationError,
        Message = message
    };

    public static UserOperationResult Conflict(string message) => new()
    {
        Status = UserOperationStatus.Conflict,
        Message = message
    };

    public static UserOperationResult NotFound() => new()
    {
        Status = UserOperationStatus.NotFound
    };

    public static UserOperationResult Success(UserSummaryDto? user = null) => new()
    {
        Status = UserOperationStatus.Success,
        User = user
    };
}
