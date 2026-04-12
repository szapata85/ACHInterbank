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
    private readonly IAchTransactionReportService _transactionReportService;
    private readonly IAchReturnRejectionReportService _returnRejectionReportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportGenerator reportGenerator,
        IAchTransactionReportService transactionReportService,
        IAchReturnRejectionReportService returnRejectionReportService,
        ILogger<ReportsController> logger)
    {
        _reportGenerator = reportGenerator;
        _transactionReportService = transactionReportService;
        _returnRejectionReportService = returnRejectionReportService;
        _logger = logger;
    }

    [HttpGet("transactions/sent")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetSentTransactions(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? achCycleId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        [FromQuery] int? bankId,
        [FromQuery] TransactionTypeEnum? transactionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var response = await _transactionReportService.GetSentTransactionsAsync(
            new AchTransactionReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                AchCycleId = achCycleId,
                State = state,
                Reference = reference,
                BankId = bankId,
                TransactionType = transactionType,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    [HttpGet("transactions/received")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetReceivedTransactions(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? achCycleId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        [FromQuery] int? bankId,
        [FromQuery] TransactionTypeEnum? transactionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var response = await _transactionReportService.GetReceivedTransactionsAsync(
            new AchTransactionReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                AchCycleId = achCycleId,
                State = state,
                Reference = reference,
                BankId = bankId,
                TransactionType = transactionType,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    [HttpGet("transactions/sent/pdf")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetSentTransactionsPdf(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? achCycleId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        [FromQuery] int? bankId,
        [FromQuery] TransactionTypeEnum? transactionType,
        CancellationToken ct = default)
    {
        var file = await _reportGenerator.GenerateSentTransactionsPdfAsync(
            new AchTransactionReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                AchCycleId = achCycleId,
                State = state,
                Reference = reference,
                BankId = bankId,
                TransactionType = transactionType,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("transactions/received/pdf")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetReceivedTransactionsPdf(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? achCycleId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        [FromQuery] int? bankId,
        [FromQuery] TransactionTypeEnum? transactionType,
        CancellationToken ct = default)
    {
        var file = await _reportGenerator.GenerateReceivedTransactionsPdfAsync(
            new AchTransactionReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                AchCycleId = achCycleId,
                State = state,
                Reference = reference,
                BankId = bankId,
                TransactionType = transactionType,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }


    [HttpGet("returns")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetReturns(
        [FromQuery] DateTime? date,
        [FromQuery] string? causal,
        [FromQuery] int? clearingHouseId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var response = await _returnRejectionReportService.GetReturnsAsync(
            new AchReturnRejectionReportFilter
            {
                Date = date,
                Causal = causal,
                ClearingHouseId = clearingHouseId,
                State = state,
                Reference = reference,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    [HttpGet("rejections")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetRejections(
        [FromQuery] DateTime? date,
        [FromQuery] string? causal,
        [FromQuery] int? clearingHouseId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var response = await _returnRejectionReportService.GetRejectionsAsync(
            new AchReturnRejectionReportFilter
            {
                Date = date,
                Causal = causal,
                ClearingHouseId = clearingHouseId,
                State = state,
                Reference = reference,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    [HttpGet("returns/pdf")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetReturnsPdf(
        [FromQuery] DateTime? date,
        [FromQuery] string? causal,
        [FromQuery] int? clearingHouseId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        CancellationToken ct = default)
    {
        var file = await _reportGenerator.GenerateReturnsPdfAsync(
            new AchReturnRejectionReportFilter
            {
                Date = date,
                Causal = causal,
                ClearingHouseId = clearingHouseId,
                State = state,
                Reference = reference,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("rejections/pdf")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetRejectionsPdf(
        [FromQuery] DateTime? date,
        [FromQuery] string? causal,
        [FromQuery] int? clearingHouseId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        CancellationToken ct = default)
    {
        var file = await _reportGenerator.GenerateRejectionsPdfAsync(
            new AchReturnRejectionReportFilter
            {
                Date = date,
                Causal = causal,
                ClearingHouseId = clearingHouseId,
                State = state,
                Reference = reference,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
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
