namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class CompanyEntryDescriptionDto
{
    public int Id { get; set; }
    public string Term { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StandardEntryClassCode { get; set; } = string.Empty;
}
