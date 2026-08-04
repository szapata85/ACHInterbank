namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class ClearingHouseSpecialDateDto
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string? ClearingHouseName { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsNationalHoliday { get; set; }
    public string? CalendarWarning { get; set; }
}
