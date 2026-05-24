using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Validation;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Services;

public sealed class ProcesarRespuestaAchUseCase : IProcesarRespuestaAchUseCase
{
    private readonly ProcesarRespuestaAchCommandValidator _validator;
    private readonly IAchResponseIdempotencyHashService _hashService;
    private readonly IAchResponseRepository _responseRepository;
    private readonly IAchResponseNotificationAttemptRepository _attemptRepository;
    private readonly IRespuestaAchStatusMappingService _mappingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDifferentialPrenotificationResponseProcessor? _prenotificationResponseProcessor;

    public ProcesarRespuestaAchUseCase(
        ProcesarRespuestaAchCommandValidator validator,
        IAchResponseIdempotencyHashService hashService,
        IAchResponseRepository responseRepository,
        IAchResponseNotificationAttemptRepository attemptRepository,
        IRespuestaAchStatusMappingService mappingService,
        IUnitOfWork unitOfWork,
        IDifferentialPrenotificationResponseProcessor? prenotificationResponseProcessor = null)
    {
        _validator = validator;
        _hashService = hashService;
        _responseRepository = responseRepository;
        _attemptRepository = attemptRepository;
        _mappingService = mappingService;
        _unitOfWork = unitOfWork;
        _prenotificationResponseProcessor = prenotificationResponseProcessor;
    }

    public async Task<ProcesarRespuestaAchResult> ExecuteAsync(ProcesarRespuestaAchCommand command, CancellationToken cancellationToken = default)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
        {
            return new ProcesarRespuestaAchResult(null, false, false, false, false, false, AchResponseProcessingStatus.ErrorFuncional, string.Join(";", errors), null);
        }

        var now = DateTime.UtcNow;
        var hash = _hashService.BuildHash(command);
        var existing = await _responseRepository.FindByIdempotencyHashAsync(hash, cancellationToken);
        if (existing is not null)
        {
            return new ProcesarRespuestaAchResult(existing.Id, true, true, existing.IdEstadoInterno.HasValue, existing.PermiteNotificacion, false, AchResponseProcessingStatus.Duplicada, "Duplicada", hash);
        }

        var hom = await _mappingService.HomologarAsync(new HomologarRespuestaAchRequest(
            command.CodigoCamaraCompensacion,
            command.TipoRespuesta,
            command.CodigoEstadoExterno,
            command.CodigoCausalExterna,
            command.FechaRecepcion ?? now), cancellationToken);

        var response = new AchResponse
        {
            Id = Guid.NewGuid(), TipoRespuesta = command.TipoRespuesta, IdTransaccion = command.IdTransaccion,
            CodigoCamaraCompensacion = command.CodigoCamaraCompensacion.Trim().ToUpperInvariant(), CodigoEntidadOrigen = command.CodigoEntidadOrigen,
            CodigoEntidadDestino = command.CodigoEntidadDestino, CodigoEstadoExterno = command.CodigoEstadoExterno.Trim().ToUpperInvariant(),
            CodigoCausalExterna = string.IsNullOrWhiteSpace(command.CodigoCausalExterna) ? null : command.CodigoCausalExterna.Trim().ToUpperInvariant(),
            IdEstadoInterno = hom.IdEstadoInterno, IdEstadoServicioExterno = hom.IdEstadoServicioExterno, EstadoInternoNombre = hom.EstadoInternoNombre,
            CausalNormalizada = hom.CausalNormalizada, DescripcionCausal = hom.DescripcionCausalNormalizada ?? command.DescripcionCausalExterna,
            IdTransaccionServicioExterno = command.IdTransaccionServicioExterno, HashIdempotencia = hash, MotivoNoHomologacion = hom.MotivoNoHomologacion,
            PermiteNotificacion = hom.PermiteNotificacion, CorrelationId = command.CorrelationId, FechaRecepcion = command.FechaRecepcion ?? now, FechaCreacion = now
        };

        AchResponseNotificationAttempt? attempt = null;
        DifferentialPrenotificationResponseProcessResult? prenotificationProcessing = null;

        if (command.TipoRespuesta == TipoRespuestaAch.Prenota
            && hom.ExisteHomologacion
            && _prenotificationResponseProcessor is not null)
        {
            prenotificationProcessing = await _prenotificationResponseProcessor.ProcessAsync(command, response, hom, cancellationToken);
            response.PermiteNotificacion = false;

            if (!prenotificationProcessing.Success)
            {
                response.EstadoProcesamiento = prenotificationProcessing.Duplicate
                    ? AchResponseProcessingStatus.Duplicada
                    : IsManualReview(prenotificationProcessing.ErrorCode)
                        ? AchResponseProcessingStatus.RequiereRevisionManual
                        : AchResponseProcessingStatus.ErrorFuncional;
                response.MotivoNoHomologacion = $"{prenotificationProcessing.ErrorCode}: {prenotificationProcessing.Message}".Trim();

                await _responseRepository.AddAsync(response, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return new ProcesarRespuestaAchResult(response.Id, true, prenotificationProcessing.Duplicate, true, false, false, response.EstadoProcesamiento, response.MotivoNoHomologacion, hash);
            }
        }

        if (!hom.ExisteHomologacion)
        {
            response.EstadoProcesamiento = AchResponseProcessingStatus.NoHomologada;
            response.PermiteNotificacion = false;
        }
        else if (!hom.PermiteNotificacion)
        {
            response.EstadoProcesamiento = AchResponseProcessingStatus.RequiereRevisionManual;
            response.PermiteNotificacion = false;
        }
        else if (prenotificationProcessing?.Success == true)
        {
            response.EstadoProcesamiento = AchResponseProcessingStatus.Notificada;
            response.PermiteNotificacion = false;
        }
        else
        {
            response.EstadoProcesamiento = AchResponseProcessingStatus.Homologada;
            attempt = new AchResponseNotificationAttempt
            {
                AchResponseId = response.Id, NumeroIntento = 1, EstadoNotificacion = AchResponseNotificationStatus.Pendiente,
                IdCanal = command.IdCanal, NombreCanal = command.NombreCanal, IdTransaccion = command.IdTransaccion,
                IdEstado = hom.IdEstadoServicioExterno ?? 0, Causal = hom.CausalNormalizada ?? response.CodigoCausalExterna,
                IdTransaccionServicioExterno = command.IdTransaccionServicioExterno,
                DescripcionCausal = hom.DescripcionCausalNormalizada ?? command.DescripcionCausalExterna, FechaCreacion = now
            };
        }

        await _responseRepository.AddAsync(response, cancellationToken);
        if (attempt is not null) await _attemptRepository.AddAsync(attempt, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new ProcesarRespuestaAchResult(response.Id, true, false, hom.ExisteHomologacion, hom.PermiteNotificacion, attempt is not null, response.EstadoProcesamiento, hom.MotivoNoHomologacion, hash);
    }

    private static bool IsManualReview(string? code)
        => code is "DIFFERENTIAL_RESPONSE_PRENOTIFICATION_NOT_FOUND"
            or "DIFFERENTIAL_RESPONSE_UNMATCHED"
            or "DIFFERENTIAL_RESPONSE_ALREADY_PROCESSED";
}
