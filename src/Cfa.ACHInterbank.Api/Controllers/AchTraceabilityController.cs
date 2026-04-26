using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach-traceability")]
public class AchTraceabilityController : ControllerBase
{
    private readonly IAchTraceabilityService _traceabilityService;

    public AchTraceabilityController(IAchTraceabilityService traceabilityService)
    {
        _traceabilityService = traceabilityService;
    }

    [EndpointSummary("POST sol02/{transactionId:int}/certify: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'sol02/{transactionId:int}/certify'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: modifica información. Genera auditoría: sí, mediante los servicios de operación/auditoría cuando aplica al flujo.")]
    [HttpPost("sol02/{transactionId:int}/certify")]
    public async Task<IActionResult> CertifyWithSol02(
        int transactionId,
        [FromBody] Sol02CertificationRequest request,
        CancellationToken ct)
    {
        try
        {
            var transaction = await _traceabilityService.CertifySol02Async(
                transactionId,
                request.CertificationReference,
                request.Notes,
                ct);

            return Ok(new
            {
                message = "Certificación SOL02 aplicada.",
                transactionId = transaction.Id,
                transaction.State,
                transaction.StateChangedAtUtc
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [EndpointSummary("GET transactions/{transactionId:int}: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'transactions/{transactionId:int}'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("transactions/{transactionId:int}")]
    public async Task<IActionResult> GetTransactionTraceability(int transactionId, CancellationToken ct)
    {
        var traceability = await _traceabilityService.GetTransactionTraceabilityAsync(transactionId, ct);
        if (traceability is null)
        {
            return NotFound(new { message = $"No existe la transacción ACH {transactionId}." });
        }

        return Ok(traceability);
    }

    [EndpointSummary("GET report: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'report'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("report")]
    public async Task<IActionResult> GetTraceabilityReport(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? achCycleId,
        CancellationToken ct)
    {
        var report = await _traceabilityService.GetTraceabilityReportAsync(fromUtc, toUtc, state, achCycleId, ct);
        return Ok(report);
    }
}

public class Sol02CertificationRequest
{
    public string? CertificationReference { get; set; }
    public string? Notes { get; set; }
}
