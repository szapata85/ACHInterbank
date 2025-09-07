using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class CreateTransactionRequest
{
    public decimal Amount { get; set; }
    public string Reference { get; set; } = null!;
    public TransactionTypeEnum Type { get; set; }
    public int SourceInstitutionId { get; set; }
    public int DestinationInstitutionId { get; set; }
    public int AchCycleId { get; set; }

    public List<AddendaDto>? Addendas { get; set; }
}

public class AddendaDto
{
    public string AddendaType { get; set; } = "05";
    public string Information { get; set; } = null!;
}
