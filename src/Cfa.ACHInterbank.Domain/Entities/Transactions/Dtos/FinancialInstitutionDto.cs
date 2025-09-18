using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class FinancialInstitutionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsDefaultSource { get; set; }
    public int ClearingHouseId { get; set; }
    public string RoutingNumber { get; set; } = null!;
    public string TransitCode { get; set; } = null!;
    public string CheckDigit { get; set; } = null!;
    public FinancialInstitutionStatus Status { get; set; }
}



