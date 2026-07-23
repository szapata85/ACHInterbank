namespace Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;

public sealed record HomologarRespuestaAchResult(
    MappingResolutionStatus ResolutionStatus,
    int? MappingId,
    bool ExisteHomologacion,
    bool PermiteNotificacion,
    int? IdEstadoInterno,
    int? IdEstadoServicioExterno,
    string? EstadoInternoNombre,
    string? CausalNormalizada,
    string? DescripcionCausalNormalizada,
    string? MotivoNoHomologacion)
{
    public static HomologarRespuestaAchResult Success(bool permiteNotificacion, int idEstadoInterno,
        int idEstadoServicioExterno, string estadoInternoNombre, string? causalNormalizada,
        string? descripcionCausalNormalizada, int mappingId = 0)
        => new(MappingResolutionStatus.Matched, mappingId, true, permiteNotificacion, idEstadoInterno,
            idEstadoServicioExterno, estadoInternoNombre, causalNormalizada, descripcionCausalNormalizada, null);

    public static HomologarRespuestaAchResult NotFound(string motivo)
        => new(MappingResolutionStatus.NoMatch, null, false, false, null, null, null, null, null, motivo);

    public static HomologarRespuestaAchResult Ambiguous(string motivo)
        => new(MappingResolutionStatus.Ambiguous, null, false, false, null, null, null, null, null, motivo);

    public static HomologarRespuestaAchResult NotAllowed(int idEstadoInterno, int idEstadoServicioExterno,
        string estadoInternoNombre, string? causalNormalizada, string? descripcionCausalNormalizada,
        string motivo, int mappingId = 0)
        => new(MappingResolutionStatus.Matched, mappingId, true, false, idEstadoInterno,
            idEstadoServicioExterno, estadoInternoNombre, causalNormalizada, descripcionCausalNormalizada, motivo);
}

public enum MappingResolutionStatus
{
    Matched = 1,
    NoMatch = 2,
    Ambiguous = 3
}
