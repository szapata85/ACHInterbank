using Cfa.ACHInterbank.Application.Audit.Dtos;
using Cfa.ACHInterbank.Application.Audit.Interfaces;
using Cfa.ACHInterbank.Application.Common;
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
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogDto>>> GetAuditLogsAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? changedBy,
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await _service.GetAsync(new AuditLogQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            ChangedBy = changedBy,
            Action = action,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(response);
    }
}
