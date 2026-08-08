using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-returns")]
[Authorize]
public class AchReturnsController(IAchReturnsService service) : ControllerBase
{
    [EndpointSummary("Transacciones elegibles para devoluciones por ciclo")]
    [EndpointDescription("Qué hace: lista transacciones del ciclo que cumplen criterios para devolución ACH. Cuándo se usa: al preparar archivo de devoluciones. Perfil consumidor: operación ACH de devoluciones. Permiso requerido: CanReadAch con autorización explícita en la acción y autenticación obligatoria en el controller. Tipo de operación: solo consulta. Genera auditoría: no directa. Riesgos operativos: seleccionar ciclo errado genera devoluciones inválidas. Errores esperados: 400 parámetros inválidos; 404 ciclo inexistente. Relación ACH/CENIT/NACHA-M: gestiona devoluciones bajo reglas ACH/CENIT. Precauciones para desarrollo u operación: validar ciclo y causal antes de generar archivo.")]
    [HttpGet("cycles/{cycleId}/transactions")]
    [Authorize(Policy = P0Policies.ReturnsRead)]
    [ProducesResponseType(typeof(IEnumerable<ReturnEligibleTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsByCycle(string cycleId, CancellationToken ct)
    {
        var items = await service.GetTransactionsByCycleAsync(cycleId, ct);
        return Ok(items);
    }

    [EndpointSummary("Generación de archivo de devoluciones")]
    [EndpointDescription("Qué hace: construye y retorna el archivo de devoluciones con contenido descargable. Cuándo se usa: cuando ya se aprobaron causales y transacciones. Perfil consumidor: operación ACH. Permiso requerido: CanManageAch con autorización explícita en la acción y autenticación obligatoria en el controller. Tipo de operación: modifica información. Genera auditoría: sí, por rastro de generación/descarga. Riesgos operativos: una solicitud mal parametrizada puede emitir archivo incorrecto. Errores esperados: 400 validación de solicitud; 409 por estado no permitido. Relación ACH/CENIT/NACHA-M: materializa devolución ACH conforme a operación CENIT. Precauciones para desarrollo u operación: aplicar doble validación operativa antes de distribuir el archivo.")]
    [HttpPost("generate-file")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateFile([FromBody] GenerateReturnsFileRequest request, CancellationToken ct)
    {
        try
        {
            var response = await service.GenerateReturnsFileAsync(request, ct);
            return File(response.Content, response.ContentType, response.FileName);
        }
        catch (AchReturnAlreadyGeneratedException ex)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "La devolución ya fue generada",
                Detail = ex.Message
            };
            problem.Extensions["errorCode"] = AchReturnAlreadyGeneratedException.ErrorCode;
            problem.Extensions["transactionIds"] = ex.TransactionIds;
            return Conflict(problem);
        }
    }
}
