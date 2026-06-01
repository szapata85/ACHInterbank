using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-layouts")]
[Authorize]
[Obsolete("Legacy diagnostic endpoint. Official NACHA-M configuration uses nacha-config profiles.")]
public class NachaRecordLayoutsController : ControllerBase
{
    private const string LegacyMessage = "Endpoint legacy/deprecated. La configuracion oficial NACHA-M usa nacha-config profiles.";
    private readonly INachaRecordLayoutAppService _service;

    public NachaRecordLayoutsController(INachaRecordLayoutAppService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("LEGACY diagnostico: consultar layouts NACHA-M legacy")]
    [EndpointDescription("Deprecated/diagnostico. Estos layouts no son la parametrizacion oficial NACHA-M. La administracion oficial debe hacerse con nacha-config profiles.")]
    public async Task<ActionResult<IEnumerable<NachaRecordLayoutDto>>> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(items);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("LEGACY diagnostico: consultar layout NACHA-M legacy")]
    [EndpointDescription("Deprecated/diagnostico. Este endpoint se conserva por compatibilidad; no usar como fuente oficial de parametrizacion NACHA-M.")]
    public async Task<ActionResult<NachaRecordLayoutDto>> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("LEGACY bloqueado: crear layout NACHA-M legacy")]
    [EndpointDescription("Deprecated. Mutaciones legacy bloqueadas; usar nacha-config profiles.")]
    public ActionResult<NachaRecordLayoutDto> Create([FromBody] NachaRecordLayoutDto request, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_LAYOUTS_DEPRECATED", mensaje = LegacyMessage });
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("LEGACY bloqueado: actualizar layout NACHA-M legacy")]
    [EndpointDescription("Deprecated. Mutaciones legacy bloqueadas; usar nacha-config profiles.")]
    public IActionResult Update(int id, [FromBody] NachaRecordLayoutDto request, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_LAYOUTS_DEPRECATED", mensaje = LegacyMessage });
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("LEGACY bloqueado: eliminar layout NACHA-M legacy")]
    [EndpointDescription("Deprecated. Mutaciones legacy bloqueadas; usar nacha-config profiles.")]
    public IActionResult Delete(int id, CancellationToken ct)
    {
        return StatusCode(410, new { codigo = "NACHA_LEGACY_LAYOUTS_DEPRECATED", mensaje = LegacyMessage });
    }
}
