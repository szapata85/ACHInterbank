namespace Cfa.ACHInterbank.Application.Customers.Dtos;

public record CustomerSummaryDto
{
    public int Id { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string PersonType { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string FullName { get; init; } = string.Empty;
}

public record CustomerDetailDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string? SecondLastName { get; init; }
    public string? Gender { get; init; }
    public string PersonType { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
}

public record SaveCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string? SecondLastName { get; init; }
    public string? Gender { get; init; }
    public string PersonType { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
}
