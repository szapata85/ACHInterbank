namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>
/// I03-CENIT: reserva informativa de una posible devolución, sin autorización financiera,
/// archivo NACHA-M, operación de compensación ni transmisión. Es exclusiva de CENIT.
/// </summary>
public sealed class CenitReturnPreparation
{
    public Guid Id { get; set; }
    public int OriginalTransactionId { get; set; }
    public AchTransaction OriginalTransaction { get; set; } = null!;
    public Guid SourceExecutionId { get; set; }
    public IncomingNachaIntegrationExecution SourceExecution { get; set; } = null!;
    public Guid SourceQueueId { get; set; }
    public string OriginalCycleId { get; set; } = string.Empty;
    public string SuggestedReturnCycleId { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public DateTime OriginalValueDate { get; set; }
    public DateTime SuggestedValueDate { get; set; }
    public DateTime PreparedAtUtc { get; set; }
    public string Status { get; set; } = "PREPARADA_SIN_ENVIO";
    // I04: sólo decisiones humanas; ningún campo habilita generación o transporte.
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? DecisionJustification { get; set; }
    public string? DecisionEvidenceReference { get; set; }
    // I05: artefacto cifrado para revisión local, sin publicación ni transporte.
    public string? ProtectedArtifactContent { get; set; }
    public string? ArtifactSha256 { get; set; }
    public string? ArtifactFileName { get; set; }
    public string? ArtifactTraceNumber { get; set; }
    public string? ArtifactProfileCode { get; set; }
    public int? ArtifactRecordCount { get; set; }
    public DateTime? GeneratedAtUtc { get; set; }
    public string? GeneratedByUserId { get; set; }
}
