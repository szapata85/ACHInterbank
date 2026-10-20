using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-cycles")]
[Route("api/ach-cycles")]
[Authorize]
public class AchCyclesController : ControllerBase
{
    private readonly IAchCycleAppService _service;

    public AchCyclesController(IAchCycleAppService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista ciclos operativos con filtros opcionales por cámara y ProcessingDate.
    /// </summary>
    /// <param name="clearingHouseId">Identificador opcional de la cámara compensadora.</param>
    /// <param name="startDate">Inicio opcional del rango de ProcessingDate.</param>
    /// <param name="endDate">Fin opcional del rango de ProcessingDate.</param>
    /// <param name="processingDate">Fecha de respaldo usada para startDate y/o endDate cuando alguno de esos límites no se informa.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar ciclos ACH")]
    [EndpointDescription("Lista ciclos operativos y permite filtrar por cámara y por ProcessingDate. processingDate actúa como respaldo para los límites de rango no informados; si el inicio resultante queda después del fin, el servicio normaliza el rango intercambiando ambos límites.")]
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IEnumerable<AchCycleDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de ciclos que cumplen los filtros de cámara y ProcessingDate.")]
    public async Task<IActionResult> Get(
        [FromQuery] int? clearingHouseId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] DateTime? processingDate,
        CancellationToken ct)
    {
        var effectiveStart = startDate ?? processingDate;
        var effectiveEnd = endDate ?? processingDate;

        var cycles = await _service.GetAsync(clearingHouseId, effectiveStart, effectiveEnd, ct);
        return Ok(cycles);
    }

    /// <summary>
    /// Lista ciclos relacionados con transacciones en estado exportable.
    /// </summary>
    /// <param name="clearingHouseId">Identificador opcional de la cámara compensadora.</param>
    /// <param name="startDate">Fecha efectiva mínima opcional de las transacciones asociadas.</param>
    /// <param name="endDate">Fecha efectiva máxima opcional de las transacciones asociadas.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar ciclos exportables")]
    [EndpointDescription("Devuelve ciclos que tienen transacciones en estados elegibles para exportación. Los filtros startDate y endDate se aplican sobre EffectiveEntryDate de las transacciones asociadas, no sobre ProcessingDate del ciclo.")]
    [HttpGet("exportable")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IEnumerable<AchCycleExportDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de ciclos con información de disponibilidad y artefactos de exportación.")]
    public async Task<IActionResult> GetExportable(
        [FromQuery] int? clearingHouseId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var cycles = await _service.GetExecutedWithTransactionsAsync(clearingHouseId, startDate, endDate, ct);
        return Ok(cycles);
    }

    /// <summary>
    /// Obtiene un ciclo operativo por su identificador.
    /// </summary>
    /// <param name="id">Identificador del ciclo operativo.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar ciclo ACH")]
    [EndpointDescription("Devuelve el ciclo identificado por id junto con su metadata operativa calculada. Cuando no existe, responde 404 sin inventar un cuerpo ProblemDetails que la acción actual no devuelve.")]
    [HttpGet("{id}")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchCycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Ciclo operativo encontrado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un ciclo con el identificador indicado.")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var cycle = await _service.GetByIdAsync(id, ct);
        return cycle is null ? NotFound() : Ok(cycle);
    }

    /// <summary>
    /// Crea una instancia de ciclo operativo a partir de una configuración canónica vigente.
    /// </summary>
    /// <param name="request">Datos usados para seleccionar la cámara, fecha y configuración de ciclo aplicable.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear ciclo ACH")]
    [EndpointDescription("Crea el ciclo resolviendo primero una configuración canónica activa y vigente para la cámara y ProcessingDate. El nombre y la ventana operativa finales se toman de esa configuración, incluso cuando el request aporta valores usados para resolverla.")]
    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchCycleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Ciclo creado con los valores finales de la configuración canónica resuelta.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "No fue posible resolver una configuración canónica válida y no ambigua para el ciclo solicitado.")]
    public async Task<IActionResult> Create([FromBody] AchCycleRequest request, CancellationToken ct)
    {
        try
        {
            var cycle = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = cycle.Id }, cycle);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Problem("Ciclo ACH inválido", ex.Message, StatusCodes.Status400BadRequest));
        }
    }

    /// <summary>
    /// Actualiza una instancia de ciclo operativo y vuelve a resolver su configuración canónica.
    /// </summary>
    /// <param name="id">Identificador del ciclo operativo.</param>
    /// <param name="request">Datos usados para actualizar la fecha, cámara y configuración aplicable.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar ciclo ACH")]
    [EndpointDescription("Actualiza el ciclo y resuelve la configuración canónica aplicable. El nombre y la ventana operativa finales provienen de esa configuración; cambiar la configuración de un ciclo que ya tiene transacciones es rechazado.")]
    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchCycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Ciclo actualizado con los valores de la configuración canónica resuelta.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La configuración solicitada es inválida, ambigua o no puede cambiarse para el ciclo actual.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un ciclo con el identificador indicado.")]
    public async Task<IActionResult> Update(string id, [FromBody] AchCycleRequest request, CancellationToken ct)
    {
        try
        {
            var cycle = await _service.UpdateAsync(id, request, ct);
            return Ok(cycle);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Problem("Ciclo ACH no encontrado", ex.Message, StatusCodes.Status404NotFound));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(Problem("Ciclo ACH inválido", ex.Message, StatusCodes.Status400BadRequest));
        }
    }

    /// <summary>
    /// Repara vínculos faltantes entre ciclos históricos y sus configuraciones canónicas.
    /// </summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reparar vínculos de configuración de ciclos")]
    [EndpointDescription("Busca ciclos históricos sin ClearingHouseCycleConfigId y los vincula cuando existe una coincidencia histórica única. Si encuentra asociaciones ambiguas, no modifica ningún ciclo y responde 409 con el resultado de la evaluación.")]
    [HttpPost("repair-configuration-links")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchCycleConfigurationLinkRepairResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reparación completada; el resultado informa ciclos inspeccionados, reparados y no emparejados.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La reparación se bloqueó por asociaciones ambiguas y no se modificó ningún ciclo.")]
    public async Task<IActionResult> RepairConfigurationLinks(CancellationToken ct)
    {
        var result = await _service.RepairConfigurationLinksAsync(ct);
        return result.Completed
            ? Ok(result)
            : Conflict(Problem(
                "Reparación bloqueada por asociaciones ambiguas",
                "No se modificó ningún ciclo. Revise los identificadores reportados y corrija las configuraciones superpuestas.",
                StatusCodes.Status409Conflict,
                result));
    }

    /// <summary>
    /// Elimina el ciclo operativo identificado.
    /// </summary>
    /// <param name="id">Identificador del ciclo operativo que se eliminará.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Eliminar ciclo ACH")]
    [EndpointDescription("Elimina el ciclo identificado y, si la operación concluye, responde 204 sin cuerpo. Esta documentación no agrega respuestas de error que la acción no declara actualmente.")]
    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [OpenApiResponseDescription(StatusCodes.Status204NoContent, "Ciclo eliminado correctamente; la respuesta no contiene cuerpo.")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    private static ProblemDetails Problem(string title, string detail, int status, object? result = null)
    {
        var problem = new ProblemDetails { Title = title, Detail = detail, Status = status };
        if (result is not null)
        {
            problem.Extensions["repairResult"] = result;
        }
        return problem;
    }
}
