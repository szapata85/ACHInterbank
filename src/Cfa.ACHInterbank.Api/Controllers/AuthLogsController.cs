using Cfa.ACHInterbank.Application.AuthLogs.Dtos;
using Cfa.ACHInterbank.Application.AuthLogs.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/auth-logs")]
[Authorize]
public class AuthLogsController : ControllerBase
{
    private readonly IAuthLogsService _service;

    public AuthLogsController(IAuthLogsService service)
    {
        _service = service;
    }

    /// <summary>
    /// Pendiente de documentación.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuthLogDto>>> GetAuthLogsAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? username,
        [FromQuery] bool? success,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await _service.GetAsync(new AuthLogQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            Username = username,
            Success = success,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(response);
    }
}
