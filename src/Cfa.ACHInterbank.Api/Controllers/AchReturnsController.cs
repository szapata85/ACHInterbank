using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-returns")]
public class AchReturnsController(IAchReturnsService service) : ControllerBase
{
    [EndpointSummary("GET cycles/{cycleId}/transactions: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'cycles/{cycleId}/transactions'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("cycles/{cycleId}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<ReturnEligibleTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsByCycle(string cycleId, CancellationToken ct)
    {
        var items = await service.GetTransactionsByCycleAsync(cycleId, ct);
        return Ok(items);
    }

    [EndpointSummary("POST generate-file: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'generate-file'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: modifica información. Genera auditoría: sí, mediante los servicios de operación/auditoría cuando aplica al flujo.")]
    [HttpPost("generate-file")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateFile([FromBody] GenerateReturnsFileRequest request, CancellationToken ct)
    {
        var response = await service.GenerateReturnsFileAsync(request, ct);
        return File(response.Content, response.ContentType, response.FileName);
    }
}
