using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class BulkAchTransactionRequest
{
    public string BatchReference { get; set; } = string.Empty;
    public List<BulkAchTransactionItemRequest> Transactions { get; set; } = [];
    public int? ChunkSize { get; set; }
}

public sealed class BulkAchTransactionItemRequest
{
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public TransactionTypeEnum Type { get; set; }
    public AccountTypeEnum AccountType { get; set; } = AccountTypeEnum.Checking;
    public bool IsPrenotification { get; set; }
    public int DestinationInstitutionId { get; set; }
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string? RecipientIdNumber { get; set; }
    public string? RecipientName { get; set; }
    public bool RequiresIdentityValidation { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentification { get; set; } = string.Empty;
    public int CompanyEntryDescriptionId { get; set; }
    public string? SourcePersonType { get; set; }
    public string? RecipientPersonType { get; set; }
    public List<AddendaDto>? Addendas { get; set; }
}

public sealed class BulkAchTransactionResponse
{
    public string BatchReference { get; set; } = string.Empty;
    public int TotalReceived { get; set; }
    public int TotalProcessed { get; set; }
    public int TotalSucceeded { get; set; }
    public int TotalFailed { get; set; }
    public List<int> CreatedTransactionIds { get; set; } = [];
    public List<BulkAchTransactionItemResult> ItemResults { get; set; } = [];
}

public sealed class BulkAchTransactionItemResult
{
    public int Index { get; set; }
    public string Reference { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public int? TransactionId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
