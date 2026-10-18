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
public sealed class NachaCycleReportsController : ReportsClearingHouseControllerBase
{
    private readonly IReportGenerator _reportGenerator;
    private readonly IAchNachaCycleReportService _nachaCycleReportService;

    public NachaCycleReportsController(
        IReportGenerator reportGenerator,
        IAchNachaCycleReportService nachaCycleReportService,
        IClearingHouseService clearingHouseService)
        : base(clearingHouseService)
    {
        _reportGenerator = reportGenerator;
        _nachaCycleReportService = nachaCycleReportService;
    }

    /// <summary>Reporte de archivos NACHA.</summary>
    /// <param name="date">Fecha opcional aplicada sobre GeneratedAtUtc.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de archivos NACHA")]
    [EndpointDescription("Consulta registros de exportación de archivos NACHA. date compara GeneratedAtUtc.Date y clearingHouseId filtra la cámara asociada. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los totales se calculan sobre todo el conjunto filtrado antes de paginar. clearingHouseId, cuando se informa, debe ser mayor que cero y existir; en caso contrario responde 400 con body anónimo { message }.")]
    [HttpGet("nacha-files")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchNachaFileReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reporte paginado de archivos NACHA con items, totales y paginación efectiva.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetNachaFiles(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

        var response = await _nachaCycleReportService.GetNachaFilesAsync(
            new AchNachaFileReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    /// <summary>Descargar PDF de archivos NACHA.</summary>
    /// <param name="date">Fecha opcional aplicada sobre GeneratedAtUtc.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de archivos NACHA")]
    [EndpointDescription("Genera un PDF de archivos NACHA con filtros opcionales por fecha y cámara. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todo el conjunto filtrado. Responde application/pdf con nombre ACH_NachaFiles_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("nacha-files/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_NachaFiles_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetNachaFilesPdf(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

        var file = await _reportGenerator.GenerateNachaFilesPdfAsync(
            new AchNachaFileReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Reporte de ciclos ACH.</summary>
    /// <param name="date">Fecha opcional aplicada sobre ProcessingDate.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="name">Texto opcional buscado por contenido en CycleName después de recortarlo.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de ciclos ACH")]
    [EndpointDescription("Consulta ciclos ACH. date compara ProcessingDate.Date, clearingHouseId filtra la cámara y name se recorta antes de buscarlo por contenido en CycleName. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los totales de transacciones y monto se calculan sobre todos los ciclos filtrados antes de paginar; cada item deriva Status como Programado, En ejecución o Cerrado comparando DateTime.UtcNow con StartTime/EndTime del ciclo, considerando fin al día siguiente cuando EndTime es menor que StartTime. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("cycles")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchCycleReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reporte paginado de ciclos ACH con items, totales y paginación efectiva.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetCycles(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? name,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

        var response = await _nachaCycleReportService.GetCyclesAsync(
            new AchCycleReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                Name = name,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    /// <summary>Descargar PDF de ciclos ACH.</summary>
    /// <param name="date">Fecha opcional aplicada sobre ProcessingDate.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="name">Texto opcional buscado por contenido en CycleName después de recortarlo.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de ciclos ACH")]
    [EndpointDescription("Genera un PDF de ciclos ACH con filtros opcionales por fecha, cámara y nombre. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todos los ciclos filtrados. Responde application/pdf con nombre ACH_Cycles_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("cycles/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Cycles_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetCyclesPdf(
        [FromQuery] DateTime? date,
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? name,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

        var file = await _reportGenerator.GenerateCyclesPdfAsync(
            new AchCycleReportFilter
            {
                Date = date,
                ClearingHouseId = clearingHouseId,
                Name = name,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }
}
