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

    [EndpointSummary("Aplicar certificación SOL02 a transacción")]
    [EndpointDescription("Qué hace: cambia estado de una transacción registrando certificación SOL02. Cuándo se usa: en procesos de trazabilidad y cumplimiento con evidencia externa. Perfil consumidor: operación ACH y analistas de trazabilidad. Permiso requerido: sin policy explícita en el método; aplicar control de acceso por entorno. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: certificar transacción incorrecta altera historial regulatorio. Errores esperados: 404 transacción no encontrada; 400 transición inválida. Relación ACH/CENIT/NACHA-M: integra trazabilidad operativa ACH con evidencia SOL02. Precauciones para desarrollo u operación: confirmar referencia y notas antes de certificar.")]
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

    [EndpointSummary("Consulta trazabilidad de una transacción")]
    [EndpointDescription("Qué hace: devuelve línea de tiempo y estado de la transacción ACH. Cuándo se usa: en auditorías, reclamos y soporte operativo. Perfil consumidor: auditoría y operación ACH. Permiso requerido: sin policy explícita en el método. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas. Riesgos operativos: diagnóstico incompleto si se consulta id equivocado. Errores esperados: 404 transacción inexistente; 401/403 según entorno. Relación ACH/CENIT/NACHA-M: expone trayectoria de la transacción en ACH/CENIT. Precauciones para desarrollo u operación: correlacionar con reportes de ciclo y devoluciones.")]
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

    [EndpointSummary("Reporte de trazabilidad por rango")]
    [EndpointDescription("Qué hace: genera consulta consolidada de trazabilidad por fechas, estado y ciclo. Cuándo se usa: en cierres operativos y seguimiento de incidentes. Perfil consumidor: operación ACH y control interno. Permiso requerido: sin policy explícita en el método. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas. Riesgos operativos: rangos amplios pueden afectar tiempos de respuesta. Errores esperados: 400 parámetros inválidos. Relación ACH/CENIT/NACHA-M: visibilidad transversal del flujo ACH/NACHA-M. Precauciones para desarrollo u operación: usar filtros acotados para análisis eficiente.")]
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
