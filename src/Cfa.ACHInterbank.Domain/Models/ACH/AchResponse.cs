using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchResponse
{
    public Guid Id { get; set; }
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
    public AchResponseProcessingStatus EstadoProcesamiento { get; set; }
    public string? MotivoNoHomologacion { get; set; }
    public bool PermiteNotificacion { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    public ICollection<AchResponseNotificationAttempt> NotificationAttempts { get; set; } = new List<AchResponseNotificationAttempt>();
}
