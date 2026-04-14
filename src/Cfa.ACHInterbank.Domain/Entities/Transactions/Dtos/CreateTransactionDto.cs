using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
    public string? TransactionExternalId { get; set; }
    public string Reference { get; set; } = null!;
    public TransactionTypeEnum Type { get; set; }

    public int DestinationInstitutionId { get; set; }

    // ✅ Nuevos campos
    public string SourceAccountNumber { get; set; } = null!;
    public string DestinationAccountNumber { get; set; } = null!;

    // Addendas opcionales
    public List<AddendaDto>? Addendas { get; set; }
}

public class AddendaDto
{
    public string AddendaType { get; set; } = "05";
    public AchAddendaBusinessType? BusinessType { get; set; }
    public string? Information { get; set; }
    public string? Purpose { get; set; }
    public string? Reference { get; set; }
    public string? CollectorId { get; set; }
    public string? ReceiverCustomerCode { get; set; }
    public string? ServiceDescription { get; set; }
    public string? ReturnReasonCode { get; set; }
    public string? OriginalTraceNumber { get; set; }
    public string? NewTraceNumber { get; set; }
}
