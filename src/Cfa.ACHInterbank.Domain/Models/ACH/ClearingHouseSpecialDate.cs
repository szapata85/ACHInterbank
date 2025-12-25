namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouseSpecialDate
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;

    public ClearingHouse ClearingHouse { get; set; } = null!;
}
