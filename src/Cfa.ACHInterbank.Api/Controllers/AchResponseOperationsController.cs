using System.Security.Claims;
using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Responses.Operations;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
public sealed class AchResponseOperationsController : ControllerBase
{
    private readonly IAchResponseOperationsService _service;
    public AchResponseOperationsController(IAchResponseOperationsService service) => _service = service;

    /// <summary>Consulta un mapping operativo de respuesta ACH por identificador.</summary>
    /// <param name="id">Identificador numérico del mapping.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar mapping de estado de respuesta ACH")]
    [EndpointDescription("Consulta el mapping de estado identificado por id. Devuelve 404 con ProblemDetails cuando no existe; no modifica la configuración ni aplica reglas adicionales sobre el mapping recuperado.")]
    [HttpGet("api/ach/response-status-mappings/{id:int}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(AchResponseMappingModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping de estado ACH encontrado para el identificador solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un mapping de estado ACH con el identificador indicado.")]
    public async Task<IActionResult> GetMapping(int id, CancellationToken ct)
    {
        var item = await _service.GetMappingAsync(id, ct);
        return item is null ? NotFound(Problem("Mapping no encontrado.", 404)) : Ok(item);
    }

    /// <summary>Crea un mapping operativo para homologar estados de respuesta ACH.</summary>
    /// <param name="request">Datos del mapping, vigencia, prioridad, estado inicial y motivo de auditoría.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear mapping de estado de respuesta ACH")]
    [EndpointDescription("Crea un mapping con la cámara, tipo de respuesta, códigos externos, estados internos, prioridad y vigencia recibidos. El servicio valida cámara activa, ResponseType, campos obligatorios, rango de vigencia y solapamientos activos. X-Correlation-ID es opcional; si no se informa, el controller genera un identificador de correlación para la auditoría.")]
    [HttpPost("api/ach/response-status-mappings")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseMappingModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Mapping de estado ACH creado y ubicación canónica devuelta mediante CreatedAtAction.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El mapping, la cámara, el tipo de respuesta, la vigencia o el contexto de auditoría no cumplen las validaciones del servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La creación entra en conflicto con un mapping activo superpuesto o con una operación concurrente persistida.")]
    public Task<IActionResult> CreateMapping([FromBody] AchResponseMappingWriteRequest request, CancellationToken ct)
        => Execute(async () =>
        {
            var correlation = Correlation(null);
            var created = await _service.CreateMappingAsync(Map(request), Actor(), correlation, ct);
            return CreatedAtAction(nameof(GetMapping), new { id = created.Id }, created);
        });

    /// <summary>Actualiza un mapping operativo de respuesta ACH existente.</summary>
    /// <param name="id">Identificador numérico del mapping.</param>
    /// <param name="request">Datos completos del mapping y ExpectedVersion usada para control optimista.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar mapping de estado de respuesta ACH")]
    [EndpointDescription("Reemplaza los datos configurables del mapping identificado por id. ExpectedVersion es obligatoria para esta actualización; el servicio valida la versión vigente, la cámara activa, ResponseType, vigencia y ausencia de solapamientos activos. X-Correlation-ID es opcional y se usa para auditoría cuando está presente.")]
    [HttpPut("api/ach/response-status-mappings/{id:int}")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseMappingModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping de estado ACH actualizado con la configuración solicitada.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El mapping, la cámara, el tipo de respuesta, la vigencia o el contexto de auditoría no cumplen las validaciones del servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el mapping de estado ACH que se intenta actualizar.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ExpectedVersion no coincide, existe un solapamiento activo o la persistencia detecta concurrencia.")]
    public Task<IActionResult> UpdateMapping(int id, [FromBody] AchResponseMappingWriteRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.UpdateMappingAsync(id, Map(request), Actor(), Correlation(null), ct)));

    /// <summary>Activa un mapping de estado de respuesta ACH.</summary>
    /// <param name="id">Identificador numérico del mapping.</param>
    /// <param name="request">Versión esperada, motivo y correlación opcional de la transición.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Activar mapping de estado de respuesta ACH")]
    [EndpointDescription("Activa el mapping identificado por id usando ExpectedVersion para control optimista. Si ya está activo, el servicio devuelve el mapping sin repetir la transición; al activar valida además que no exista un mapping activo superpuesto para la misma clave y prioridad. CorrelationId del body prevalece sobre X-Correlation-ID; si ninguno existe, el controller genera uno.")]
    [HttpPost("api/ach/response-status-mappings/{id:int}/activate")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseMappingModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping de estado ACH activo; incluye el estado persistido y su versión vigente.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El motivo o el contexto requerido para auditar la transición no es válido.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el mapping de estado ACH que se intenta activar.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ExpectedVersion no coincide, la activación produciría un solapamiento activo o existe concurrencia de persistencia.")]
    public Task<IActionResult> ActivateMapping(int id, [FromBody] VersionedReasonRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.SetMappingActiveAsync(id, true, request.ExpectedVersion,
            request.Reason, Actor(), Correlation(request.CorrelationId), ct)));

    /// <summary>Desactiva un mapping de estado de respuesta ACH.</summary>
    /// <param name="id">Identificador numérico del mapping.</param>
    /// <param name="request">Versión esperada, motivo y correlación opcional de la transición.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Desactivar mapping de estado de respuesta ACH")]
    [EndpointDescription("Desactiva el mapping identificado por id usando ExpectedVersion para control optimista. Si ya está inactivo, el servicio devuelve el mapping sin repetir la transición. CorrelationId del body prevalece sobre X-Correlation-ID; si ninguno existe, el controller genera uno para la auditoría.")]
    [HttpPost("api/ach/response-status-mappings/{id:int}/deactivate")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseMappingModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping de estado ACH inactivo; incluye el estado persistido y su versión vigente.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El motivo o el contexto requerido para auditar la transición no es válido.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el mapping de estado ACH que se intenta desactivar.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ExpectedVersion no coincide o la persistencia detecta una modificación concurrente.")]
    public Task<IActionResult> DeactivateMapping(int id, [FromBody] VersionedReasonRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.SetMappingActiveAsync(id, false, request.ExpectedVersion,
            request.Reason, Actor(), Correlation(request.CorrelationId), ct)));

    /// <summary>Consulta el historial de auditoría de un mapping de respuesta ACH.</summary>
    /// <param name="id">Identificador numérico del mapping.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar auditoría de mapping de respuesta ACH")]
    [EndpointDescription("Devuelve los eventos de auditoría cuyo EntityType es AchResponseStatusMapping y cuyo EntityId corresponde al id solicitado, ordenados por fecha descendente. La ausencia de eventos se representa como una colección vacía.")]
    [HttpGet("api/ach/response-status-mappings/{id:int}/audit")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchResponseAuditModel>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Eventos de auditoría registrados para el mapping solicitado, posiblemente una colección vacía.")]
    public async Task<IActionResult> MappingAudit(int id, CancellationToken ct)
        => Ok(await _service.GetAuditAsync("AchResponseStatusMapping", id.ToString(), ct));

    /// <summary>Consulta el historial de auditoría de una respuesta ACH.</summary>
    /// <param name="id">Identificador de la respuesta ACH.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar auditoría de respuesta ACH")]
    [EndpointDescription("Devuelve los eventos de auditoría cuyo EntityType es AchResponse y cuyo EntityId corresponde al identificador solicitado, ordenados por fecha descendente. La ausencia de eventos se representa como una colección vacía.")]
    [HttpGet("api/ach/responses/{id:guid}/audit")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchResponseAuditModel>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Eventos de auditoría registrados para la respuesta ACH solicitada, posiblemente una colección vacía.")]
    public async Task<IActionResult> ResponseAudit(Guid id, CancellationToken ct)
        => Ok(await _service.GetAuditAsync("AchResponse", id.ToString(), ct));

    /// <summary>Lista respuestas ACH huérfanas para revisión operativa.</summary>
    /// <param name="clearingHouseId">Filtro opcional por identificador de cámara compensadora.</param>
    /// <param name="status">Filtro opcional por ResolutionStatus exacto, después de aplicar Trim.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar respuestas ACH huérfanas")]
    [EndpointDescription("Devuelve hasta 500 respuestas huérfanas, ordenadas por ReceivedAtUtc descendente. clearingHouseId y status son filtros opcionales; status se compara con ResolutionStatus después de eliminar espacios exteriores.")]
    [HttpGet("api/ach/responses/orphans")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchResponseOrphanModel>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Respuestas ACH huérfanas que cumplen los filtros solicitados, con un máximo de 500 elementos.")]
    public async Task<IActionResult> ListOrphans([FromQuery] int? clearingHouseId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _service.ListOrphansAsync(clearingHouseId, status, ct));

    /// <summary>Crea o recupera el registro huérfano asociado a una respuesta ACH.</summary>
    /// <param name="id">Identificador de la respuesta ACH origen.</param>
    /// <param name="request">Motivo, referencias candidatas y correlación opcional del registro huérfano.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Registrar respuesta ACH como huérfana")]
    [EndpointDescription("Si ya existe un registro huérfano para la respuesta indicada, lo devuelve sin crear otro. En caso contrario requiere la respuesta, conserva su cámara canónica, transiciona su estado a Huerfana y crea además un caso de conciliación abierto. CorrelationId del body prevalece sobre X-Correlation-ID y, si ambos faltan, el controller genera uno.")]
    [HttpPost("api/ach/responses/{id:guid}/orphan")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseOrphanModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Registro huérfano existente o recién creado para la respuesta ACH indicada.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El motivo o el contexto requerido por la operación no es válido.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la respuesta ACH que se intenta registrar como huérfana.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La respuesta carece de cámara canónica, su transición de estado no es válida o existe concurrencia de persistencia.")]
    public Task<IActionResult> CreateOrphan(Guid id, [FromBody] CreateOrphanRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.CreateOrphanAsync(id, request.Reason, request.CandidateReferences,
            Actor(), Correlation(request.CorrelationId), ct)));

    /// <summary>Inicia la revisión manual de una respuesta ACH huérfana.</summary>
    /// <param name="id">Identificador del registro huérfano.</param>
    /// <param name="request">Versión esperada, motivo y correlación opcional de la revisión.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Iniciar revisión de respuesta ACH huérfana")]
    [EndpointDescription("Usa ExpectedVersion para control optimista. Un huérfano ya InReview se devuelve sin repetir efectos; solo un registro Pending puede pasar a InReview y la respuesta asociada transiciona a EnRevision. CorrelationId del body prevalece sobre X-Correlation-ID.")]
    [HttpPost("api/ach/responses/orphans/{id:guid}/review/start")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseOrphanModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Registro huérfano en revisión o que ya se encontraba en revisión.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El motivo o el contexto requerido por la operación no es válido.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la respuesta huérfana indicada.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ExpectedVersion no coincide, la huérfana ya fue resuelta, la transición asociada no es válida o existe concurrencia.")]
    public Task<IActionResult> BeginReview(Guid id, [FromBody] VersionedReasonRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.BeginReviewAsync(id, request.ExpectedVersion, request.Reason,
            Actor(), Correlation(request.CorrelationId), ct)));

    /// <summary>Resuelve o rechaza manualmente una respuesta ACH huérfana.</summary>
    /// <param name="id">Identificador del registro huérfano.</param>
    /// <param name="request">Versión esperada, motivo, referencia funcional opcional, decisión de rechazo y correlación opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Resolver respuesta ACH huérfana")]
    [EndpointDescription("Solo resuelve registros InReview. Con Reject=true marca el huérfano como Rejected; con Reject=false exige FunctionalReference y verifica que identifique exactamente una transacción en la misma cámara antes de marcarlo Resolved. Un registro ya Resolved o Rejected se devuelve sin repetir efectos. CorrelationId del body prevalece sobre X-Correlation-ID.")]
    [HttpPost("api/ach/responses/orphans/{id:guid}/resolve")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseOrphanModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Respuesta huérfana resuelta, rechazada o ya finalizada previamente.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "Falta el motivo/contexto requerido o FunctionalReference cuando se solicita asociar en lugar de rechazar.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la respuesta huérfana indicada.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ExpectedVersion no coincide, la revisión no fue iniciada, la referencia no identifica una única transacción compatible o existe concurrencia.")]
    public Task<IActionResult> ResolveOrphan(Guid id, [FromBody] ResolveOrphanRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.ResolveOrphanAsync(id,
            new ManualResolutionCommand(request.ExpectedVersion, request.Reason, request.FunctionalReference,
                request.Reject, Correlation(request.CorrelationId)), Actor(), ct)));

    /// <summary>Solicita el reproceso de una respuesta ACH.</summary>
    /// <param name="id">Identificador de la respuesta ACH a reprocesar.</param>
    /// <param name="request">CommandId idempotente, versión esperada, motivo y correlación opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Solicitar reproceso de respuesta ACH")]
    [EndpointDescription("CommandId identifica idempotentemente la solicitud: si ya existe un intento con el mismo CommandId, se devuelve ese intento. Para una solicitud nueva, el servicio requiere la respuesta, verifica ExpectedVersion, impide otro intento Pending o Running, transiciona la respuesta a PendienteReproceso y crea un intento Pending. CorrelationId del body prevalece sobre X-Correlation-ID.")]
    [HttpPost("api/ach/responses/{id:guid}/reprocess")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(AchResponseReprocessModel), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status202Accepted, "Intento de reproceso solicitado o intento existente recuperado por CommandId.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El motivo o el contexto requerido por la solicitud de reproceso no es válido.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la respuesta ACH que se intenta reprocesar.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ExpectedVersion no coincide, ya existe un reproceso activo, la transición no es válida o existe concurrencia.")]
    public Task<IActionResult> Reprocess(Guid id, [FromBody] ReprocessResponseRequest request, CancellationToken ct)
        => Execute(async () => Accepted(await _service.RequestReprocessAsync(id,
            new ReprocessCommand(request.CommandId, request.ExpectedVersion, request.Reason,
                Correlation(request.CorrelationId)), Actor(), ct)));

    /// <summary>Lista los intentos de reproceso de una respuesta ACH.</summary>
    /// <param name="id">Identificador de la respuesta ACH.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar intentos de reproceso de respuesta ACH")]
    [EndpointDescription("Devuelve todos los intentos de reproceso asociados a la respuesta, ordenados por AttemptNumber descendente. Si la respuesta no tiene intentos, devuelve una colección vacía; la consulta no valida por separado la existencia de la respuesta.")]
    [HttpGet("api/ach/responses/{id:guid}/reprocess-attempts")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchResponseReprocessModel>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Intentos de reproceso asociados a la respuesta ACH, ordenados del intento más reciente al más antiguo.")]
    public async Task<IActionResult> ReprocessAttempts(Guid id, CancellationToken ct)
        => Ok(await _service.ListReprocessAttemptsAsync(id, ct));

    /// <summary>Consulta un intento de reproceso concreto de una respuesta ACH.</summary>
    /// <param name="id">Identificador de la respuesta ACH.</param>
    /// <param name="attemptId">Identificador numérico del intento de reproceso.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar intento de reproceso de respuesta ACH")]
    [EndpointDescription("Busca el intento por la combinación AchResponseId e identificador del intento. Devuelve 404 con ProblemDetails cuando no existe esa combinación.")]
    [HttpGet("api/ach/responses/{id:guid}/reprocess-attempts/{attemptId:long}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(AchResponseReprocessModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Intento de reproceso encontrado para la respuesta ACH indicada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el intento de reproceso indicado para esa respuesta ACH.")]
    public async Task<IActionResult> ReprocessAttempt(Guid id, long attemptId, CancellationToken ct)
    {
        var item = await _service.GetReprocessAttemptAsync(id, attemptId, ct);
        return item is null ? NotFound(Problem("Intento de reproceso no encontrado.", 404)) : Ok(item);
    }

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (AchResponseNotFoundException ex) { return NotFound(Problem(ex.Message, 404)); }
        catch (AchResponseConflictException ex)
        {
            var problem = Problem(ex.Message, 409);
            if (ex.CurrentVersion.HasValue) problem.Extensions["currentVersion"] = ex.CurrentVersion.Value;
            return Conflict(problem);
        }
        catch (AchResponseOperationException ex) { return BadRequest(Problem(ex.Message, 400)); }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message, 400)); }
        catch (InvalidOperationException ex) { return Conflict(Problem(ex.Message, 409)); }
    }

    private string Actor() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "authenticated-user";
    private string Correlation(string? supplied) => !string.IsNullOrWhiteSpace(supplied)
        ? supplied.Trim()
        : Request.Headers.TryGetValue("X-Correlation-ID", out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString().Trim() : Guid.NewGuid().ToString("N");
    private static ProblemDetails Problem(string detail, int status) => new()
        { Title = status == 409 ? "Conflicto de concurrencia" : "Operación de respuestas ACH", Detail = detail, Status = status };
    private static AchResponseMappingCommand Map(AchResponseMappingWriteRequest x)
        => new(x.ClearingHouseId, x.ResponseType, x.ExternalCode, x.ExternalCause, x.InternalStatusId,
            x.ExternalServiceStatusId, x.InternalStatusName, x.NormalizedCause, x.NormalizedDescription,
            x.RequiresCause, x.AllowsNotification, x.Priority, x.EffectiveFrom, x.EffectiveTo,
            x.IsActive, x.ExpectedVersion, x.Reason);
}
