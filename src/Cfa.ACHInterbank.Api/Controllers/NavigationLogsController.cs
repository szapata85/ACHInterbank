using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.NavigationLogs.Dtos;
using Cfa.ACHInterbank.Application.NavigationLogs.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
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

    [HttpPost]
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
