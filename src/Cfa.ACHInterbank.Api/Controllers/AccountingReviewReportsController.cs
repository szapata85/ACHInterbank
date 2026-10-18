using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/reports")]
[ControllerContractName("Reports")]
[Authorize]
public sealed class AccountingReviewReportsController : ControllerBase
{
    private readonly IAccountingReviewExportAppService _accountingReviewExportAppService;

    public AccountingReviewReportsController(IAccountingReviewExportAppService accountingReviewExportAppService)
    {
        _accountingReviewExportAppService = accountingReviewExportAppService;
    }

    /// <summary>Exporta la revisión contable operativa para terceros.</summary>
    /// <param name="request">Filtros, inclusiones y formato de exportación. Los formatos aceptados son pdf, csv, excel y xlsx.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Exportar revisión contable operativa")]
    [EndpointDescription("Genera en memoria una exportación de revisión operativa contra terceros; no contabiliza ni crea asientos. El body es AccountingReviewExportApiRequest. Format se recorta y compara sin distinguir mayúsculas: pdf produce application/pdf, csv produce text/csv y excel/xlsx producen application/vnd.openxmlformats-officedocument.spreadsheetml.sheet. El nombre de descarga es accounting-review-reconciliation-<timestamp UTC yyyyMMddHHmmss> con extensión .pdf, .csv o .xlsx. Body nulo, formato no soportado, ArgumentException del servicio o contenido generado vacío producen 400 con body anónimo { message }.")]
    [HttpPost("accounting-review/export")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf", "text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo generado por File(...); Content-Type depende de Format y Content-Disposition usa accounting-review-reconciliation-<timestamp UTC yyyyMMddHHmmss> con extensión .pdf, .csv o .xlsx.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El body es nulo, el formato no es pdf/csv/excel/xlsx, el servicio produjo ArgumentException o no generó contenido; el body anónimo contiene message.")]
    public async Task<IActionResult> ExportAccountingReview([FromBody] AccountingReviewExportApiRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequest(new { message = "El cuerpo de la solicitud es requerido." });
        }

        try
        {
            var result = await _accountingReviewExportAppService.ExportAsync(request, ct);
            if (result.Content is null || result.Content.Length == 0)
            {
                return BadRequest(new { message = "No fue posible generar contenido exportable para el alcance solicitado." });
            }

            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
