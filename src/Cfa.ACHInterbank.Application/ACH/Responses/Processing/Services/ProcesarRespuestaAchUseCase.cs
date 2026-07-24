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
        var correlationId = string.IsNullOrWhiteSpace(command.CorrelationId)
            ? $"response-{hash[..Math.Min(16, hash.Length)]}"
            : command.CorrelationId.Trim();
        var clearingHouseId = await _mappingService.ResolveClearingHouseIdAsync(command.CodigoCamaraCompensacion, cancellationToken);
        if (!clearingHouseId.HasValue)
            return new ProcesarRespuestaAchResult(null, false, false, false, false, false,
                AchResponseProcessingStatus.ErrorFuncional, "La cámara compensadora no existe o está inactiva.", hash);

        var existing = await _responseRepository.FindByIdempotencyHashAsync(hash, cancellationToken);
        if (existing is not null)
        {
            existing.DuplicateReceiptCount++;
            existing.FechaActualizacion = now;
            existing.Version = Guid.NewGuid();
            await _responseRepository.AddAuditAsync(NewAudit(existing.Id, "DuplicateReceipt",
                existing.EstadoProcesamiento.ToString(), existing.EstadoProcesamiento.ToString(),
                "system:response-receiver", "Recepción idempotente duplicada.", correlationId, now), cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
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
            Id = Guid.NewGuid(), ClearingHouseId = clearingHouseId.Value, TipoRespuesta = command.TipoRespuesta, IdTransaccion = command.IdTransaccion,
            CodigoCamaraCompensacion = command.CodigoCamaraCompensacion.Trim().ToUpperInvariant(), CodigoEntidadOrigen = command.CodigoEntidadOrigen,
            CodigoEntidadDestino = command.CodigoEntidadDestino, CodigoEstadoExterno = command.CodigoEstadoExterno.Trim().ToUpperInvariant(),
            CodigoCausalExterna = string.IsNullOrWhiteSpace(command.CodigoCausalExterna) ? null : command.CodigoCausalExterna.Trim().ToUpperInvariant(),
            IdEstadoInterno = hom.IdEstadoInterno, IdEstadoServicioExterno = hom.IdEstadoServicioExterno, EstadoInternoNombre = hom.EstadoInternoNombre,
            CausalNormalizada = hom.CausalNormalizada, DescripcionCausal = hom.DescripcionCausalNormalizada ?? command.DescripcionCausalExterna,
            IdTransaccionServicioExterno = command.IdTransaccionServicioExterno, HashIdempotencia = hash,
            CanonicalPayloadHash = hash, OperationalDate = (command.FechaRecepcion ?? now).Date,
            AppliedMappingId = hom.MappingId > 0 ? hom.MappingId : null, MotivoNoHomologacion = hom.MotivoNoHomologacion,
            PermiteNotificacion = hom.PermiteNotificacion, CorrelationId = correlationId,
            FechaRecepcion = command.FechaRecepcion ?? now, FechaCreacion = now,
            EstadoProcesamiento = AchResponseProcessingStatus.Recibida, Version = Guid.NewGuid()
        };
        response.AuditEntries.Add(NewAudit(response.Id, "Received", null, AchResponseProcessingStatus.Recibida.ToString(),
            "system:response-receiver", "Respuesta recibida.", correlationId, now));

        AchResponseNotificationAttempt? attempt = null;
        DifferentialPrenotificationResponseProcessResult? prenotificationProcessing = null;

        if (command.TipoRespuesta == TipoRespuestaAch.Prenota
            && hom.ExisteHomologacion
            && _prenotificationResponseProcessor is not null)
        {
            prenotificationProcessing = await _prenotificationResponseProcessor.ProcessAsync(command, response, hom, cancellationToken);

            if (!prenotificationProcessing.Success)
            {
                response.PermiteNotificacion = false;
                var failedStatus = prenotificationProcessing.Duplicate
                    ? AchResponseProcessingStatus.Duplicada
                    : IsManualReview(prenotificationProcessing.ErrorCode)
                        ? AchResponseProcessingStatus.RequiereRevisionManual
                        : AchResponseProcessingStatus.ErrorFuncional;
                response.MotivoNoHomologacion = $"{prenotificationProcessing.ErrorCode}: {prenotificationProcessing.Message}".Trim();
                Transition(response, failedStatus, "PrenotificationProcessing",
                    response.MotivoNoHomologacion, correlationId, now);

                await _responseRepository.AddAsync(response, cancellationToken);
                try
                {
                    await _unitOfWork.CommitIdempotentAsync(cancellationToken);
                }
                catch (IdempotentWriteConflictException)
                {
                    return await ResolveConcurrentDuplicate(hash, correlationId, now, cancellationToken);
                }

                return new ProcesarRespuestaAchResult(response.Id, true, prenotificationProcessing.Duplicate, true, false, false, response.EstadoProcesamiento, response.MotivoNoHomologacion, hash);
            }
        }

        if (hom.ResolutionStatus == MappingResolutionStatus.Ambiguous)
        {
            Transition(response, AchResponseProcessingStatus.RequiereRevisionManual, "MappingAmbiguous",
                hom.MotivoNoHomologacion ?? "Mapping ambiguo.", correlationId, now);
            response.PermiteNotificacion = false;
        }
        else if (!hom.ExisteHomologacion)
        {
            Transition(response, AchResponseProcessingStatus.NoHomologada, "MappingNotFound",
                hom.MotivoNoHomologacion ?? "Mapping no encontrado.", correlationId, now);
            response.PermiteNotificacion = false;
        }
        else if (!hom.PermiteNotificacion)
        {
            Transition(response, AchResponseProcessingStatus.RequiereRevisionManual, "MappingApplied",
                "El mapping aplicado requiere revisión manual.", correlationId, now);
            response.PermiteNotificacion = false;
        }
        else
        {
            Transition(response, AchResponseProcessingStatus.Homologada, "MappingApplied",
                "Mapping aplicado de forma determinística.", correlationId, now);
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
        try
        {
            await _unitOfWork.CommitIdempotentAsync(cancellationToken);
        }
        catch (IdempotentWriteConflictException)
        {
            return await ResolveConcurrentDuplicate(hash, correlationId, now, cancellationToken);
        }

        return new ProcesarRespuestaAchResult(response.Id, true, false, hom.ExisteHomologacion, hom.PermiteNotificacion, attempt is not null, response.EstadoProcesamiento, hom.MotivoNoHomologacion, hash);
    }

    private async Task<ProcesarRespuestaAchResult> ResolveConcurrentDuplicate(string hash, string correlationId,
        DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _responseRepository.FindByIdempotencyHashAsync(hash, cancellationToken)
            ?? throw new InvalidOperationException("La identidad concurrente confirmada no pudo recuperarse.");
        existing.DuplicateReceiptCount++;
        existing.FechaActualizacion = now;
        existing.Version = Guid.NewGuid();
        await _responseRepository.AddAuditAsync(NewAudit(existing.Id, "DuplicateReceipt",
            existing.EstadoProcesamiento.ToString(), existing.EstadoProcesamiento.ToString(),
            "system:response-receiver", "Recepción concurrente duplicada.", correlationId, now), cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);
        return new ProcesarRespuestaAchResult(existing.Id, true, true, existing.IdEstadoInterno.HasValue,
            existing.PermiteNotificacion, false, AchResponseProcessingStatus.Duplicada, "Duplicada", hash);
    }

    private static bool IsManualReview(string? code)
        => code is "DIFFERENTIAL_RESPONSE_PRENOTIFICATION_NOT_FOUND"
            or "DIFFERENTIAL_RESPONSE_UNMATCHED"
            or "DIFFERENTIAL_RESPONSE_ALREADY_PROCESSED";

    private static void Transition(AchResponse response, AchResponseProcessingStatus target, string action,
        string reason, string correlationId, DateTime occurredAtUtc)
    {
        var previous = response.EstadoProcesamiento;
        AchResponseStatePolicy.EnsureTransition(previous, target, "system:response-processor", reason, correlationId);
        if (previous == target) return;
        response.EstadoProcesamiento = target;
        response.Version = Guid.NewGuid();
        response.AuditEntries.Add(NewAudit(response.Id, action, previous.ToString(), target.ToString(),
            "system:response-processor", reason, correlationId, occurredAtUtc));
    }

    private static AchResponseAudit NewAudit(Guid responseId, string action, string? previousState,
        string? newState, string actor, string reason, string correlationId, DateTime occurredAtUtc)
        => new()
        {
            EntityType = nameof(AchResponse), EntityId = responseId.ToString(), AchResponseId = responseId,
            Action = action, PreviousState = previousState, NewState = newState, Actor = actor,
            Reason = reason, CorrelationId = correlationId, OccurredAtUtc = occurredAtUtc
        };
}
