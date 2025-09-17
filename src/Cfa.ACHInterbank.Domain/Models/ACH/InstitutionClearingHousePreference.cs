namespace Cfa.ACHInterbank.Domain.Models.ACH;

// Domain
public class InstitutionClearingHousePreference
{
    public int Id { get; set; }

    public int FinancialInstitutionId { get; set; }
    public FinancialInstitution FinancialInstitution { get; set; } = null!;

    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;

    public bool IsDefault { get; set; } = false; // puede haber varias en true
    public int Priority { get; set; } = 1;       // 1 = mayor prioridad
}


