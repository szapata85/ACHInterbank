using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Audit.Dtos;
using Cfa.ACHInterbank.Application.Audit.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
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

    /// <summary>Consulta los registros de auditoría persistidos.</summary>
    /// <param name="startDate">Límite inferior opcional de ChangedAt. Si se informa a medianoche, se usa el inicio de ese día.</param>
    /// <param name="endDate">Límite superior opcional de ChangedAt. Si se informa a medianoche, se amplía hasta el último tick de ese día.</param>
    /// <param name="changedBy">Texto opcional buscado por contenido en ChangedBy después de eliminar espacios exteriores.</param>
    /// <param name="action">Acción opcional comparada por igualdad sin distinguir mayúsculas después de eliminar espacios exteriores.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 50.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar registros de auditoría")]
    [EndpointDescription("Consulta los registros de auditoría aplicando los filtros opcionales startDate, endDate, changedBy y action. El servicio normaliza page a 1 y pageSize a 50 cuando reciben valores menores o iguales a cero, ordena por ChangedAt descendente y devuelve un BaseResponseModel cuyo data contiene PagedResponse<AuditLogDto>.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.AuditLogsRead)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de registros de auditoría que cumplen los filtros solicitados, envuelta en BaseResponseModel.")]
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
