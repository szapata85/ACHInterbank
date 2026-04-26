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

    [EndpointSummary("Catálogo de rieles de pago habilitados")]
    [EndpointDescription("Qué hace: retorna rieles disponibles en el registro de capacidades. Cuándo se usa: al consultar alcance funcional por riel. Perfil consumidor: arquitectura de pagos y equipos de integración. Permiso requerido: FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry. Tipo de operación: solo consulta. Genera auditoría: sí, por logs de consulta. Riesgos operativos: usar catálogo desactualizado afecta decisiones de integración. Errores esperados: 401/403 por permiso. Relación ACH/CENIT/NACHA-M: gobernanza de capacidades ACH/CENIT y rieles complementarios. Precauciones para desarrollo u operación: sincronizar consumo con vigencia normativa.")]
    [HttpGet("rails")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    public ActionResult<IReadOnlyList<PaymentRailRegistryRailItem>> GetRails()
        => Ok(_service.GetAvailableRails());

    [EndpointSummary("Capacidades vigentes por riel")]
    [EndpointDescription("Qué hace: consulta capacidades efectivas del riel para una fecha dada. Cuándo se usa: en diseño de productos y validación de compatibilidad. Perfil consumidor: arquitectos de integración y operación. Permiso requerido: FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas. Riesgos operativos: código de riel inválido provoca decisiones erróneas. Errores esperados: 400 riel inválido; 401/403. Relación ACH/CENIT/NACHA-M: define qué capacidades ACH/CENIT están disponibles por riel. Precauciones para desarrollo u operación: enviar asOfUtc coherente con fecha operativa.")]
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

    [EndpointSummary("Detalle de capacidad por riel")]
    [EndpointDescription("Qué hace: retorna una capacidad específica y su estado efectivo. Cuándo se usa: al validar una regla puntual de interoperabilidad. Perfil consumidor: arquitectura, QA y cumplimiento. Permiso requerido: FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas. Riesgos operativos: capabilityCode incorrecto produce falsos negativos. Errores esperados: 404 capacidad no encontrada; 400 riel inválido; 401/403. Relación ACH/CENIT/NACHA-M: alinea implementación con matriz de capacidades ACH/CENIT. Precauciones para desarrollo u operación: validar códigos oficiales antes de invocar.")]
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
