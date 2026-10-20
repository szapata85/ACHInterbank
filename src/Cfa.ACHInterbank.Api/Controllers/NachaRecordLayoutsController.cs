using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-layouts")]
[Authorize]
public class NachaRecordLayoutsController : ControllerBase
{
    private const string LegacyMessage = "Endpoint legacy/deprecated. La configuracion oficial NACHA-M usa nacha-config profiles.";
    private readonly INachaRecordLayoutAppService _service;

    public NachaRecordLayoutsController(INachaRecordLayoutAppService service)
    {
        _service = service;
    }

    /// <summary>Consulta los layouts NACHA-M legacy disponibles para diagnóstico.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("Consultar layouts NACHA-M legacy")]
    [EndpointDescription("Consulta los layouts legacy persistidos ordenados por RecordCode. Cada resultado incluye sus campos ordenados por StartPosition. Esta superficie es de diagnóstico y compatibilidad; no administra la configuración oficial NACHA-M, que se gestiona mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(IEnumerable<NachaRecordLayoutDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Layouts NACHA-M legacy disponibles para diagnóstico.")]
    public async Task<ActionResult<IEnumerable<NachaRecordLayoutDto>>> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(items);
    }

    /// <summary>Consulta un layout NACHA-M legacy por identificador.</summary>
    /// <param name="id">Identificador interno del layout legacy.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("Consultar layout NACHA-M legacy")]
    [EndpointDescription("Busca un layout legacy por su identificador e incluye sus campos ordenados por StartPosition. Devuelve 404 sin body cuando no existe. Esta superficie se conserva para diagnóstico y compatibilidad y no representa la configuración oficial NACHA-M.")]
    [ProducesResponseType(typeof(NachaRecordLayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Layout NACHA-M legacy encontrado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un layout NACHA-M legacy con el identificador indicado.")]
    public async Task<ActionResult<NachaRecordLayoutDto>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Expone la operación legacy de creación de layouts, actualmente retirada.</summary>
    /// <param name="request">Layout recibido por el contrato legacy. La acción actual no lo persiste.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("Crear layout NACHA-M legacy (retirado)")]
    [EndpointDescription("La mutación legacy está retirada: la acción no invoca el servicio ni persiste el layout recibido y responde 410 con un cuerpo anónimo que contiene codigo y mensaje. La configuración oficial se administra mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status410Gone)]
    [OpenApiResponseDescription(StatusCodes.Status410Gone, "La creación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.")]
    public ActionResult<NachaRecordLayoutDto> Create([FromBody] NachaRecordLayoutDto request, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_LAYOUTS_DEPRECATED", mensaje = LegacyMessage });
    }

    /// <summary>Expone la operación legacy de actualización de layouts, actualmente retirada.</summary>
    /// <param name="id">Identificador recibido por el contrato legacy. La acción actual no modifica ningún registro.</param>
    /// <param name="request">Layout recibido por el contrato legacy. La acción actual no lo persiste.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("Actualizar layout NACHA-M legacy (retirado)")]
    [EndpointDescription("La mutación legacy está retirada: la acción no invoca el servicio ni modifica el layout indicado y responde 410 con un cuerpo anónimo que contiene codigo y mensaje. La configuración oficial se administra mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status410Gone)]
    [OpenApiResponseDescription(StatusCodes.Status410Gone, "La actualización legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.")]
    public IActionResult Update(int id, [FromBody] NachaRecordLayoutDto request, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_LAYOUTS_DEPRECATED", mensaje = LegacyMessage });
    }

    /// <summary>Expone la operación legacy de eliminación de layouts, actualmente retirada.</summary>
    /// <param name="id">Identificador recibido por el contrato legacy. La acción actual no elimina ningún registro.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("Eliminar layout NACHA-M legacy (retirado)")]
    [EndpointDescription("La mutación legacy está retirada: la acción no invoca el servicio ni elimina el layout indicado y responde 410 con un cuerpo anónimo que contiene codigo y mensaje. La configuración oficial se administra mediante nacha-config profiles.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status410Gone)]
    [OpenApiResponseDescription(StatusCodes.Status410Gone, "La eliminación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.")]
    public IActionResult Delete(int id, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_LAYOUTS_DEPRECATED", mensaje = LegacyMessage });
    }
}
