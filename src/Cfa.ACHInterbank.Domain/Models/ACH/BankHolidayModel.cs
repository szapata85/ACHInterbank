namespace Cfa.ACHInterbank.Domain.Models.ACH;

//public class BankHoliday
//{
//    public int Id { get; set; }
//    public DateTime Date { get; set; }
//    public string? Description { get; set; }
//}

public class BankHolidayModel
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }   // mejor que DateTime para festivos
    public string Description { get; set; } = default!;
    public string CountryCode { get; set; } = "CO"; // opcional para multi-país
}
