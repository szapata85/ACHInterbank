using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchResponse
{
    public Guid Id { get; set; }
    public int? ClearingHouseId { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }
    public int? AchTransactionId { get; set; }
    public AchTransaction? AchTransaction { get; set; }
    public AchResponseCorrelationStatus CorrelationStatus { get; set; } = AchResponseCorrelationStatus.Unknown;
    public string? CorrelationCriterion { get; set; }
    public TipoRespuestaAch TipoRespuesta { get; set; }
    public string IdTransaccion { get; set; } = string.Empty;
    public string CodigoCamaraCompensacion { get; set; } = string.Empty;
    public string? CodigoEntidadOrigen { get; set; }
    public string? CodigoEntidadDestino { get; set; }
    public string CodigoEstadoExterno { get; set; } = string.Empty;
    public string? CodigoCausalExterna { get; set; }
    public int? IdEstadoInterno { get; set; }
    public int? IdEstadoServicioExterno { get; set; }
    public string? EstadoInternoNombre { get; set; }
    public string? CausalNormalizada { get; set; }
    public string? DescripcionCausal { get; set; }
    public int IdTransaccionServicioExterno { get; set; }
    public string HashIdempotencia { get; set; } = string.Empty;
    public string CanonicalPayloadHash { get; set; } = string.Empty;
    public DateTime OperationalDate { get; set; }
    public int? AppliedMappingId { get; set; }
    public AchResponseStatusMapping? AppliedMapping { get; set; }
    public int DuplicateReceiptCount { get; set; }
    public Guid Version { get; set; }
    public AchResponseProcessingStatus EstadoProcesamiento { get; set; }
    public string? MotivoNoHomologacion { get; set; }
    public bool PermiteNotificacion { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<AchResponseNotificationAttempt> NotificationAttempts { get; set; } = new List<AchResponseNotificationAttempt>();
    public ICollection<AchResponseAudit> AuditEntries { get; set; } = new List<AchResponseAudit>();
    public AchResponseOrphan? Orphan { get; set; }
}
