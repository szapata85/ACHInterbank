using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/uat/contrapartidas")]
[Authorize(Policy = "CanManageAch")]
public sealed class UatContrapartidasController : ControllerBase
{
    private readonly IContrapartidaDispatchJobService _dispatchJobService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;

    public UatContrapartidasController(
        IContrapartidaDispatchJobService dispatchJobService,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        _dispatchJobService = dispatchJobService;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
    }

    [HttpPost("dispatch-cycle")]
    [ProducesResponseType(typeof(ContrapartidaCycleDispatchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DispatchCycle([FromBody] UatContrapartidasDispatchCycleRequest request, CancellationToken ct)
    {
        if (!IsUatDispatchEnabled())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.CycleId) || request.ClearingHouseId <= 0)
        {
            return BadRequest(new
            {
                errorCode = "UAT_DISPATCH_INVALID_REQUEST",
                message = "cycleId y clearingHouseId son obligatorios."
            });
        }

        var cycleId = request.CycleId.Trim();
        var triggeredBy = string.IsNullOrWhiteSpace(request.TriggeredBy) ? "g34-playwright" : request.TriggeredBy.Trim();
        var result = request.TransactionId.HasValue
            ? await _dispatchJobService.ProcessTransactionAsync(
                cycleId,
                request.ClearingHouseId,
                request.TransactionId.Value,
                triggeredBy,
                ct)
            : await _dispatchJobService.ProcessCycleAsync(
                cycleId,
                request.ClearingHouseId,
                triggeredBy,
                request.ChunkSize <= 0 ? 50 : request.ChunkSize,
                ct);

        return Ok(result);
    }

    private bool IsUatDispatchEnabled()
    {
        if (_hostEnvironment.IsDevelopment()
            || string.Equals(_hostEnvironment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var flag = _configuration["RUN_UAT_TRANSACTION_NACHA_DISPATCH"]
            ?? Environment.GetEnvironmentVariable("RUN_UAT_TRANSACTION_NACHA_DISPATCH");

        if (string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return TryReadRequestFlag("X-UAT-Transaction-Nacha-Dispatch");
    }

    private bool TryReadRequestFlag(string headerName)
    {
        var controllerContext = ControllerContext;
        var httpContext = controllerContext?.HttpContext;
        if (httpContext is null)
        {
            return false;
        }

        var request = httpContext.Request;
        if (!request.Headers.TryGetValue(headerName, out var headerValue))
        {
            return false;
        }

        return string.Equals(headerValue.ToString(), "true", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class UatContrapartidasDispatchCycleRequest
{
    public string CycleId { get; set; } = string.Empty;

    public int ClearingHouseId { get; set; }

    public string? TriggeredBy { get; set; }

    public int ChunkSize { get; set; } = 50;

    public int? TransactionId { get; set; }
}
