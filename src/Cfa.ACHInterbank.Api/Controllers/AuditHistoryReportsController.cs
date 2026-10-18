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
public sealed class AuditHistoryReportsController : ControllerBase
{
    private readonly IReportGenerator _reportGenerator;
    private readonly IAchAuditHistoryReportService _auditHistoryReportService;

    public AuditHistoryReportsController(
        IReportGenerator reportGenerator,
        IAchAuditHistoryReportService auditHistoryReportService)
    {
        _reportGenerator = reportGenerator;
        _auditHistoryReportService = auditHistoryReportService;
    }

    /// <summary>Consulta el reporte de auditoría ACH.</summary>
    /// <param name="user">Texto opcional buscado por contenido en el usuario que realizó el cambio, después de recortarlo.</param>
    /// <param name="action">Texto opcional buscado por contenido en la acción auditada, después de recortarlo.</param>
    /// <param name="entity">Texto opcional buscado por contenido en el nombre de entidad auditada, después de recortarlo.</param>
    /// <param name="fromUtc">Instante UTC inicial opcional, inclusivo sobre <c>ChangedAt</c>.</param>
    /// <param name="toUtc">Instante UTC final opcional, inclusivo sobre <c>ChangedAt</c>.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de auditoría ACH")]
    [EndpointDescription("Consulta registros de auditoría. user, action y entity se recortan y buscan por contenido; fromUtc y toUtc delimitan ChangedAt de forma inclusiva. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los resultados se ordenan por ChangedAt descendente.")]
    [HttpGet("audit")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchAuditReportResponseDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de registros de auditoría con paginación efectiva y total de coincidencias.")]
    public async Task<IActionResult> GetAudit(
        [FromQuery] string? user,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var response = await _auditHistoryReportService.GetAuditAsync(
            new AchAuditReportFilter
            {
                User = user,
                Action = action,
                Entity = entity,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    /// <summary>Descarga el reporte de auditoría ACH en PDF.</summary>
    /// <param name="user">Texto opcional buscado por contenido en el usuario que realizó el cambio, después de recortarlo.</param>
    /// <param name="action">Texto opcional buscado por contenido en la acción auditada, después de recortarlo.</param>
    /// <param name="entity">Texto opcional buscado por contenido en el nombre de entidad auditada, después de recortarlo.</param>
    /// <param name="fromUtc">Instante UTC inicial opcional, inclusivo sobre <c>ChangedAt</c>.</param>
    /// <param name="toUtc">Instante UTC final opcional, inclusivo sobre <c>ChangedAt</c>.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de auditoría ACH")]
    [EndpointDescription("Genera un PDF de auditoría con filtros opcionales por usuario, acción, entidad e intervalo UTC. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200. Responde application/pdf con nombre ACH_Audit_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre.")]
    [HttpGet("audit/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Audit_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    public async Task<IActionResult> GetAuditPdf(
        [FromQuery] string? user,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
    {
        var file = await _reportGenerator.GenerateAuditPdfAsync(
            new AchAuditReportFilter
            {
                User = user,
                Action = action,
                Entity = entity,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Consulta el histórico de cambios de estado ACH.</summary>
    /// <param name="fromUtc">Instante UTC inicial opcional, inclusivo sobre la fecha de creación del evento.</param>
    /// <param name="toUtc">Instante UTC final opcional, inclusivo sobre la fecha de creación del evento.</param>
    /// <param name="transactionId">Identificador opcional de transacción ACH.</param>
    /// <param name="toState">Estado destino opcional del evento.</param>
    /// <param name="source">Origen opcional del evento de cambio de estado.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Histórico de cambios de estado ACH")]
    [EndpointDescription("Consulta eventos de cambio de estado ACH filtrables por intervalo UTC, transactionId, estado destino y origen. fromUtc/toUtc se aplican inclusivamente sobre CreatedAt; page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los resultados se ordenan por CreatedAt descendente.")]
    [HttpGet("history")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchHistoryReportResponseDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de eventos de cambio de estado ACH con paginación efectiva y total de coincidencias.")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? transactionId,
        [FromQuery] AchTransferStateEnum? toState,
        [FromQuery] AchStateEventSourceEnum? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var response = await _auditHistoryReportService.GetHistoryAsync(
            new AchHistoryReportFilter
            {
                FromUtc = fromUtc,
                ToUtc = toUtc,
                TransactionId = transactionId,
                ToState = toState,
                Source = source,
                Page = page,
                PageSize = pageSize
            },
            ct);

        return Ok(response);
    }

    /// <summary>Descarga el histórico de cambios de estado ACH en PDF.</summary>
    /// <param name="fromUtc">Instante UTC inicial opcional, inclusivo sobre la fecha de creación del evento.</param>
    /// <param name="toUtc">Instante UTC final opcional, inclusivo sobre la fecha de creación del evento.</param>
    /// <param name="transactionId">Identificador opcional de transacción ACH.</param>
    /// <param name="toState">Estado destino opcional del evento.</param>
    /// <param name="source">Origen opcional del evento de cambio de estado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de histórico ACH")]
    [EndpointDescription("Genera un PDF del histórico de cambios de estado con filtros opcionales por intervalo UTC, transactionId, estado destino y origen. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200. Responde application/pdf con nombre ACH_History_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre.")]
    [HttpGet("history/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_History_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    public async Task<IActionResult> GetHistoryPdf(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? transactionId,
        [FromQuery] AchTransferStateEnum? toState,
        [FromQuery] AchStateEventSourceEnum? source,
        CancellationToken ct = default)
    {
        var file = await _reportGenerator.GenerateHistoryPdfAsync(
            new AchHistoryReportFilter
            {
                FromUtc = fromUtc,
                ToUtc = toUtc,
                TransactionId = transactionId,
                ToState = toState,
                Source = source,
                Page = 1,
                PageSize = 5000
            },
            ct);

        return File(file.Content, file.ContentType, file.FileName);
    }

}
