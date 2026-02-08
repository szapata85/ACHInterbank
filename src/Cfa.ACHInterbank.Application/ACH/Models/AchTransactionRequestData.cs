using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public record AchTransactionRequestData
{
    public decimal Amount { get; init; }
    public string Reference { get; init; } = string.Empty;
    public TransactionTypeEnum Type { get; init; }
    public AccountTypeEnum AccountType { get; init; }
    public bool IsPrenotification { get; init; }
    public bool IsReturn { get; init; }
    public int DestinationInstitutionId { get; init; }
    public string SourceAccountNumber { get; init; } = string.Empty;
    public string DestinationAccountNumber { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyIdentification { get; init; } = string.Empty;
    public string CompanyEntryDescription { get; init; } = "PAGOS";
    public string? RecipientIdNumber { get; init; }
    public bool RequiresIdentityValidation { get; init; }
    public IEnumerable<AddendaDto>? Addendas { get; init; }
}
