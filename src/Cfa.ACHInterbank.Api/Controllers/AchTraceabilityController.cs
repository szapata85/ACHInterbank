using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach-traceability")]
public class AchTraceabilityController : ControllerBase
{
    private readonly IAchTraceabilityService _traceabilityService;

    public AchTraceabilityController(IAchTraceabilityService traceabilityService)
    {
        _traceabilityService = traceabilityService;
    }

    [HttpPost("sol02/{transactionId:int}/certify")]
    public async Task<IActionResult> CertifyWithSol02(
        int transactionId,
        [FromBody] Sol02CertificationRequest request,
        CancellationToken ct)
    {
        try
        {
            var transaction = await _traceabilityService.CertifySol02Async(
                transactionId,
                request.CertificationReference,
                request.Notes,
                ct);

            return Ok(new
            {
                message = "Certificación SOL02 aplicada.",
                transactionId = transaction.Id,
                transaction.State,
                transaction.StateChangedAtUtc
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("transactions/{transactionId:int}")]
    public async Task<IActionResult> GetTransactionTraceability(int transactionId, CancellationToken ct)
    {
        var traceability = await _traceabilityService.GetTransactionTraceabilityAsync(transactionId, ct);
        if (traceability is null)
        {
            return NotFound(new { message = $"No existe la transacción ACH {transactionId}." });
        }

        return Ok(traceability);
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetTraceabilityReport(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? achCycleId,
        CancellationToken ct)
    {
        var report = await _traceabilityService.GetTraceabilityReportAsync(fromUtc, toUtc, state, achCycleId, ct);
        return Ok(report);
    }
}

public class Sol02CertificationRequest
{
    public string? CertificationReference { get; set; }
    public string? Notes { get; set; }
}
