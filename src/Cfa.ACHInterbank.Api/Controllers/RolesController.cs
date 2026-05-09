using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRolesService _service;

    public RolesController(IRolesService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = P1Policies.RolesRead)]
    public async Task<ActionResult<IEnumerable<RoleSummaryDto>>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _service.GetAllAsync(cancellationToken);

        return Ok(roles);
    }
}
