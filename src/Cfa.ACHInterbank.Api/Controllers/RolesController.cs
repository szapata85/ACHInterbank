using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
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
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleSummaryDto>>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _service.GetAllAsync(cancellationToken);

        return Ok(roles);
    }
}
