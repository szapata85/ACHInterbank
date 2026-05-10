namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

public sealed record HomologarRespuestaAchResult(
    bool ExisteHomologacion,
    bool PermiteNotificacion,
    int? IdEstadoInterno,
    int? IdEstadoServicioExterno,
    string? EstadoInternoNombre,
    string? CausalNormalizada,
    string? DescripcionCausalNormalizada,
    string? MotivoNoHomologacion)
{
    public static HomologarRespuestaAchResult Success(
        bool permiteNotificacion,
        int idEstadoInterno,
        int idEstadoServicioExterno,
        string estadoInternoNombre,
        string? causalNormalizada,
        string? descripcionCausalNormalizada)
        => new(true, permiteNotificacion, idEstadoInterno, idEstadoServicioExterno, estadoInternoNombre, causalNormalizada, descripcionCausalNormalizada, null);

    public static HomologarRespuestaAchResult NotFound(string motivo)
        => new(false, false, null, null, null, null, null, motivo);

    public static HomologarRespuestaAchResult NotAllowed(
        int idEstadoInterno,
        int idEstadoServicioExterno,
        string estadoInternoNombre,
        string? causalNormalizada,
        string? descripcionCausalNormalizada,
        string motivo)
        => new(true, false, idEstadoInterno, idEstadoServicioExterno, estadoInternoNombre, causalNormalizada, descripcionCausalNormalizada, motivo);
}
