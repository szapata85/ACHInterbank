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
public sealed class TransactionReportsController : ReportsClearingHouseControllerBase
{
    private readonly IReportGenerator _reportGenerator;
    private readonly IAchTransactionReportService _transactionReportService;

    public TransactionReportsController(
        IReportGenerator reportGenerator,
        IAchTransactionReportService transactionReportService,
        IClearingHouseService clearingHouseService)
        : base(clearingHouseService)
    {
        _reportGenerator = reportGenerator;
        _transactionReportService = transactionReportService;
    }

    /// <summary>Reporte de transacciones enviadas.</summary>
    /// <param name="date">Fecha opcional aplicada sobre EffectiveEntryDate.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH; se recorta antes de compararlo por igualdad.</param>
    /// <param name="state">Estado ACH opcional por el que se filtran las transacciones.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="bankId">Identificador opcional de institución origen (SourceInstitutionId).</param>
    /// <param name="transactionType">Tipo de transacción opcional.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de transacciones enviadas")]
    [EndpointDescription("Consulta transacciones enviadas aplicando los filtros opcionales publicados. El servicio excluye las transacciones en estado ReturnedByOperator o ReturnedByEpr y aquellas con ReturnReasonCode; bankId filtra SourceInstitutionId. date compara la fecha de EffectiveEntryDate, achCycleId se recorta y compara por igualdad, reference busca por contenido en Reference o TransactionExternalId y page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero, con pageSize máximo de 200. clearingHouseId, cuando se informa, debe ser mayor que cero y corresponder a una cámara existente; en caso contrario la acción responde 400 con un body anónimo que contiene message.")]
    [HttpGet("transactions/sent")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchTransactionReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reporte paginado de transacciones enviadas con items, totales y paginación efectiva.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
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
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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

    /// <summary>Reporte de transacciones recibidas.</summary>
    /// <param name="date">Fecha opcional aplicada sobre EffectiveEntryDate.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH; se recorta antes de compararlo por igualdad.</param>
    /// <param name="state">Estado ACH opcional por el que se filtran las transacciones.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="bankId">Identificador opcional de institución destino (DestinationInstitutionId).</param>
    /// <param name="transactionType">Tipo de transacción opcional.</param>
    /// <param name="page">Número de página; valores menores o iguales a cero se normalizan a 1.</param>
    /// <param name="pageSize">Tamaño de página; valores menores o iguales a cero se normalizan a 25 y valores superiores a 200 se limitan a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reporte de transacciones recibidas")]
    [EndpointDescription("Consulta transacciones recibidas aplicando los filtros opcionales publicados. bankId filtra DestinationInstitutionId; date compara la fecha de EffectiveEntryDate, achCycleId se recorta y compara por igualdad, reference busca por contenido en Reference o TransactionExternalId y page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero, con pageSize máximo de 200. clearingHouseId, cuando se informa, debe ser mayor que cero y corresponder a una cámara existente; en caso contrario la acción responde 400 con un body anónimo que contiene message.")]
    [HttpGet("transactions/received")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchTransactionReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reporte paginado de transacciones recibidas con items, totales y paginación efectiva.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
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
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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

    /// <summary>Descargar PDF de transacciones enviadas.</summary>
    /// <param name="date">Fecha opcional aplicada sobre EffectiveEntryDate.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH; se recorta antes de compararlo por igualdad.</param>
    /// <param name="state">Estado ACH opcional por el que se filtran las transacciones.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="bankId">Identificador opcional de institución origen (SourceInstitutionId).</param>
    /// <param name="transactionType">Tipo de transacción opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de transacciones enviadas")]
    [EndpointDescription("Genera un PDF de transacciones enviadas con los mismos filtros de la consulta JSON. El generador solicita la primera página con PageSize=5000, pero el servicio normaliza pageSize a un máximo de 200; los totales se calculan sobre todo el conjunto filtrado. La respuesta exitosa usa application/pdf y el nombre ACH_Sent_<timestamp UTC yyyyMMdd_HHmmss>.pdf; File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("transactions/sent/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Sent_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
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
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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

    /// <summary>Descargar PDF de transacciones recibidas.</summary>
    /// <param name="date">Fecha opcional aplicada sobre EffectiveEntryDate.Date.</param>
    /// <param name="clearingHouseId">Identificador opcional de cámara; si se informa debe ser mayor que cero y existir.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH; se recorta antes de compararlo por igualdad.</param>
    /// <param name="state">Estado ACH opcional por el que se filtran las transacciones.</param>
    /// <param name="reference">Texto opcional buscado por contenido en Reference o TransactionExternalId.</param>
    /// <param name="bankId">Identificador opcional de institución destino (DestinationInstitutionId).</param>
    /// <param name="transactionType">Tipo de transacción opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar PDF de transacciones recibidas")]
    [EndpointDescription("Genera un PDF de transacciones recibidas con los mismos filtros de la consulta JSON. El generador solicita la primera página con PageSize=5000, pero el servicio normaliza pageSize a un máximo de 200; los totales se calculan sobre todo el conjunto filtrado. La respuesta exitosa usa application/pdf y el nombre ACH_Received_<timestamp UTC yyyyMMdd_HHmmss>.pdf; File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.")]
    [HttpGet("transactions/received/pdf")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Received_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.")]
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
        var clearingHouseValidation = await ValidateClearingHouseIdAsync(clearingHouseId, ct);
        if (clearingHouseValidation is not null) return clearingHouseValidation;

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
}
