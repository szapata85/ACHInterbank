namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ClearingHouseConfig
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string HolidayStrategy { get; set; } // e.g., "Colombian", "US", etc.

    public ClearingHouse? ClearingHouse { get; set; }
}

