using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/reports")]
[ControllerContractName("Reports")]
[Authorize]
public sealed class ReconciliationReportsController : ReportsClearingHouseControllerBase
{
    private readonly IReportGenerator _reportGenerator;
    private readonly IAchReconciliationReportService _reconciliationReportService;

    public ReconciliationReportsController(
        IReportGenerator reportGenerator,
        IAchReconciliationReportService reconciliationReportService,
        IClearingHouseService clearingHouseService)
        : base(clearingHouseService)
    {
        _reportGenerator = reportGenerator;
        _reconciliationReportService = reconciliationReportService;
    }

    /// <summary>Consulta el reporte de conciliación ACH.</summary>
    /// <param name="date">Fecha opcional comparada con <c>EffectiveEntryDate.Date</c>.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH; se recorta antes de compararlo por igualdad.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de conciliación ACH")]
    [EndpointDescription("Consulta totales, diferencias e inconsistencias de conciliación sobre transacciones ACH. date compara EffectiveEntryDate.Date; clearingHouseId filtra AchCycle.ClearingHouseId y, cuando se informa, debe ser mayor que cero y corresponder a una cámara existente; achCycleId se recorta y compara por igualdad. Los totales enviados excluyen Type=Return, los recibidos incluyen estados Certified o AppliedTacitly y los devueltos incluyen ReturnedByOperator o ReturnedByEpr. Las inconsistencias actuales detectan devoluciones sin causal, causal registrada en transacciones no devueltas y montos negativos. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("reconciliation")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchReconciliationReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Totales enviados, recibidos y devueltos, diferencias entre esos totales e inconsistencias detectadas para los filtros solicitados.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetReconciliation(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? achCycleId,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

        var response = await _reconciliationReportService.GetReconciliationAsync(
            new AchReconciliationReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                AchCycleId = achCycleId
            },
            ct);

        return Ok(response);
    }

    /// <summary>Descarga el reporte de conciliación ACH en PDF.</summary>
    /// <param name="date">Fecha opcional comparada con <c>EffectiveEntryDate.Date</c>.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH; se recorta antes de compararlo por igualdad.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de conciliación ACH")]
    [EndpointDescription("Genera el PDF de conciliación con los mismos filtros del reporte JSON. Responde application/pdf con nombre ACH_Reconciliation_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("reconciliation/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Reconciliation_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetReconciliationPdf(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? achCycleId,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

        var file = await _reportGenerator.GenerateReconciliationPdfAsync(
            new AchReconciliationReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                AchCycleId = achCycleId
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }
}
