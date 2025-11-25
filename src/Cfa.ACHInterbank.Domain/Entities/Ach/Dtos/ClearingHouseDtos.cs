namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class ClearingHouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string OriginCode { get; set; } = null!;
    public string? HolidayStrategy { get; set; }
}
