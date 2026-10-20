using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/responses")]
[Authorize]
public class AchResponsesController : ControllerBase
{
    /// <summary>
    /// Procesa una respuesta ACH recibida y devuelve el resultado de su procesamiento.
    /// </summary>
    /// <param name="request">Datos de la respuesta recibida, cámara, estado externo, canal e identificadores de correlación.</param>
    /// <param name="validator">Validador estructural de la solicitud pública.</param>
    /// <param name="mapper">Mapeador entre el contrato HTTP y el comando de aplicación.</param>
    /// <param name="useCase">Caso de uso que procesa la respuesta.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Procesar respuesta ACH")]
    [EndpointDescription("Valida los campos requeridos del contrato y devuelve 400 con ValidationProblemDetails cuando la solicitud es inválida. Para una solicitud válida ejecuta el caso de uso de procesamiento y devuelve ProcesarRespuestaAchResponse: 200 cuando Procesada es true y 422 cuando el caso de uso devuelve Procesada en false.")]
    [HttpPost("process")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(ProcesarRespuestaAchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProcesarRespuestaAchResponse), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Respuesta ACH procesada; el cuerpo informa el estado y los indicadores obtenidos por el caso de uso.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud no cumple las validaciones estructurales del controller.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "El caso de uso devolvió la respuesta como no procesada y expone el resultado funcional en el mismo contrato de respuesta.")]
    public async Task<IActionResult> Process([FromBody] ProcesarRespuestaAchRequest request,
        [FromServices] ProcesarRespuestaAchRequestValidator validator,
        [FromServices] ProcesarRespuestaAchApiMapper mapper,
        [FromServices] IProcesarRespuestaAchUseCase useCase, CancellationToken ct)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return BadRequest(new ValidationProblemDetails(errors.GroupBy(x => x)
                .ToDictionary(g => g.Key, g => new[] { g.Key })));
        var result = await useCase.ExecuteAsync(mapper.MapRequest(request), ct);
        var response = mapper.MapResponse(result);
        return result.Procesada ? Ok(response) : UnprocessableEntity(response);
    }

    /// <summary>
    /// Ejecuta el intento de notificación asociado a una respuesta ACH.
    /// </summary>
    /// <param name="request">Identificador del intento de notificación y correlación opcional.</param>
    /// <param name="validator">Validador estructural de la solicitud pública.</param>
    /// <param name="mapper">Mapeador del contrato de notificación.</param>
    /// <param name="useCase">Caso de uso que procesa el intento de notificación.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Enviar notificación de respuesta ACH")]
    [EndpointDescription("Requiere NotificationAttemptId mayor que cero. Devuelve 404 sin cuerpo cuando el caso de uso indica que el intento no fue encontrado; cuando existe, devuelve 200 con NotificarRespuestaAchResponse incluso si el resultado registra un error funcional o técnico. La correlación opcional se recibe en el body, no como header contractual de esta operación.")]
    [HttpPost("notifications/send")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [ProducesResponseType(typeof(NotificarRespuestaAchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Intento de notificación encontrado y procesado; el cuerpo refleja su resultado funcional o técnico.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El identificador del intento de notificación no es válido.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el intento de notificación indicado; la respuesta no contiene cuerpo.")]
    public async Task<IActionResult> SendNotification([FromBody] NotificarRespuestaAchRequest request,
        [FromServices] NotificarRespuestaAchRequestValidator validator,
        [FromServices] NotificarRespuestaAchApiMapper mapper,
        [FromServices] INotificarRespuestaAchUseCase useCase, CancellationToken ct)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0)
            return BadRequest(new ValidationProblemDetails(errors.GroupBy(x => x)
                .ToDictionary(g => g.Key, g => new[] { g.Key })));
        var result = await useCase.ExecuteAsync(mapper.MapRequest(request), ct);
        return result.Encontrada ? Ok(mapper.MapResponse(result)) : NotFound();
    }

    /// <summary>
    /// Busca respuestas ACH usando filtros opcionales y paginación.
    /// </summary>
    /// <param name="request">Filtros por fechas, tipo, transacción, cámara, entidades, estado, correlación y parámetros de página.</param>
    /// <param name="mapper">Mapeador del contrato de consulta.</param>
    /// <param name="repository">Repositorio de lectura de respuestas ACH.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar respuestas ACH")]
    [EndpointDescription("Devuelve respuestas ordenadas por FechaRecepcion descendente. El repositorio normaliza PageNumber a 1 cuando es menor o igual que cero; PageSize usa 10 cuando es menor o igual que cero y limita valores mayores a 100. Los filtros de texto informados se aplican a los campos persistidos después de Trim.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(PagedResponse<AchResponseListItemResponse>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de respuestas ACH que cumplen los filtros solicitados.")]
    public async Task<IActionResult> Search([FromQuery] AchResponseSearchRequest request,
        [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseRepository repository,
        CancellationToken ct) => Ok(mapper.MapPagedResult(await repository.SearchAsync(mapper.MapSearchRequest(request), ct)));

    /// <summary>
    /// Consulta indicadores agregados de respuestas ACH.
    /// </summary>
    /// <param name="request">Rango opcional de fechas y tipo de respuesta opcional.</param>
    /// <param name="mapper">Mapeador del contrato de consulta.</param>
    /// <param name="repository">Repositorio de lectura de respuestas ACH.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar resumen de respuestas ACH")]
    [EndpointDescription("Calcula contadores sobre las respuestas que cumplen el rango de FechaRecepcion y el tipo opcional. TipoRespuesta acepta Prenota o Transaccion sin distinguir mayúsculas y minúsculas; cualquier otro valor no vacío devuelve 400 con ProblemDetails sin consultar el repositorio.")]
    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(AchResponseDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Contadores agregados de respuestas ACH para los filtros solicitados.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "TipoRespuesta tiene un valor distinto de Prenota o Transaccion.")]
    public async Task<IActionResult> GetDashboard([FromQuery] AchResponseDashboardRequest request,
        [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseRepository repository,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.TipoRespuesta) && mapper.ParseTipoRespuestaOrNull(request.TipoRespuesta) is null)
            return BadRequest(new ProblemDetails { Title = "Tipo de respuesta inválido", Detail = "Valores permitidos: Prenota, Transaccion" });
        return Ok(mapper.MapDashboard(await repository.GetDashboardAsync(mapper.MapDashboardRequest(request), ct)));
    }

    /// <summary>
    /// Consulta el detalle persistido de una respuesta ACH.
    /// </summary>
    /// <param name="id">Identificador de la respuesta ACH.</param>
    /// <param name="mapper">Mapeador del modelo de lectura al contrato HTTP.</param>
    /// <param name="repository">Repositorio de lectura de respuestas ACH.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar detalle de respuesta ACH")]
    [EndpointDescription("Busca la respuesta por su identificador. Cuando existe, devuelve el detalle junto con sus intentos de notificación ordenados por número de intento; cuando no existe responde 404 sin cuerpo.")]
    [HttpGet("{id:guid}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(AchResponseDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de la respuesta ACH encontrada, incluidos sus intentos de notificación.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una respuesta ACH con el identificador indicado; la respuesta no contiene cuerpo.")]
    public async Task<IActionResult> GetDetail(Guid id, [FromServices] AchResponseQueryApiMapper mapper,
        [FromServices] IAchResponseRepository repository, CancellationToken ct)
    {
        var detail = await repository.FindDetailByIdAsync(id, ct);
        return detail is null ? NotFound() : Ok(mapper.MapDetail(detail));
    }

    /// <summary>
    /// Consulta los intentos de notificación asociados a una respuesta ACH.
    /// </summary>
    /// <param name="id">Identificador de la respuesta ACH.</param>
    /// <param name="mapper">Mapeador del modelo de intento al contrato público.</param>
    /// <param name="repository">Repositorio de intentos de notificación.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar intentos de notificación de una respuesta ACH")]
    [EndpointDescription("Devuelve los intentos asociados al identificador de respuesta, ordenados por NumeroIntento. Si no existen intentos, la colección puede estar vacía; esta acción no convierte ese caso en 404.")]
    [HttpGet("{id:guid}/notification-attempts")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchResponseNotificationAttemptResponse>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Intentos de notificación asociados a la respuesta, ordenados por número de intento.")]
    public async Task<IActionResult> GetAttempts(Guid id, [FromServices] AchResponseQueryApiMapper mapper,
        [FromServices] IAchResponseNotificationAttemptRepository repository, CancellationToken ct)
        => Ok((await repository.FindPublicByResponseIdAsync(id, ct)).Select(mapper.MapAttempt).ToList());

    /// <summary>
    /// Consulta los mappings de estado de respuesta ACH usando filtros opcionales.
    /// </summary>
    /// <param name="codigoCamaraCompensacion">Código de cámara opcional.</param>
    /// <param name="tipoRespuesta">Tipo opcional; acepta Prenota o Transaccion.</param>
    /// <param name="activo">Filtro opcional por estado activo.</param>
    /// <param name="mapper">Mapeador de consulta y de contrato de salida.</param>
    /// <param name="repository">Repositorio de mappings de estado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar mappings de estado de respuestas ACH")]
    [EndpointDescription("Consulta los mappings persistidos con filtros opcionales por cámara, tipo de respuesta y estado activo. TipoRespuesta acepta Prenota o Transaccion sin distinguir mayúsculas y minúsculas; cualquier otro valor no vacío devuelve 400 con ProblemDetails sin consultar el repositorio.")]
    [HttpGet("/api/ach/response-status-mappings")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchResponseStatusMappingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mappings de estado que cumplen los filtros solicitados.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "TipoRespuesta tiene un valor distinto de Prenota o Transaccion.")]
    public async Task<IActionResult> GetMappings([FromQuery] string? codigoCamaraCompensacion,
        [FromQuery] string? tipoRespuesta, [FromQuery] bool? activo,
        [FromServices] AchResponseQueryApiMapper mapper,
        [FromServices] IAchResponseStatusMappingRepository repository, CancellationToken ct)
    {
        var tipo = mapper.ParseTipoRespuestaOrNull(tipoRespuesta);
        if (!string.IsNullOrWhiteSpace(tipoRespuesta) && tipo is null)
            return BadRequest(new ProblemDetails { Title = "Tipo de respuesta inválido", Detail = "Valores permitidos: Prenota, Transaccion" });
        var items = await repository.ListAsync(codigoCamaraCompensacion, tipo, activo, ct);
        return Ok(items.Select(mapper.MapStatusMapping).ToList());
    }
}
