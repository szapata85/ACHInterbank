using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("incoming-nacha-command-center")]
[Authorize]
public class IncomingNachaCommandCenterController : ControllerBase
{
    private static IActionResult MapInvalidOperation(InvalidOperationException ex)
        => new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status409Conflict };
    private readonly IIncomingNachaCommandCenterQueryService _queryService;
    private readonly IIncomingNachaCommandCenterOperationsService _operationsService;
    private readonly IIncomingNachaOrphanManualResolutionService _orphanResolutionService;

    public IncomingNachaCommandCenterController(
        IIncomingNachaCommandCenterQueryService queryService,
        IIncomingNachaCommandCenterOperationsService operationsService,
        IIncomingNachaOrphanManualResolutionService orphanResolutionService)
    {
        _queryService = queryService;
        _operationsService = operationsService;
        _orphanResolutionService = orphanResolutionService;
    }

    /// <summary>Consulta indicadores consolidados del procesamiento inbound NACHA-M.</summary>
    /// <param name="windowHours">Cantidad de horas usadas para construir la ventana. El servicio aplica un valor efectivo entre 1 y 168.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar resumen de observabilidad inbound NACHA-M")]
    [EndpointDescription("Consolida indicadores de ingestas, cola de despacho, eventos y errores dentro de una ventana horaria. windowHours se normaliza al rango 1..168 y el valor efectivo se devuelve en WindowHours. La operación es de solo lectura.")]
    [HttpGet("observability/summary")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaObservabilitySummaryDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resumen consolidado de observabilidad para la ventana efectiva solicitada.")]
    public async Task<IActionResult> GetObservabilitySummary([FromQuery] int windowHours = 24, CancellationToken ct = default)
        => Ok(await _queryService.GetObservabilitySummaryAsync(windowHours, ct));

    /// <summary>Consulta las ingestas NACHA-M entrantes registradas por el command center.</summary>
    /// <param name="query">Paginación, filtros y ordenamiento aplicados a las ingestas.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar ingestas inbound NACHA-M")]
    [EndpointDescription("Devuelve una página de ingestas entrantes. Page se normaliza a un mínimo de 1 y PageSize al rango 1..200. Permite filtrar por estados de ingesta y parseo, correlación, archivo, cámara, ciclo, fecha operacional, rango de carga, resultado funcional y presencia de incidencias; SortBy reconoce fileName, operationalDate y cycle, y para otros valores ordena por UploadedAtUtc.")]
    [HttpGet("ingestions")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaPageResult<IncomingNachaIngestionListItemDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de ingestas inbound que cumplen los filtros solicitados.")]
    public async Task<IActionResult> GetIngestions([FromQuery] IncomingNachaIngestionQuery query, CancellationToken ct)
        => Ok(await _queryService.GetIngestionsAsync(query, ct));

    /// <summary>Consulta el detalle operativo de una ingesta NACHA-M entrante.</summary>
    /// <param name="ingestionId">Identificador de la ingesta consultada.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar detalle de una ingesta inbound NACHA-M")]
    [EndpointDescription("Devuelve el detalle de la ingesta indicada, su resumen de archivo, resultado de admisión cuando existe, cola asociada, eventos de procesamiento y conteo de transacciones pendientes. Si no existe una ingesta con el identificador indicado, responde 404.")]
    [HttpGet("ingestions/{ingestionId:guid}")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaIngestionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle operativo de la ingesta NACHA-M solicitada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una ingesta NACHA-M entrante con el identificador indicado.")]
    public async Task<IActionResult> GetIngestionDetail(Guid ingestionId, CancellationToken ct)
    {
        var result = await _queryService.GetIngestionDetailAsync(ingestionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Consulta las validaciones de admisión asociadas a una ingesta NACHA-M entrante.</summary>
    /// <param name="ingestionId">Identificador de la ingesta cuya admisión se desea consultar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar validaciones de admisión de una ingesta")]
    [EndpointDescription("Devuelve el resultado de admisión disponible para la ingesta indicada. Si existe una incidencia de admisión, informa su código, valores esperado y encontrado, severidad y acción sugerida; cuando la ingesta superó admisión y está en una etapa de procesamiento admitida, devuelve el resultado ADMISSION_ACCEPTED. Si no existe la ingesta, responde 404; para otras etapas sin resultado de admisión disponible, devuelve una colección vacía.")]
    [HttpGet("ingestions/{ingestionId:guid}/validations")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IReadOnlyList<IncomingNachaValidationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Validaciones de admisión registradas o derivadas para la ingesta solicitada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una ingesta NACHA-M entrante con el identificador indicado.")]
    public async Task<IActionResult> GetIngestionValidations(Guid ingestionId, CancellationToken ct)
    {
        var result = await _queryService.GetIngestionValidationsAsync(ingestionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Consulta los lotes persistidos para una ingesta NACHA-M entrante.</summary>
    /// <param name="ingestionId">Identificador de la ingesta usada para filtrar los lotes.</param>
    /// <param name="query">Paginación, búsqueda y ordenamiento aplicados a los lotes encontrados.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar lotes de una ingesta NACHA-M")]
    [EndpointDescription("Devuelve una página de lotes asociados a la ingesta indicada. Page se normaliza a un mínimo de 1 y PageSize se limita al rango 1..200. Search filtra por nombre de compañía o número de lote; SortBy acepta companyName y, para cualquier otro valor, ordena por número de lote. Si la ingesta no tiene lotes asociados, devuelve 200 con una página vacía.")]
    [HttpGet("ingestions/{ingestionId:guid}/batches")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaPageResult<IncomingNachaBatchDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de lotes asociados a la ingesta solicitada.")]
    public async Task<IActionResult> GetBatches(Guid ingestionId, [FromQuery] IncomingNachaBatchQuery query, CancellationToken ct)
        => Ok(await _queryService.GetBatchesAsync(ingestionId, query, ct));

    /// <summary>Consulta las transacciones persistidas para una ingesta NACHA-M entrante.</summary>
    /// <param name="ingestionId">Identificador de la ingesta usada para filtrar las transacciones.</param>
    /// <param name="query">Paginación, filtros y ordenamiento aplicados a las transacciones encontradas.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar transacciones de una ingesta NACHA-M")]
    [EndpointDescription("Devuelve una página de transacciones asociadas a la ingesta indicada junto con la clasificación disponible, estado de despacho, último intento de integración encontrado, resultado funcional y conteo de adendas. Page se normaliza a un mínimo de 1 y PageSize se limita a 1..200. Los filtros opcionales se aplican sobre lote, búsqueda, resultado, outcome, estado de procesamiento, presencia de adenda, error técnico y código de transacción. Si no existen coincidencias, devuelve 200 con una página vacía.")]
    [HttpGet("ingestions/{ingestionId:guid}/transactions")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaPageResult<IncomingNachaTransactionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de transacciones asociadas a la ingesta y filtros solicitados.")]
    public async Task<IActionResult> GetTransactions(Guid ingestionId, [FromQuery] IncomingNachaTransactionQuery query, CancellationToken ct)
        => Ok(await _queryService.GetTransactionsAsync(ingestionId, query, ct));

    /// <summary>Consulta las adendas persistidas de una transacción dentro de una ingesta NACHA-M.</summary>
    /// <param name="ingestionId">Identificador de la ingesta a la que debe pertenecer la transacción.</param>
    /// <param name="entryDetailId">Identificador interno del detalle de transacción cuyas adendas se consultan.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar adendas de una transacción inbound")]
    [EndpointDescription("Devuelve las adendas cuyo EntryDetail pertenece simultáneamente a la transacción y a la ingesta indicadas, ordenadas por secuencia de adenda. OriginalTraceNumber y PaymentInformation se exponen con el enmascaramiento aplicado por el servicio. Si no existen coincidencias, devuelve 200 con una colección vacía.")]
    [HttpGet("ingestions/{ingestionId:guid}/transactions/{entryDetailId:int}/addendas")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IReadOnlyList<IncomingNachaAddendaDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Adendas asociadas a la transacción dentro de la ingesta solicitada.")]
    public async Task<IActionResult> GetAddendas(Guid ingestionId, int entryDetailId, CancellationToken ct)
        => Ok(await _queryService.GetAddendasAsync(ingestionId, entryDetailId, ct));

    /// <summary>Consulta devoluciones entrantes pendientes de una relación final.</summary>
    /// <param name="query">Paginación y búsqueda aplicadas a las devoluciones pendientes.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar devoluciones entrantes pendientes de relación")]
    [EndpointDescription("Devuelve vínculos no finales con tipo NotFound, Ambiguous o NoResuelto. Page se normaliza a un mínimo de 1 y PageSize al rango 1..100. Search busca en nombre de archivo, número de secuencia, trazabilidad original y código de causal. Si no existen coincidencias, devuelve 200 con una página vacía.")]
    [HttpGet("orphans")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaPageResult<IncomingNachaOrphanDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de devoluciones entrantes pendientes de relación final.")]
    public async Task<IActionResult> GetOrphans([FromQuery] IncomingNachaOrphanQuery query, CancellationToken ct)
        => Ok(await _queryService.GetOrphansAsync(query, ct));

    /// <summary>Consulta una devolución entrante pendiente o previamente resuelta.</summary>
    /// <param name="linkId">Identificador del vínculo de la devolución entrante.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar detalle de una devolución entrante")]
    [EndpointDescription("Devuelve el vínculo de devolución indicado con datos de ingesta, transacción de entrada, causal, candidatos registrados y estado de resolución. Si el vínculo no existe, responde 404.")]
    [HttpGet("orphans/{linkId:guid}")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaOrphanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de la devolución entrante solicitada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un vínculo de devolución entrante con el identificador indicado.")]
    public async Task<IActionResult> GetOrphan(Guid linkId, CancellationToken ct)
    {
        var result = await _queryService.GetOrphanAsync(linkId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Consulta transacciones candidatas para relacionar una devolución entrante.</summary>
    /// <param name="linkId">Identificador del vínculo de devolución pendiente.</param>
    /// <param name="search">Filtro opcional por identificador de transacción, trazabilidad o identificador externo.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar candidatos para relacionar una devolución")]
    [EndpointDescription("Devuelve hasta 20 transacciones candidatas para la devolución indicada. El servicio restringe candidatos por monto y, cuando están disponibles, cámara, cuenta y candidatos registrados en la evidencia; además informa compatibilidad y motivos de incompatibilidad. Si el vínculo no existe, responde 404; si el vínculo no conserva detalle de entrada, devuelve 200 con una colección vacía.")]
    [HttpGet("orphans/{linkId:guid}/candidates")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IReadOnlyList<IncomingNachaOrphanCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Transacciones candidatas evaluadas para la devolución entrante.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la devolución entrante indicada.")]
    public async Task<IActionResult> GetOrphanCandidates(Guid linkId, [FromQuery] string? search, CancellationToken ct)
    {
        try
        {
            return Ok(await _queryService.GetOrphanCandidatesAsync(linkId, search, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>Relaciona una devolución entrante pendiente con una transacción ACH.</summary>
    /// <param name="linkId">Identificador del vínculo de devolución pendiente.</param>
    /// <param name="request">Transacción seleccionada y evidencia textual de la resolución manual.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Relacionar una devolución entrante con una transacción")]
    [EndpointDescription("Confirma una relación manual LinkToTransaction para el vínculo indicado. La justificación debe tener al menos 8 caracteres. Si la misma relación ya fue aplicada al mismo destino, devuelve 200 sin repetir efectos. Devuelve 400 cuando la solicitud o la evidencia conservada son insuficientes, 404 cuando no existe el vínculo o la transacción seleccionada y 409 cuando la devolución no puede resolverse con esa decisión, incluida una transacción incompatible o una resolución previa distinta.")]
    [HttpPost("orphans/{linkId:guid}/resolve")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IncomingNachaOrphanManualResolutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "La relación manual fue aplicada o ya se había aplicado al mismo destino.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud no cumple las condiciones requeridas para aplicar la relación manual.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la devolución entrante o la transacción seleccionada.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La devolución no puede resolverse con la decisión solicitada en su estado actual.")]
    public async Task<IActionResult> ResolveOrphan(
        Guid linkId,
        [FromBody] IncomingNachaOrphanResolveRequestDto request,
        CancellationToken ct)
    {
        var result = await _orphanResolutionService.ResolveAsync(new IncomingNachaOrphanManualResolutionRequest
        {
            IncomingNachaTransactionLinkId = linkId,
            ResolutionAction = IncomingNachaOrphanResolutionAction.LinkToTransaction,
            ResolvedAchTransactionId = request.AchTransactionId,
            ResolutionReason = request.Justification,
            Comment = request.Comment,
            CorrelationId = request.CorrelationId,
            ResolvedBy = User?.Identity?.Name ?? "operador.ach"
        }, ct);

        if (result.IsResolved)
        {
            return Ok(result);
        }

        return result.Status switch
        {
            "NotFound" or "TransactionNotFound" => NotFound(new { message = result.Message, result.Status }),
            "Invalid" => BadRequest(new { message = result.Message, result.Status }),
            _ => Conflict(new { message = result.Message, result.Status })
        };
    }

    /// <summary>Consulta los ítems de la cola de despacho inbound NACHA-M.</summary>
    /// <param name="query">Paginación y filtros aplicados a la cola.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar cola de despacho inbound NACHA-M")]
    [EndpointDescription("Devuelve una página de ítems de dispatch queue ordenados por prioridad e identificador. Page se normaliza a un mínimo de 1 y PageSize al rango 1..200. Puede filtrar por ingesta, estado de cola, cámara y ciclo ACH.")]
    [HttpGet("queue")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaPageResult<IncomingNachaQueueListItemDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de ítems de la cola de despacho que cumplen los filtros solicitados.")]
    public async Task<IActionResult> GetQueue([FromQuery] IncomingNachaQueueQuery query, CancellationToken ct)
        => Ok(await _queryService.GetQueueAsync(query, ct));

    /// <summary>Consulta el detalle de un ítem de la cola de despacho inbound.</summary>
    /// <param name="queueId">Identificador del ítem de cola consultado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar detalle de un ítem de cola inbound")]
    [EndpointDescription("Devuelve el ítem de cola, la ingesta asociada, su clasificación, las ejecuciones de integración registradas y hasta 100 eventos de procesamiento relacionados. AllowedActions se calcula con la máquina de estados vigente. Si el ítem no existe, responde 404.")]
    [HttpGet("queue/{queueId:guid}")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaQueueDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle operativo del ítem de cola solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un ítem de cola con el identificador indicado.")]
    public async Task<IActionResult> GetQueueDetail(Guid queueId, CancellationToken ct)
    {
        var result = await _queryService.GetQueueDetailAsync(queueId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Solicita un reintento manual de un ítem de la cola de despacho.</summary>
    /// <param name="queueId">Identificador del ítem sobre el que se solicita el reintento.</param>
    /// <param name="request">Justificación, clave de idempotencia de la acción y prioridad opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reintentar manualmente un ítem de cola inbound")]
    [EndpointDescription("Aplica la transición ManualRetry cuando la máquina de estados la permite. Justification debe contener al menos 8 caracteres e IdempotencyKey se recibe dentro del body; una repetición de la misma clave para la misma acción devuelve 200 como replay idempotente. Priority, cuando se informa, se limita al rango 1..999. Las validaciones de la acción, un queueId inexistente o una transición no permitida se traducen actualmente a 409.")]
    [HttpPost("queue/{queueId:guid}/retry")]
    [Authorize(Policy = P1Policies.CommandCenterRetry)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reintento manual aplicado o solicitud idempotente ya procesada.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.")]
    public async Task<IActionResult> RetryManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _operationsService.RetryManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    /// <summary>Solicita el desbloqueo manual de un ítem de la cola de despacho.</summary>
    /// <param name="queueId">Identificador del ítem sobre el que se solicita el desbloqueo.</param>
    /// <param name="request">Justificación, clave de idempotencia de la acción y prioridad opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Desbloquear manualmente un ítem de cola inbound")]
    [EndpointDescription("Aplica la transición ManualUnblock cuando la máquina de estados la permite, programa el próximo intento para el momento actual y limpia el último error. Justification debe contener al menos 8 caracteres e IdempotencyKey se recibe dentro del body; una repetición de la misma clave para la misma acción devuelve 200 como replay idempotente. Priority, cuando se informa, se limita a 1..999. Las validaciones de la acción, un queueId inexistente o una transición no permitida se traducen actualmente a 409.")]
    [HttpPost("queue/{queueId:guid}/unblock")]
    [Authorize(Policy = P1Policies.CommandCenterUnblock)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Desbloqueo manual aplicado o solicitud idempotente ya procesada.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.")]
    public async Task<IActionResult> UnblockManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _operationsService.UnblockManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    /// <summary>Solicita la reprogramación manual de un ítem de la cola de despacho.</summary>
    /// <param name="queueId">Identificador del ítem que se desea reencolar.</param>
    /// <param name="request">Justificación, clave de idempotencia de la acción y prioridad opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reencolar manualmente un ítem de cola inbound")]
    [EndpointDescription("Aplica la transición ManualRequeue cuando la máquina de estados la permite y programa el próximo intento para el momento actual. Justification debe contener al menos 8 caracteres e IdempotencyKey se recibe dentro del body; una repetición de la misma clave para la misma acción devuelve 200 como replay idempotente. Priority, cuando se informa, se limita a 1..999. Las validaciones de la acción, un queueId inexistente o una transición no permitida se traducen actualmente a 409.")]
    [HttpPost("queue/{queueId:guid}/requeue")]
    [Authorize(Policy = P1Policies.CommandCenterRequeue)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reencolado manual aplicado o solicitud idempotente ya procesada.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.")]
    public async Task<IActionResult> RequeueManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _operationsService.RequeueManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    /// <summary>Solicita marcar un ítem de cola como falla final.</summary>
    /// <param name="queueId">Identificador del ítem que se desea cerrar como falla final.</param>
    /// <param name="request">Justificación y clave de idempotencia de la acción. Priority no modifica esta transición.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Marcar un ítem de cola como falla final")]
    [EndpointDescription("Aplica la transición ManualMarkFailedFinal cuando la máquina de estados la permite y elimina la fecha del próximo intento. Justification debe contener al menos 8 caracteres e IdempotencyKey se recibe dentro del body; una repetición de la misma clave para la misma acción devuelve 200 como replay idempotente. Priority no se usa al aplicar esta acción. Las validaciones de la acción, un queueId inexistente o una transición no permitida se traducen actualmente a 409.")]
    [HttpPost("queue/{queueId:guid}/mark-failed-final")]
    [Authorize(Policy = P1Policies.CommandCenterMarkFailedFinal)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "El ítem fue marcado como falla final o la misma solicitud ya había sido procesada.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.")]
    public async Task<IActionResult> MarkFailedFinal(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _operationsService.MarkFailedFinalManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }
}
