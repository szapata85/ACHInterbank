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
    private const int MaxDateRangeDays = 31;
    private static readonly TimeSpan DefaultDateRange = TimeSpan.FromDays(7);
    private static readonly TimeSpan ReportGenerationTimeout = TimeSpan.FromSeconds(30);

    private readonly IReportGenerator _reportGenerator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportGenerator reportGenerator, ILogger<ReportsController> logger)
    {
        _reportGenerator = reportGenerator;
        _logger = logger;
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
        var reportName = "traceability";
        var user = User?.Identity?.Name ?? "anonymous";
        var startedAtUtc = DateTime.UtcNow;
        var normalized = NormalizeDateRange(fromUtc, toUtc);

        if (normalized.ValidationError is not null)
        {
            _logger.LogWarning(
                "ReportValidationFailed report={ReportName} user={User} fromUtc={FromUtc} toUtc={ToUtc} state={State} achCycleId={AchCycleId} reason={Reason}",
                reportName,
                user,
                fromUtc,
                toUtc,
                state,
                achCycleId,
                normalized.ValidationError);

            return BadRequest(new { message = normalized.ValidationError });
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["report"] = reportName,
            ["user"] = user,
            ["fromUtc"] = normalized.FromUtc,
            ["toUtc"] = normalized.ToUtc,
            ["state"] = state?.ToString(),
            ["achCycleId"] = achCycleId
        });

        _logger.LogInformation(
            "ReportExecutionStarted report={ReportName} user={User} fromUtc={FromUtc} toUtc={ToUtc} state={State} achCycleId={AchCycleId}",
            reportName,
            user,
            normalized.FromUtc,
            normalized.ToUtc,
            state,
            achCycleId);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ReportGenerationTimeout);

        try
        {
            var file = await _reportGenerator.GenerateTraceabilityPdfAsync(
                new TraceabilityReportFilter
                {
                    FromUtc = normalized.FromUtc,
                    ToUtc = normalized.ToUtc,
                    State = state,
                    AchCycleId = achCycleId
                },
                timeoutCts.Token);

            var elapsedMs = (long)(DateTime.UtcNow - startedAtUtc).TotalMilliseconds;
            _logger.LogInformation(
                "ReportExecutionCompleted report={ReportName} user={User} durationMs={DurationMs} sizeBytes={SizeBytes}",
                reportName,
                user,
                elapsedMs,
                file.Content.Length);

            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            var elapsedMs = (long)(DateTime.UtcNow - startedAtUtc).TotalMilliseconds;
            _logger.LogWarning(
                ex,
                "ReportExecutionTimeout report={ReportName} user={User} durationMs={DurationMs} timeoutSeconds={TimeoutSeconds}",
                reportName,
                user,
                elapsedMs,
                ReportGenerationTimeout.TotalSeconds);

            return StatusCode(StatusCodes.Status408RequestTimeout, new
            {
                message = "La generación del reporte tardó demasiado. Ajusta los filtros e intenta nuevamente."
            });
        }
        catch (Exception ex)
        {
            var elapsedMs = (long)(DateTime.UtcNow - startedAtUtc).TotalMilliseconds;
            _logger.LogError(
                ex,
                "ReportExecutionFailed report={ReportName} user={User} durationMs={DurationMs}",
                reportName,
                user,
                elapsedMs);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "No fue posible generar el reporte en este momento. Intenta de nuevo más tarde."
            });
        }
    }

    private static (DateTime? FromUtc, DateTime? ToUtc, string? ValidationError) NormalizeDateRange(DateTime? fromUtc, DateTime? toUtc)
    {
        DateTime? normalizedFrom = fromUtc;
        DateTime? normalizedTo = toUtc;

        if (!normalizedFrom.HasValue && !normalizedTo.HasValue)
        {
            normalizedTo = DateTime.UtcNow;
            normalizedFrom = normalizedTo.Value.Subtract(DefaultDateRange);
        }
        else if (!normalizedFrom.HasValue && normalizedTo.HasValue)
        {
            normalizedFrom = normalizedTo.Value.AddDays(-MaxDateRangeDays);
        }
        else if (normalizedFrom.HasValue && !normalizedTo.HasValue)
        {
            normalizedTo = normalizedFrom.Value.AddDays(MaxDateRangeDays);
        }

        if (normalizedFrom.HasValue && normalizedTo.HasValue && normalizedFrom.Value > normalizedTo.Value)
        {
            return (normalizedFrom, normalizedTo, "La fecha inicial no puede ser mayor que la fecha final.");
        }

        if (normalizedFrom.HasValue && normalizedTo.HasValue)
        {
            var days = (normalizedTo.Value - normalizedFrom.Value).TotalDays;
            if (days > MaxDateRangeDays)
            {
                return (normalizedFrom, normalizedTo, $"El rango máximo permitido para reportes es de {MaxDateRangeDays} días.");
            }
        }

        return (normalizedFrom, normalizedTo, null);
    }
}
