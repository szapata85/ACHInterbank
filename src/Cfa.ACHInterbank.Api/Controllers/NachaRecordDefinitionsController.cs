using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-record-definitions")]
[Authorize]
public class NachaRecordDefinitionsController : ControllerBase
{
    private const string LegacyMessage = "Endpoint legacy/deprecated. La configuracion oficial NACHA-M usa nacha-config profiles.";
    private readonly INachaRecordDefinitionAppService _service;

    public NachaRecordDefinitionsController(INachaRecordDefinitionAppService service)
    {
        _service = service;
    }

    /// <summary>Consulta las definiciones NACHA-M legacy disponibles para diagnóstico.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("Consultar definiciones NACHA-M legacy")]
    [EndpointDescription("Consulta las definiciones legacy persistidas y las devuelve ordenadas por Sequence. Esta superficie es de diagnóstico y compatibilidad; no administra la configuración oficial NACHA-M, que se gestiona mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(IEnumerable<NachaRecordDefinitionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Definiciones NACHA-M legacy disponibles para diagnóstico.")]
    public async Task<ActionResult<IEnumerable<NachaRecordDefinitionDto>>> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(items);
    }

    /// <summary>Consulta una definición NACHA-M legacy por identificador.</summary>
    /// <param name="id">Identificador interno de la definición legacy.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("Consultar definición NACHA-M legacy")]
    [EndpointDescription("Busca una definición legacy por su identificador. Devuelve 404 sin body cuando no existe. Esta superficie se conserva para diagnóstico y compatibilidad y no representa la configuración oficial NACHA-M.")]
    [ProducesResponseType(typeof(NachaRecordDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Definición NACHA-M legacy encontrada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una definición NACHA-M legacy con el identificador indicado.")]
    public async Task<ActionResult<NachaRecordDefinitionDto>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Expone la operación legacy de creación, actualmente retirada.</summary>
    /// <param name="request">Definición recibida por el contrato legacy. La acción actual no la persiste.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("Crear definición NACHA-M legacy (retirado)")]
    [EndpointDescription("La mutación legacy está retirada: la acción no invoca el servicio ni persiste la definición recibida y responde 410 con un cuerpo anónimo que contiene codigo y mensaje. La configuración oficial se administra mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status410Gone)]
    [OpenApiResponseDescription(StatusCodes.Status410Gone, "La creación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.")]
    public ActionResult<NachaRecordDefinitionDto> Create([FromBody] NachaRecordDefinitionDto request, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_DEFINITIONS_DEPRECATED", mensaje = LegacyMessage });
    }

    /// <summary>Expone la operación legacy de actualización, actualmente retirada.</summary>
    /// <param name="id">Identificador recibido por el contrato legacy. La acción actual no modifica ningún registro.</param>
    /// <param name="request">Definición recibida por el contrato legacy. La acción actual no la persiste.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("Actualizar definición NACHA-M legacy (retirado)")]
    [EndpointDescription("La mutación legacy está retirada: la acción no invoca el servicio ni modifica la definición indicada y responde 410 con un cuerpo anónimo que contiene codigo y mensaje. La configuración oficial se administra mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status410Gone)]
    [OpenApiResponseDescription(StatusCodes.Status410Gone, "La actualización legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.")]
    public IActionResult Update(int id, [FromBody] NachaRecordDefinitionDto request, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_DEFINITIONS_DEPRECATED", mensaje = LegacyMessage });
    }

    /// <summary>Expone la operación legacy de eliminación, actualmente retirada.</summary>
    /// <param name="id">Identificador recibido por el contrato legacy. La acción actual no elimina ningún registro.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("Eliminar definición NACHA-M legacy (retirado)")]
    [EndpointDescription("La mutación legacy está retirada: la acción no invoca el servicio ni elimina la definición indicada y responde 410 con un cuerpo anónimo que contiene codigo y mensaje. La configuración oficial se administra mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status410Gone)]
    [OpenApiResponseDescription(StatusCodes.Status410Gone, "La eliminación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.")]
    public IActionResult Delete(int id, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_DEFINITIONS_DEPRECATED", mensaje = LegacyMessage });
    }
}
