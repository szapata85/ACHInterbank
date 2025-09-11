namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class FinancialInstitutionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int ClearingHouseId { get; set; }
}

