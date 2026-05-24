namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class PrenotificationStatusDto
{
    public int Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string ClearingHouse { get; init; } = string.Empty;
    public string SourceFinancialInstitution { get; init; } = string.Empty;
    public bool SourceIsDefault { get; init; }
    public int TransactionId { get; init; }
    public string NachaCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusDescription { get; init; } = string.Empty;
    public DateTime EffectiveDate { get; init; }
    public DateTimeOffset? ApprovedAt { get; init; }
    public DateTime MaturityDate { get; init; }
    public bool IsMatured { get; init; }
    public bool CanBeUsedForDebit { get; init; }
    public string Message { get; init; } = string.Empty;
}
