using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.NavigationLogs.Dtos;
using Cfa.ACHInterbank.Application.NavigationLogs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/navigation-logs")]
[Authorize]
public class NavigationLogsController : ControllerBase
{
    private readonly INavigationLogsService _service;

    public NavigationLogsController(INavigationLogsService service)
    {
        _service = service;
    }

    /// <summary>Consulta registros de navegación.</summary>
    /// <param name="startDate">Límite inferior opcional de VisitedAt. Si se informa a medianoche, se usa el inicio de ese día.</param>
    /// <param name="endDate">Límite superior opcional de VisitedAt. Si se informa a medianoche, se amplía hasta el último tick de ese día.</param>
    /// <param name="userId">Texto opcional buscado por contenido en el UserId persistido o en Username/FullName del usuario relacionado.</param>
    /// <param name="route">Texto opcional buscado por contenido en la ruta persistida después de eliminar espacios exteriores.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 50.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar registros de navegación")]
    [EndpointDescription("Consulta los registros de navegación aplicando los filtros opcionales startDate, endDate, userId y route. userId también busca por Username o FullName del usuario relacionado. El servicio normaliza page a 1 y pageSize a 50 cuando reciben valores menores o iguales a cero, ordena por VisitedAt descendente y devuelve un BaseResponseModel cuyo data contiene PagedResponse<NavigationLogDto>.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.NavigationLogsRead)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de registros de navegación que cumplen los filtros solicitados, envuelta en BaseResponseModel.")]
    public async Task<IActionResult> GetNavigationLogsAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? userId,
        [FromQuery] string? route,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetAsync(new NavigationLogQuery
        {
            StartDate = startDate,
            EndDate = endDate,
            UserId = userId,
            Route = route,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, result));
    }

    /// <summary>Registra una navegación del usuario autenticado.</summary>
    /// <param name="request">Ruta visitada y metadata opcional de la navegación.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Registrar navegación")]
    [EndpointDescription("Registra una navegación asociada al usuario autenticado. El servicio elimina query string y fragmento de Route, asegura un slash inicial y limita la ruta a 300 caracteres; SessionId se recorta a 100 caracteres; DurationMs solo se conserva entre 1 y 86400000; VisitedAt se convierte a UTC o usa la hora UTC actual. El controller obtiene el usuario de los claims y captura IP y User-Agent de la solicitud. Una ruta vacía produce 400; el éxito produce 200.")]
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Registro de navegación persistido; la respuesta usa BaseResponseModel sin data de negocio adicional.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La ruta de navegación está vacía después de normalizarla; la respuesta usa BaseResponseModel con el error NAV_ROUTE_REQUIRED.")]
    public async Task<IActionResult> AddNavigationLogAsync([FromBody] NavigationLogCreate request, CancellationToken cancellationToken = default)
    {
        var result = await _service.AddAsync(
            request,
            GetCurrentUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, result));
        }

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, Result.Success()));
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue("uid")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.Identity?.Name;
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return NormalizeIp(first);
            }
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        return remoteIp is null ? null : NormalizeIp(remoteIp.ToString());
    }

    private static string NormalizeIp(string value)
    {
        if (IPAddress.TryParse(value, out var parsed) && parsed.IsIPv4MappedToIPv6)
        {
            return parsed.MapToIPv4().ToString();
        }

        return value;
    }
}
