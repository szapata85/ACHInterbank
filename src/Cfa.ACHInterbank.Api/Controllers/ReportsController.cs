using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

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
    private readonly IAchNachaCycleReportService _nachaCycleReportService;
    private readonly IAchReconciliationReportService _reconciliationReportService;
    private readonly IAchAuditHistoryReportService _auditHistoryReportService;
    private readonly IClearingHouseService _clearingHouseService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportGenerator reportGenerator,
        IAchTransactionReportService transactionReportService,
        IAchReturnRejectionReportService returnRejectionReportService,
        IAchNachaCycleReportService nachaCycleReportService,
        IAchReconciliationReportService reconciliationReportService,
        IAchAuditHistoryReportService auditHistoryReportService,
        IClearingHouseService clearingHouseService,
        ILogger<ReportsController> logger)
    {
        _reportGenerator = reportGenerator;
        _transactionReportService = transactionReportService;
        _returnRejectionReportService = returnRejectionReportService;
        _nachaCycleReportService = nachaCycleReportService;
        _reconciliationReportService = reconciliationReportService;
        _auditHistoryReportService = auditHistoryReportService;
        _clearingHouseService = clearingHouseService;
        _logger = logger;
    }

    [EndpointSummary("GET transactions/sent: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'transactions/sent'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET transactions/received: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'transactions/received'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET transactions/sent/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'transactions/sent/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET transactions/received/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'transactions/received/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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


    [EndpointSummary("GET returns: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'returns'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET rejections: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'rejections'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET returns/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'returns/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET rejections/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'rejections/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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


    [EndpointSummary("GET nacha-files: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'nacha-files'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("nacha-files")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET nacha-files/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'nacha-files/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("nacha-files/pdf")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET cycles: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'cycles'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("cycles")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET cycles/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'cycles/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("cycles/pdf")]
    [Authorize(Policy = "CanReadAch")]
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


    [EndpointSummary("GET reconciliation: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'reconciliation'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("reconciliation")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET reconciliation/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'reconciliation/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("reconciliation/pdf")]
    [Authorize(Policy = "CanReadAch")]
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


    [EndpointSummary("GET audit: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'audit'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("audit")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET audit/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'audit/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("audit/pdf")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET history: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'history'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("history")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET history/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'history/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("history/pdf")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("GET traceability/pdf: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'traceability/pdf'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    private async Task<IActionResult?> ValidateClearingHouseIdAsync(int? clearingHouseId, CancellationToken ct)
    {
        if (!clearingHouseId.HasValue)
        {
            return null;
        }

        if (clearingHouseId.Value <= 0)
        {
            return BadRequest(new { message = "ClearingHouseId debe ser mayor a cero." });
        }

        var exists = await _clearingHouseService.GetByIdAsync(clearingHouseId.Value, ct);
        if (exists is null)
        {
            return BadRequest(new { message = "La cámara seleccionada no existe." });
        }

        return null;
    }
}
