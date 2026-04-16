using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public record AchTransactionRequestData
{
    public decimal Amount { get; init; }
    /// <summary>
    /// Identificador operativo/idempotencia canónico.
    /// </summary>
    public string? TransactionExternalId { get; init; }
    /// <summary>
    /// LEGACY transicional.
    /// </summary>
    public string Reference { get; init; } = string.Empty;
    public TransactionTypeEnum Type { get; init; }
    public AccountTypeEnum AccountType { get; init; }
    public bool IsPrenotification { get; init; }
    public int DestinationInstitutionId { get; init; }
    public string SourceAccountNumber { get; init; } = string.Empty;
    public string DestinationAccountNumber { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyIdentification { get; init; } = string.Empty;
    public int CompanyEntryDescriptionId { get; init; }
    public string? SourcePersonType { get; init; }
    public string? RecipientPersonType { get; init; }
    public string? RecipientIdNumber { get; init; }
    public string? RecipientName { get; init; }
    public bool RequiresIdentityValidation { get; init; }
    public IReadOnlyList<AddendaDto>? Addendas { get; init; }
}
