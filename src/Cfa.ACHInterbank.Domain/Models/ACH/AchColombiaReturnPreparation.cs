namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>
/// Reserva persistente de una candidata ACH Colombia. No constituye un archivo generado,
/// una devolución enviada ni un asiento financiero.
/// </summary>
public sealed class AchColombiaReturnPreparation
{
    public long Id { get; set; }
    public int OriginalTransactionId { get; set; }
    public AchTransaction OriginalTransaction { get; set; } = null!;
    public Guid SourceEventId { get; set; }
    public IncomingNachaProcessingEvent SourceEvent { get; set; } = null!;
    public string OriginalCycleId { get; set; } = string.Empty;
    public string? SuggestedReturnCycleId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Status { get; set; } = "PREPARADA_SIN_ENVIO";
    public DateTime PreparedAtUtc { get; set; }
}
