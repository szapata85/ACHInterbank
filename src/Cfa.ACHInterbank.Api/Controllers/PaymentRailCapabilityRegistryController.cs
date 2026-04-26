using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

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

    [EndpointSummary("GET rails: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'rails'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("rails")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    public ActionResult<IReadOnlyList<PaymentRailRegistryRailItem>> GetRails()
        => Ok(_service.GetAvailableRails());

    [EndpointSummary("GET rails/{railCode}/capabilities: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'rails/{railCode}/capabilities'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("rails/{railCode}/capabilities")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
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

    [EndpointSummary("GET rails/{railCode}/capabilities/{capabilityCode}: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'rails/{railCode}/capabilities/{capabilityCode}'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("rails/{railCode}/capabilities/{capabilityCode}")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
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
