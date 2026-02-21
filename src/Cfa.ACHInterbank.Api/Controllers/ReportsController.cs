using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportGenerator _reportGenerator;

    public ReportsController(IReportGenerator reportGenerator)
    {
        _reportGenerator = reportGenerator;
    }

    [HttpGet("traceability/pdf")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetTraceabilityPdf(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? achCycleId,
        CancellationToken ct)
    {
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value > toUtc.Value)
        {
            return BadRequest(new { message = "La fecha inicial no puede ser mayor que la fecha final." });
        }

        var file = await _reportGenerator.GenerateTraceabilityPdfAsync(
            new TraceabilityReportFilter
            {
                FromUtc = fromUtc,
                ToUtc = toUtc,
                State = state,
                AchCycleId = achCycleId
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }
}

