using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-returns")]
public class AchReturnsController(IAchReturnsService service) : ControllerBase
{
    [EndpointSummary("Transacciones elegibles para devoluciones por ciclo")]
    [EndpointDescription("Qué hace: lista transacciones del ciclo que cumplen criterios para devolución ACH. Cuándo se usa: al preparar archivo de devoluciones. Perfil consumidor: operación ACH de devoluciones. Permiso requerido: sin policy explícita en el método; sujeto a controles de despliegue. Tipo de operación: solo consulta. Genera auditoría: no directa. Riesgos operativos: seleccionar ciclo errado genera devoluciones inválidas. Errores esperados: 400 parámetros inválidos; 404 ciclo inexistente. Relación ACH/CENIT/NACHA-M: gestiona devoluciones bajo reglas ACH/CENIT. Precauciones para desarrollo u operación: validar ciclo y causal antes de generar archivo.")]
    [HttpGet("cycles/{cycleId}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<ReturnEligibleTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsByCycle(string cycleId, CancellationToken ct)
    {
        var items = await service.GetTransactionsByCycleAsync(cycleId, ct);
        return Ok(items);
    }

    [EndpointSummary("Generación de archivo de devoluciones")]
    [EndpointDescription("Qué hace: construye y retorna el archivo de devoluciones con contenido descargable. Cuándo se usa: cuando ya se aprobaron causales y transacciones. Perfil consumidor: operación ACH. Permiso requerido: sin policy explícita en el método; revisar seguridad del entorno. Tipo de operación: modifica información. Genera auditoría: sí, por rastro de generación/descarga. Riesgos operativos: una solicitud mal parametrizada puede emitir archivo incorrecto. Errores esperados: 400 validación de solicitud; 409 por estado no permitido. Relación ACH/CENIT/NACHA-M: materializa devolución ACH conforme a operación CENIT. Precauciones para desarrollo u operación: aplicar doble validación operativa antes de distribuir el archivo.")]
    [HttpPost("generate-file")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateFile([FromBody] GenerateReturnsFileRequest request, CancellationToken ct)
    {
        var response = await service.GenerateReturnsFileAsync(request, ct);
        return File(response.Content, response.ContentType, response.FileName);
    }
}
