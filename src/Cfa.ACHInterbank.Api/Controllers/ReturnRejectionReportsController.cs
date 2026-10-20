using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
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
public sealed class ReturnRejectionReportsController : ReportsClearingHouseControllerBase
{
    private readonly IReportGenerator _reportGenerator;
    private readonly IAchReturnRejectionReportService _returnRejectionReportService;

    public ReturnRejectionReportsController(
        IReportGenerator reportGenerator,
        IAchReturnRejectionReportService returnRejectionReportService,
        IClearingHouseService clearingHouseService)
        : base(clearingHouseService)
    {
        _reportGenerator = reportGenerator;
        _returnRejectionReportService = returnRejectionReportService;
    }

    /// <summary>Reporte de devoluciones.</summary>
    /// <param name="date">Fecha opcional aplicada sobre StateChangedAtUtc.Date.</param>
    /// <param name="causal">Código de causal opcional; se recorta y convierte a mayúsculas antes de compararlo por igualdad.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="state">Estado ACH opcional.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de devoluciones")]
    [EndpointDescription("Consulta transacciones cuyo ReturnReasonCode coincide con R__ o DEV__ según los patrones SQL implementados. date compara StateChangedAtUtc.Date; causal se recorta, se convierte a mayúsculas y se compara por igualdad; reference busca por contenido en Reference o TransactionExternalId. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. clearingHouseId, cuando se informa, debe ser mayor que cero y existir; en caso contrario responde 400 con body anónimo { message }.")]
    [HttpGet("returns")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchReturnRejectionReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reporte paginado de devoluciones con items, totales y paginación efectiva.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
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
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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

    /// <summary>Reporte de rechazos.</summary>
    /// <param name="date">Fecha opcional aplicada sobre StateChangedAtUtc.Date.</param>
    /// <param name="causal">Código de causal opcional; se recorta y convierte a mayúsculas antes de compararlo por igualdad.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="state">Estado ACH opcional.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de rechazos")]
    [EndpointDescription("Consulta transacciones cuyo ReturnReasonCode coincide con el patrón SQL D__ implementado. date compara StateChangedAtUtc.Date; causal se recorta, se convierte a mayúsculas y se compara por igualdad; reference busca por contenido en Reference o TransactionExternalId. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. clearingHouseId, cuando se informa, debe ser mayor que cero y existir; en caso contrario responde 400 con body anónimo { message }.")]
    [HttpGet("rejections")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchReturnRejectionReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reporte paginado de rechazos con items, totales y paginación efectiva.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
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
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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

    /// <summary>Descargar PDF de devoluciones.</summary>
    /// <param name="date">Fecha opcional aplicada sobre StateChangedAtUtc.Date.</param>
    /// <param name="causal">Código de causal opcional; se recorta y convierte a mayúsculas antes de compararlo por igualdad.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="state">Estado ACH opcional.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de devoluciones")]
    [EndpointDescription("Genera un PDF de devoluciones usando los filtros de la consulta correspondiente. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todo el conjunto filtrado. Responde application/pdf con nombre ACH_Returns_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("returns/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Returns_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetReturnsPdf(
        [FromQuery] DateTime? date,
        [FromQuery] string? causal,
        [FromQuery] int? clearingHouseId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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

    /// <summary>Descargar PDF de rechazos.</summary>
    /// <param name="date">Fecha opcional aplicada sobre StateChangedAtUtc.Date.</param>
    /// <param name="causal">Código de causal opcional; se recorta y convierte a mayúsculas antes de compararlo por igualdad.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="state">Estado ACH opcional.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de rechazos")]
    [EndpointDescription("Genera un PDF de rechazos usando los filtros de la consulta correspondiente. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todo el conjunto filtrado. Responde application/pdf con nombre ACH_Rejections_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("rejections/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Rejections_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
    public async Task<IActionResult> GetRejectionsPdf(
        [FromQuery] DateTime? date,
        [FromQuery] string? causal,
        [FromQuery] int? clearingHouseId,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? reference,
        CancellationToken ct = default)
    {
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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
}
