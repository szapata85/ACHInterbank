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

    [EndpointSummary("Inventario operativo de rieles en Capability Registry")]
    [EndpointDescription("Qué consulta: lista los rieles de pago que participan en PaymentRail Capability Registry para análisis administrativo. Para qué perfil operativo sirve: arquitectura de pagos, gobierno ACH/CENIT, QA funcional y auditoría técnica que validan cobertura por riel antes de decisiones de integración. Permiso requerido: FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry, con fallback de autorización a CanManageAch o CanReadAch según la política registrada en DependencyInjection. Solo lectura: sí, no persiste ni modifica datos. Significado del resultado: cada railCode indica un riel disponible para consultar capacidades efectivas. Gobernanza y auditoría: el acceso queda trazable en logs y permite evidencia de consulta regulatoria. Precaución operacional: no usar esta consulta como señal de habilitación productiva automática; confirmar ventana operativa y controles de release. Relación con PaymentRail, ACH y CENIT: define alcance de observabilidad de capacidades que afectan compensación ACH/CENIT en modo shadow. Advertencia: este endpoint no ejecuta cutover, no altera capacidades y no cambia el comportamiento legacy vigente.")]
    [HttpGet("rails")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentRailRegistryRailItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<IReadOnlyList<PaymentRailRegistryRailItem>> GetRails()
        => Ok(_service.GetAvailableRails());

    [EndpointSummary("Matriz efectiva de capacidades por riel y fecha operativa")]
    [EndpointDescription("Qué consulta: devuelve las capacidades efectivas de un riel (railCode) para la fecha asOfUtc solicitada. Para qué perfil operativo sirve: operación ACH, arquitectura PaymentRail, compliance y soporte que verifican si una capacidad está gobernada por override o por estrategia. Permiso requerido: FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry, con fallback a CanManageAch o CanReadAch en la política de autorización. Solo lectura: sí, únicamente lectura del estado efectivo. Significado del resultado: Source=RegistryOverride indica que existe una sobreescritura auditable en el registro; Source=StrategyDefault indica estado heredado desde la estrategia operativa del riel. Gobernanza y auditoría: permite demostrar trazabilidad de decisiones de habilitación sin modificar la estrategia legacy. Precaución operacional: railCode inválido retorna 400 y no debe interpretarse como indisponibilidad transaccional; validar catálogo oficial del riel. Relación con PaymentRail, ACH y CENIT: visibiliza cómo el riel opera en paralelo/shadow respecto al circuito ACH/CENIT. Advertencia: no ejecuta cutover, no habilita cambios en producción y no modifica reglas legacy ACH/CENIT.")]
    [HttpGet("rails/{railCode}/capabilities")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentRailCapabilityRegistryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    [EndpointSummary("Estado efectivo de una capacidad específica por riel")]
    [EndpointDescription("Qué consulta: obtiene la capacidad puntual (capabilityCode) para un riel y fecha operativa. Para qué perfil operativo sirve: validación QA, auditoría de gobierno funcional y equipos de integración que necesitan confirmar una capacidad antes de pruebas o monitoreo. Permiso requerido: FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry con fallback a CanManageAch o CanReadAch según la política. Solo lectura: sí, sin escritura en registry ni estrategia. Significado del resultado: cuando Source=RegistryOverride existe override auditable explícito; cuando Source=StrategyDefault la capacidad conserva el valor calculado por la estrategia del riel. Gobernanza y auditoría: diferencia explícitamente control administrativo versus valor base operativo para evidencia de cumplimiento. Precaución operacional: capabilityCode no catalogado o no disponible retorna 404; railCode inválido retorna 400; ambos casos deben tratarse como validación de catálogo, no como cambio de estado transaccional. Relación con PaymentRail, ACH y CENIT: habilita inspección controlada del comportamiento esperado en paralelo con el flujo legacy ACH/CENIT. Advertencia: no ejecuta cutover, no altera capacidades y no cambia comportamiento legacy ni reglas criptográficas.")]
    [HttpGet("rails/{railCode}/capabilities/{capabilityCode}")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    [ProducesResponseType(typeof(PaymentRailCapabilityRegistryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
