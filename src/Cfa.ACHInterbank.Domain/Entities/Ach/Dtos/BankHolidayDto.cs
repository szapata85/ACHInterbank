namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class BankHolidayDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "CO";
}
