using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TaskDefinitionsController : ControllerBase
{
    private readonly ITaskDefinitionAppService _service;

    public TaskDefinitionsController(ITaskDefinitionAppService service)
    {
        _service = service;
    }

    /// <summary>Lista las definiciones de tarea persistidas.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar definiciones de tarea")]
    [EndpointDescription("Consulta todas las definiciones de tarea persistidas, incluyendo sus parámetros, ordenadas por Id ascendente.")]
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IEnumerable<TaskDefinitionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Definiciones de tarea persistidas con sus parámetros, ordenadas por Id ascendente.")]
    public async Task<ActionResult<IEnumerable<TaskDefinitionDto>>> Get(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(items);
    }

    /// <summary>Consulta una definición de tarea por identificador.</summary>
    /// <param name="id">Identificador numérico de la definición de tarea.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar definición de tarea")]
    [EndpointDescription("Consulta la definición de tarea persistida identificada por id e incluye sus parámetros. Devuelve 404 cuando no existe una definición con ese identificador.")]
    [HttpGet("{id}")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(TaskDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Definición de tarea encontrada con sus parámetros persistidos.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una definición de tarea con el identificador indicado; la respuesta no tiene body.")]
    public async Task<ActionResult<TaskDefinitionDto>> Get(int id, CancellationToken ct)
    {
        var task = await _service.GetByIdAsync(id, ct);
        if (task is null) return NotFound();
        return Ok(task);
    }

    /// <summary>Crea una definición de tarea.</summary>
    /// <param name="task">Definición y parámetros que se desean persistir.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear definición de tarea")]
    [EndpointDescription("Crea una definición nueva a partir del body. El servicio recorta Code y Name; convierte Description, TimeZoneId y CronExpression en null cuando contienen solo espacios; intenta convertir TimeOfDay a TimeOnly y persiste null si no puede interpretarlo; descarta parámetros con Key vacío y recorta Key y Value. La respuesta se emite con CreatedAtAction y la definición persistida.")]
    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(TaskDefinitionDto), StatusCodes.Status201Created)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Definición de tarea creada y devuelta con la ubicación de consulta por identificador.")]
    public async Task<ActionResult<TaskDefinitionDto>> Post([FromBody] TaskDefinitionDto task, CancellationToken ct)
    {
        var created = await _service.CreateAsync(task, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Actualiza una definición de tarea.</summary>
    /// <param name="id">Identificador numérico de la definición que se desea actualizar.</param>
    /// <param name="task">Estado completo de la definición y sus parámetros.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar definición de tarea")]
    [EndpointDescription("Exige que el id de la ruta coincida con TaskDefinitionDto.Id; si difiere responde 400 sin body. Si la definición no existe responde 404. Para una definición existente aplica la misma normalización de textos y TimeOfDay usada en creación. En Parameters, los elementos con Key vacío se descartan; los Id existentes se actualizan, Id 0 se agrega como parámetro nuevo y los parámetros persistidos que no aparecen con su Id se eliminan.")]
    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(TaskDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Definición de tarea actualizada con sus parámetros persistidos.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El id de la ruta no coincide con el Id del body; la respuesta no tiene body.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una definición de tarea con el identificador indicado; la respuesta no tiene body.")]
    public async Task<IActionResult> Put(int id, [FromBody] TaskDefinitionDto task, CancellationToken ct)
    {
        if (id != task.Id) return BadRequest();
        var updated = await _service.UpdateAsync(id, task, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Elimina una definición de tarea.</summary>
    /// <param name="id">Identificador numérico de la definición que se desea eliminar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Eliminar definición de tarea")]
    [EndpointDescription("Elimina la definición persistida identificada por id. Devuelve 204 cuando la entidad fue eliminada y 404 cuando no existe una definición con ese identificador.")]
    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status204NoContent, "La definición de tarea fue eliminada; la respuesta no tiene body.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una definición de tarea con el identificador indicado; la respuesta no tiene body.")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
