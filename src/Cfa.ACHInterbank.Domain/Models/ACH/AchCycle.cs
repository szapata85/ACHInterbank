namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchCycle
{
    public int Id { get; set; }
    public string CycleName { get; set; } = null!;
    public DateTime ProcessingDate { get; set; }
    public TimeSpan CutoffTime { get; set; }
    public bool RescheduleOnHoliday { get; set; }

    public int ClearingHouseId { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }

    public ICollection<AchTransaction> Transactions { get; set; } = new List<AchTransaction>();
}

