using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public record CustomerThirdPartyQuery
{
    public string? Search { get; init; }
    public string? DestinationAccountNumber { get; init; }
    public string? RecipientIdNumber { get; init; }
    public int? DestinationInstitutionId { get; init; }
    public CustomerThirdPartyStatusEnum? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record CustomerThirdPartyListDto
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int DestinationInstitutionId { get; init; }
    public string DestinationInstitutionName { get; init; } = string.Empty;
    public string DestinationAccountNumber { get; init; } = string.Empty;
    public string RecipientIdNumber { get; init; } = string.Empty;
    public CustomerThirdPartyStatusEnum Status { get; init; }
    public int? PrenotificationTransactionId { get; init; }
    public string? ValidationCycleId { get; init; }
    public DateTime? ValidationReceivedAt { get; init; }
    public string? ValidationMessage { get; init; }
}

public record UpdateCustomerThirdPartyStatusRequest
{
    public CustomerThirdPartyStatusEnum Status { get; init; }
    public string? ValidationMessage { get; init; }
}
