using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("incoming-nacha-command-center")]
[Authorize(Policy = "CanReadAch")]
public class IncomingNachaCommandCenterController : ControllerBase
{
    private static IActionResult MapInvalidOperation(InvalidOperationException ex)
        => new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status409Conflict };
    private readonly IIncomingNachaCommandCenterService _service;

    public IncomingNachaCommandCenterController(IIncomingNachaCommandCenterService service)
    {
        _service = service;
    }

    [HttpGet("ingestions")]
    public async Task<IActionResult> GetIngestions([FromQuery] IncomingNachaIngestionQuery query, CancellationToken ct)
        => Ok(await _service.GetIngestionsAsync(query, ct));

    [HttpGet("ingestions/{ingestionId:guid}")]
    public async Task<IActionResult> GetIngestionDetail(Guid ingestionId, CancellationToken ct)
    {
        var result = await _service.GetIngestionDetailAsync(ingestionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] IncomingNachaQueueQuery query, CancellationToken ct)
        => Ok(await _service.GetQueueAsync(query, ct));

    [HttpGet("queue/{queueId:guid}")]
    public async Task<IActionResult> GetQueueDetail(Guid queueId, CancellationToken ct)
    {
        var result = await _service.GetQueueDetailAsync(queueId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("queue/{queueId:guid}/retry")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> RetryManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.RetryManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    [HttpPost("queue/{queueId:guid}/unblock")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UnblockManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UnblockManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    [HttpPost("queue/{queueId:guid}/requeue")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> RequeueManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.RequeueManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    [HttpPost("queue/{queueId:guid}/mark-failed-final")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> MarkFailedFinal(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.MarkFailedFinalManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }
}
