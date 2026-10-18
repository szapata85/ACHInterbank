using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/payment-rails/capability-registry")]
[Authorize]
public class PaymentRailCapabilityRegistryController : ControllerBase
{
    private readonly IPaymentRailCapabilityRegistryService _service;

    public PaymentRailCapabilityRegistryController(IPaymentRailCapabilityRegistryService service)
    {
        _service = service;
    }

    /// <summary>Consulta los rieles publicados por el registro de capacidades.</summary>
    [EndpointSummary("Consultar rieles del registro de capacidades")]
    [EndpointDescription("Devuelve el catálogo que el servicio de Capability Registry publica actualmente para consultar capacidades por riel. Cada elemento indica el código, nombre visible y los indicadores IsKnownRail e IsOperational definidos por ese catálogo; esta operación no modifica el registro.")]
    [HttpGet("rails")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentRailRegistryRailItem>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Rieles publicados por el registro de capacidades.")]
    public ActionResult<IReadOnlyList<PaymentRailRegistryRailItem>> GetRails()
        => Ok(_service.GetAvailableRails());

    /// <summary>Consulta las capacidades efectivas de un riel.</summary>
    /// <param name="railCode">Código del riel. El servicio admite los códigos definidos en PaymentRailCodes y responde 400 cuando el valor no está soportado.</param>
    /// <param name="asOfUtc">Instante opcional usado para evaluar overrides vigentes. Si se omite, el servicio usa la hora UTC actual.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar capacidades efectivas de un riel")]
    [EndpointDescription("Devuelve todas las capacidades conocidas por el registro para el riel solicitado. Para cada capacidad, el servicio prioriza un override activo y vigente del registry; cuando no existe, deriva el estado desde la estrategia operacional del riel. Un railCode no soportado se traduce explícitamente a 400.")]
    [HttpGet("rails/{railCode}/capabilities")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentRailCapabilityRegistryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Capacidades efectivas del riel para el instante solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El railCode está vacío o no corresponde a un riel soportado por el servicio.")]
    public async Task<ActionResult<IReadOnlyList<PaymentRailCapabilityRegistryItem>>> GetCapabilitiesByRailAsync(
        string railCode,
        [FromQuery] DateTime? asOfUtc,
        CancellationToken ct)
    {
        try
        {
            var capabilities = await _service.GetEffectiveCapabilitiesByRailAsync(railCode, asOfUtc, ct);
            return Ok(capabilities);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { code = "INVALID_RAIL", message = ex.Message, railCode });
        }
    }

    /// <summary>Consulta una capacidad efectiva de un riel.</summary>
    /// <param name="railCode">Código del riel. Un valor no soportado se responde como 400.</param>
    /// <param name="capabilityCode">Código de capacidad que debe pertenecer al catálogo PaymentRailCapabilityRegistryCodes.</param>
    /// <param name="asOfUtc">Instante opcional usado para evaluar overrides vigentes. Si se omite, el servicio usa la hora UTC actual.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar capacidad efectiva de un riel")]
    [EndpointDescription("Obtiene una capacidad puntual para el riel y el instante solicitados. El controller devuelve 404 cuando capabilityCode no pertenece al catálogo o cuando la capacidad no está disponible; un railCode no soportado se traduce explícitamente a 400. El estado conserva la misma resolución de override vigente o estrategia operacional usada por la consulta de capacidades del riel.")]
    [HttpGet("rails/{railCode}/capabilities/{capabilityCode}")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    [ProducesResponseType(typeof(PaymentRailCapabilityRegistryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Capacidad efectiva encontrada para el riel y el instante solicitados.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El railCode está vacío o no corresponde a un riel soportado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El capabilityCode no pertenece al catálogo o la capacidad no está disponible para el riel solicitado.")]
    public async Task<ActionResult<PaymentRailCapabilityRegistryItem>> GetCapabilityByRailAsync(
        string railCode,
        string capabilityCode,
        [FromQuery] DateTime? asOfUtc,
        CancellationToken ct)
    {
        if (!PaymentRailCapabilityRegistryCodes.All.Contains(capabilityCode, StringComparer.OrdinalIgnoreCase))
        {
            return NotFound(new { code = "CAPABILITY_NOT_FOUND", message = "Capability no registrada en catálogo de gobernanza.", railCode, capabilityCode });
        }

        try
        {
            var capability = await _service.GetEffectiveCapabilityByRailAsync(railCode, capabilityCode, asOfUtc, ct);
            if (capability is null)
            {
                return NotFound(new { code = "CAPABILITY_NOT_FOUND", message = "Capability no disponible para el riel solicitado.", railCode, capabilityCode });
            }

            return Ok(capability);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { code = "INVALID_RAIL", message = ex.Message, railCode });
        }
    }
}
