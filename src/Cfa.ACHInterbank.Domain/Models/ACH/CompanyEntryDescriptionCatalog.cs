namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CompanyEntryDescriptionCatalog
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StandardEntryClassCode { get; set; } = "PPD";
    public bool IsActive { get; set; } = true;
}
