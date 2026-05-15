using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-returns/return-of-return")]
[Authorize]
public class AchReturnOfReturnController(
    IAchReturnOfReturnEligibilityService eligibilityService,
    IAchReturnOfReturnFileGenerationService generationService) : ControllerBase
{
    [HttpPost("evaluate")]
    [Authorize(Policy = P0Policies.ReturnsRead)]
    [ProducesResponseType(typeof(AchReturnOfReturnEligibilityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateReturnOfReturnRequest request, CancellationToken ct)
    {
        if (request is null || request.SourceReturnTransactionId <= 0 || string.IsNullOrWhiteSpace(request.NewReturnReasonCode))
        {
            return BadRequest(new { message = "sourceReturnTransactionId y newReturnReasonCode son obligatorios." });
        }

        var result = await eligibilityService.EvaluateAsync(
            new AchReturnOfReturnEligibilityRequest(
                request.SourceReturnTransactionId,
                request.NewReturnReasonCode,
                DateTime.UtcNow,
                request.RequestedBy,
                request.Source),
            ct);

        return Ok(result);
    }

    [HttpPost("generate-audit-file")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateAuditFile([FromBody] GenerateReturnOfReturnAuditFileRequest request, CancellationToken ct)
    {
        if (request is null || request.FlowIds is null || request.FlowIds.Count == 0)
        {
            return BadRequest(new { message = "Debe enviar al menos un flowId." });
        }

        var result = await generationService.GenerateAsync(
            new AchReturnOfReturnFileGenerationRequest(
                request.FlowIds,
                DateTime.UtcNow,
                request.RequestedBy,
                request.Source),
            ct);

        if (!result.IsGenerated || result.Content is null || string.IsNullOrWhiteSpace(result.FileName))
        {
            return Conflict(new
            {
                message = "No fue posible generar el archivo de auditoría Return Of Return.",
                failures = result.Failures,
                flowIds = result.FlowIds
            });
        }

        return File(result.Content, "text/plain", result.FileName);
    }

    [HttpPost("generate-nacha-file")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateNachaFile([FromBody] GenerateReturnOfReturnAuditFileRequest request, CancellationToken ct)
    {
        if (request is null || request.FlowIds is null || request.FlowIds.Count == 0)
        {
            return BadRequest(new { message = "Debe enviar al menos un flowId." });
        }

        var result = await generationService.GenerateNachaAsync(
            new AchReturnOfReturnFileGenerationRequest(
                request.FlowIds,
                DateTime.UtcNow,
                request.RequestedBy,
                request.Source),
            ct);

        if (!result.IsGenerated || result.Content is null || string.IsNullOrWhiteSpace(result.FileName))
        {
            return Conflict(new
            {
                message = "No fue posible generar el archivo NACHA de devolución de devolución.",
                failures = result.Failures,
                flowIds = result.FlowIds
            });
        }

        return File(result.Content, "text/plain", result.FileName);
    }
}

public sealed record EvaluateReturnOfReturnRequest(
    int SourceReturnTransactionId,
    string NewReturnReasonCode,
    string? RequestedBy = null,
    string? Source = null);

public sealed record GenerateReturnOfReturnAuditFileRequest(
    IReadOnlyCollection<int> FlowIds,
    string? RequestedBy = null,
    string? Source = null);
