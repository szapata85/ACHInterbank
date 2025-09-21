using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
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
    public string AddendaType { get; set; } = null!;
    public string Information { get; set; } = null!;
}

