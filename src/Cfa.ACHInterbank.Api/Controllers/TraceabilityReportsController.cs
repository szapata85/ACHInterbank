using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/reports")]
[ControllerContractName("Reports")]
[Authorize]
public sealed class TraceabilityReportsController : ControllerBase
{
    private const int MaxDateRangeDays = 31;
    private static readonly TimeSpan DefaultDateRange = TimeSpan.FromDays(7);
    private static readonly TimeSpan ReportGenerationTimeout = TimeSpan.FromSeconds(30);

    private const string LegacyLoggerCategory = "Cfa.ACHInterbank.Api.Controllers.ReportsController";

    private readonly IReportGenerator _reportGenerator;
    private readonly ILogger _logger;

    public TraceabilityReportsController(
        IReportGenerator reportGenerator,
        ILoggerFactory loggerFactory)
    {
        _reportGenerator = reportGenerator;
        _logger = loggerFactory.CreateLogger(LegacyLoggerCategory);
    }

    /// <summary>Descarga trazabilidad ACH en PDF.</summary>
    /// <param name="fromUtc">Inicio UTC opcional del rango. Si se omite junto con toUtc, se usa ahora UTC menos 7 días; si solo se omite este valor, se usa toUtc menos 31 días.</param>
    /// <param name="toUtc">Fin UTC opcional del rango. Si se omite junto con fromUtc, se usa ahora UTC; si solo se omite este valor, se usa fromUtc más 31 días.</param>
    /// <param name="state">Estado ACH opcional.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH remitido al generador de trazabilidad.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de trazabilidad ACH")]
    [EndpointDescription("Genera un PDF de trazabilidad con rango UTC, estado y ciclo opcionales. Sin fechas usa un rango de 7 días terminado en DateTime.UtcNow; si falta solo un extremo completa 31 días desde el extremo informado. Rechaza con 400 cuando fromUtc es posterior a toUtc o cuando el rango supera 31 días. La generación tiene timeout interno de 30 segundos: si vence sin cancelación del cliente responde 408; una excepción no controlada por el generador se traduce a 500. El éxito usa application/pdf con nombre ACH_Traceability_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre.")]
    [HttpGet("traceability/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Traceability_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El rango es inválido porque fromUtc es posterior a toUtc o porque supera el máximo de 31 días; el body anónimo contiene message.")]
    [OpenApiResponseDescription(StatusCodes.Status408RequestTimeout, "La generación excedió el timeout interno de 30 segundos sin cancelación del cliente; el body anónimo contiene message.")]
    [OpenApiResponseDescription(StatusCodes.Status500InternalServerError, "La generación lanzó una excepción capturada por la acción; el body anónimo contiene message.")]
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
