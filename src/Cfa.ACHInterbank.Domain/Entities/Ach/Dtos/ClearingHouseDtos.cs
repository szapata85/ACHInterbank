using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class ClearingHouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string OriginCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public string? HolidayStrategy { get; set; }
    public string? PaymentRailCode { get; set; }
    public bool RequiresNachaProfile { get; set; }
    public int? NachaProfileId { get; set; }
    public string? NachaProfileCode { get; set; }
    public string? NachaProfileName { get; set; }
    public int ActiveCycleCount { get; set; }
    public bool IsReady { get; set; }
    public IReadOnlyList<string> MissingRequirements { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ClearingHouseAdminQuery : PaginationRequest
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateClearingHouseRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OriginCode { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public string HolidayStrategy { get; set; } = string.Empty;
    public string? PaymentRailCode { get; set; }
    public bool RequiresNachaProfile { get; set; }
    public int? NachaProfileId { get; set; }
}

public sealed class UpdateClearingHouseRequest : CreateClearingHouseRequest
{
    public DateTimeOffset? ExpectedUpdatedAt { get; set; }
}

public sealed class ChangeClearingHouseStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class ClearingHouseReadinessDto
{
    public bool IsReady { get; init; }
    public IReadOnlyList<string> MissingRequirements { get; init; } = [];
}

public sealed class ClearingHouseNachaProfileOptionDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class ClearingHousePaymentRailOptionDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
