namespace Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;

public class ExternalFileNameReservation
{
    public long Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string ScopeCode { get; set; } = string.Empty;
    public DateOnly OperationalDate { get; set; }
    public string IdempotencyKeyHash { get; set; } = string.Empty;
    public string RequestFingerprintHash { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string? FileIdModifier { get; set; }
    public string? ExternalFileName { get; set; }
    public string Status { get; set; } = "Reserved";
    public DateTime ReservedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime LastAccessedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "system";
    public byte[] RowVersion { get; set; } = [];
}
