using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/uat/contrapartidas")]
[Authorize(Policy = "CanManageAch")]
public sealed class UatContrapartidasController : ControllerBase
{
    private readonly IContrapartidaDispatchJobService _dispatchJobService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UatContrapartidasController> _logger;

    public UatContrapartidasController(
        IContrapartidaDispatchJobService dispatchJobService,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration,
        ILogger<UatContrapartidasController>? logger = null)
    {
        _dispatchJobService = dispatchJobService;
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
        _logger = logger ?? NullLogger<UatContrapartidasController>.Instance;
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

        if (request.TransactionId <= 0)
        {
            return BadRequest(new
            {
                errorCode = "UAT_DISPATCH_INVALID_REQUEST",
                message = "transactionId es obligatorio."
            });
        }

        var triggeredBy = User.Identity?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(triggeredBy))
        {
            triggeredBy = "uat-authenticated-user";
        }

        try
        {
            _logger.LogInformation(
                "Dispatch UAT dirigido de Proc_Contrapartidas solicitado para TransactionId {TransactionId} por {TriggeredBy}.",
                request.TransactionId,
                triggeredBy);
            var result = await _dispatchJobService.ProcessTransactionAsync(request.TransactionId, triggeredBy, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { errorCode = "UAT_DISPATCH_TARGET_NOT_FOUND" });
        }
        catch (InvalidOperationException ex) when (
            ex.Message.StartsWith("CONTRAPARTIDA_ALREADY_SUCCEEDED:", StringComparison.Ordinal))
        {
            return Conflict(new
            {
                errorCode = "CONTRAPARTIDA_ALREADY_SUCCEEDED",
                message = "La transacción ya tiene un resultado funcional exitoso."
            });
        }
    }

    private bool IsUatDispatchEnabled()
    {
        if (_hostEnvironment.IsProduction())
        {
            return false;
        }

        var allowedEnvironment = _hostEnvironment.IsDevelopment()
            || string.Equals(_hostEnvironment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(_hostEnvironment.EnvironmentName, "UAT", StringComparison.OrdinalIgnoreCase);
        if (!allowedEnvironment)
        {
            return false;
        }

        var flag = _configuration["ACH_SOAP_LIVE_TESTS"]
            ?? Environment.GetEnvironmentVariable("ACH_SOAP_LIVE_TESTS");

        return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class UatContrapartidasDispatchCycleRequest
{
    public int TransactionId { get; set; }
}
