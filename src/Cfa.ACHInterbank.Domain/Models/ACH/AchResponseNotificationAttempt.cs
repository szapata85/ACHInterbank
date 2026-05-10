using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchResponseNotificationAttempt
{
    public long Id { get; set; }
    public Guid AchResponseId { get; set; }
    public AchResponse AchResponse { get; set; } = null!;
    public int NumeroIntento { get; set; }
    public AchResponseNotificationStatus EstadoNotificacion { get; set; }
    public int IdCanal { get; set; }
    public string NombreCanal { get; set; } = string.Empty;
    public string IdTransaccion { get; set; } = string.Empty;
    public int IdEstado { get; set; }
    public string? Causal { get; set; }
    public int IdTransaccionServicioExterno { get; set; }
    public string? DescripcionCausal { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public bool? ExisteError { get; set; }
    public string? CodigoError { get; set; }
    public string? DescripcionError { get; set; }
    public string? ErrorTecnico { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
}
