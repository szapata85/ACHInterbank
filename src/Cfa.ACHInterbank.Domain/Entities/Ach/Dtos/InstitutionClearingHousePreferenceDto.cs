namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class InstitutionClearingHousePreferenceDto
{
    public int Id { get; set; }
    public int FinancialInstitutionId { get; set; }
    public string? FinancialInstitutionName { get; set; }
    public int ClearingHouseId { get; set; }
    public string? ClearingHouseName { get; set; }
    public bool IsDefault { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}
