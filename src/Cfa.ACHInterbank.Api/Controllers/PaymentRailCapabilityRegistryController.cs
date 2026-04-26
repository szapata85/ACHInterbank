using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
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

    [HttpGet("rails")]
    [Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
    public ActionResult<IReadOnlyList<PaymentRailRegistryRailItem>> GetRails()
        => Ok(_service.GetAvailableRails());

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
