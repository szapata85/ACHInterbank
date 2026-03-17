using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-returns")]
public class AchReturnsController(IAchReturnsService service) : ControllerBase
{
    [HttpGet("cycles/{cycleId}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<ReturnEligibleTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsByCycle(string cycleId, CancellationToken ct)
    {
        var items = await service.GetTransactionsByCycleAsync(cycleId, ct);
        return Ok(items);
    }

    [HttpPost("generate-file")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateFile([FromBody] GenerateReturnsFileRequest request, CancellationToken ct)
    {
        var response = await service.GenerateReturnsFileAsync(request, ct);
        return File(response.Content, response.ContentType, response.FileName);
    }
}
