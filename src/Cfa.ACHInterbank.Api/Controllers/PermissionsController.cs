using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionsService _service;

    public PermissionsController(IPermissionsService service)
    {
        _service = service;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionSummaryDto>>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        var permissions = await _service.GetAllAsync(cancellationToken);

        return Ok(permissions);
    }
}
