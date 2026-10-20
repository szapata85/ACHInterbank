using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-returns/transport")]
[Authorize]
public sealed class AchOutboundReturnTransportController(
    IAchOutboundReturnDispatchService dispatchService,
    IAchOutboundReturnResultProcessor resultProcessor) : ControllerBase
{
    [HttpPost("generate-and-dispatch")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(typeof(AchOutboundReturnDispatchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AchOutboundReturnDispatchResult>> GenerateAndDispatch(
        [FromBody] GenerateAndDispatchApiRequest request,
        CancellationToken ct)
    {
        var result = await dispatchService.GenerateAndDispatchAsync(
            new AchOutboundReturnGenerateDispatchRequest(
                request.Generation,
                request.IdempotencyKey,
                User.Identity?.Name ?? "system"),
            ct);
        return Ok(result);
    }

    [HttpPost("files/{fileName}/dispatch")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(typeof(AchOutboundReturnDispatchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AchOutboundReturnDispatchResult>> DispatchExisting(
        string fileName,
        [FromBody] DispatchExistingApiRequest request,
        CancellationToken ct)
    {
        var result = await dispatchService.DispatchAsync(
            new AchOutboundReturnDispatchRequest(
                fileName,
                request.IdempotencyKey,
                User.Identity?.Name ?? "system"),
            ct);
        return Ok(result);
    }

    [HttpPost("results")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(typeof(AchOutboundReturnResultProcessingResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AchOutboundReturnResultProcessingResult>> ProcessResult(
        [FromBody] AchOutboundReturnResultRequest request,
        CancellationToken ct)
        => Ok(await resultProcessor.ProcessAsync(request, ct));

    public sealed record GenerateAndDispatchApiRequest(
        GenerateReturnsFileRequest Generation,
        string IdempotencyKey);

    public sealed record DispatchExistingApiRequest(string IdempotencyKey);
}
