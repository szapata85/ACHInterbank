namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class BankHolidayDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CommemorativeDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "CO";
    public string? RuleCode { get; set; }
    public string? RuleKind { get; set; }
    public bool IsSystemGenerated { get; set; }
    public string? LegalOrigin { get; set; }
    public int? EffectiveFromYear { get; set; }
    public bool WasMoved => CommemorativeDate.HasValue && CommemorativeDate.Value.Date != Date.Date;
}
