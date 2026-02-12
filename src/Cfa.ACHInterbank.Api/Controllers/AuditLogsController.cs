using Cfa.ACHInterbank.Application.Audit.Dtos;
using Cfa.ACHInterbank.Application.Audit.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogsService _service;

    public AuditLogsController(IAuditLogsService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    public async Task<IActionResult> GetAuditLogsAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? changedBy,
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAsync(new AuditLogQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            ChangedBy = changedBy,
            Action = action,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, result));
    }
}
