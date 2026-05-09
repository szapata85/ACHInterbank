using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchResponseStatusMapping
{
    public int Id { get; set; }
    public string CodigoCamaraCompensacion { get; set; } = string.Empty;
    public TipoRespuestaAch TipoRespuesta { get; set; }
    public string CodigoEstadoExterno { get; set; } = string.Empty;
    public string? CodigoCausalExterna { get; set; }
    public int IdEstadoInterno { get; set; }
    public int IdEstadoServicioExterno { get; set; }
    public string EstadoInternoNombre { get; set; } = string.Empty;
    public string? CausalNormalizada { get; set; }
    public string? DescripcionCausalNormalizada { get; set; }
    public bool RequiereCausal { get; set; }
    public bool PermiteNotificacion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaInicioVigencia { get; set; }
    public DateTime? FechaFinVigencia { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
