namespace Cfa.ACHInterbank.Application.ACH.Models;

public class CompanyEntryDescriptionAdminDto
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StandardEntryClassCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CompanyEntryDescriptionUpsertRequest
{
    public string? Term { get; set; }
    public string? Description { get; set; }
    public string? StandardEntryClassCode { get; set; }
    public bool IsActive { get; set; } = true;
}
