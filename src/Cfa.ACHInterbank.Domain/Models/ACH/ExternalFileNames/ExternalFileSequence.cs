namespace Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;

public class ExternalFileSequence
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string ScopeCode { get; set; } = string.Empty;
    public DateOnly SequenceDate { get; set; }
    public int LastValue { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = [];
}
