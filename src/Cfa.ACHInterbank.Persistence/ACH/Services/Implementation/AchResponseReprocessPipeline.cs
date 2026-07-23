using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchResponseReprocessPipeline : IAchResponseReprocessPipeline
{
    private readonly AchDbContext _db;
    private readonly IRespuestaAchStatusMappingService _mapping;

    public AchResponseReprocessPipeline(AchDbContext db, IRespuestaAchStatusMappingService mapping)
    {
        _db = db;
        _mapping = mapping;
    }

    public async Task<AchResponseReprocessExecutionResult> ExecuteAsync(Guid responseId, long attemptId,
        CancellationToken cancellationToken = default)
    {
        var response = await _db.AchResponses.Include(x => x.NotificationAttempts)
            .SingleOrDefaultAsync(x => x.Id == responseId, cancellationToken);
        if (response is null)
            return new(AchResponseReprocessResultCode.CorrelationNotFound, "La respuesta persistida no existe.");
        if (string.IsNullOrWhiteSpace(response.CorrelationId))
            return new(AchResponseReprocessResultCode.CorrelationNotFound, "La respuesta no tiene correlation ID reutilizable.");

        // A confirmed effect is a satisfactory terminal result; never replay it.
        if (response.NotificationAttempts.Any(x => x.EstadoNotificacion == AchResponseNotificationStatus.Exitosa))
            return new(AchResponseReprocessResultCode.AlreadyApplied, "Ya existe un efecto de notificación confirmado.");

        var mapping = await _mapping.HomologarAsync(new HomologarRespuestaAchRequest(
            response.CodigoCamaraCompensacion, response.TipoRespuesta, response.CodigoEstadoExterno,
            response.CodigoCausalExterna, response.OperationalDate), cancellationToken);
        if (mapping.ResolutionStatus == MappingResolutionStatus.Ambiguous)
            return new(AchResponseReprocessResultCode.MappingAmbiguous,
                "El mapping vigente es ambiguo; requiere revisión manual.");
        if (!mapping.ExisteHomologacion)
            return new(AchResponseReprocessResultCode.MappingNotFound,
                "No existe mapping vigente; requiere revisión manual.");
        if (!mapping.PermiteNotificacion)
            return new(AchResponseReprocessResultCode.MissingOperationalData,
                "El mapping vigente no permite automatización; requiere revisión manual.");

        response.IdEstadoInterno = mapping.IdEstadoInterno;
        response.IdEstadoServicioExterno = mapping.IdEstadoServicioExterno;
        response.EstadoInternoNombre = mapping.EstadoInternoNombre;
        response.CausalNormalizada = mapping.CausalNormalizada;
        response.DescripcionCausal = mapping.DescripcionCausalNormalizada ?? response.DescripcionCausal;
        response.AppliedMappingId = mapping.MappingId;
        response.MotivoNoHomologacion = null;
        response.PermiteNotificacion = true;
        response.FechaActualizacion = DateTime.UtcNow;

        // A reprocess never invents channel data or creates a second external dispatch.
        if (response.NotificationAttempts.Count == 0)
            return new(AchResponseReprocessResultCode.MissingOperationalData,
                "Falta un checkpoint de notificación con datos operacionales verificables.");

        if (response.NotificationAttempts.Any(x => x.EstadoNotificacion == AchResponseNotificationStatus.Pendiente))
            return new(AchResponseReprocessResultCode.MissingOperationalData,
                "Existe una etapa de notificación pendiente sin evidencia de ejecución funcional.");

        await _db.SaveChangesAsync(cancellationToken);
        return new(AchResponseReprocessResultCode.Completed, "La rehomologación no tiene etapas funcionales pendientes.");
    }
}
