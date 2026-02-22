namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class UpdateInstitutionClearingHousePreferenceDto
{
    public bool? IsDefault { get; set; }
    public int? Priority { get; set; }
    public bool? IsActive { get; set; }
}
