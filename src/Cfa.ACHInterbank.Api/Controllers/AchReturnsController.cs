using Cfa.ACHInterbank.Api.OpenApi;
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
    /// <summary>
    /// Consulta las transacciones persistidas de un ciclo que se presentan al flujo de generación de devoluciones.
    /// </summary>
    /// <param name="cycleId">Identificador del ciclo operativo cuyas transacciones se desean consultar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar transacciones de un ciclo para devolución")]
    [EndpointDescription("Devuelve las transacciones persistidas del ciclo indicado ordenadas por identificador. Para cada transacción, IsEligible se marca en false cuando ya existe una devolución generada y ValidationMessage informa ese motivo; esta consulta no genera archivos ni modifica la transacción.")]
    [HttpGet("cycles/{cycleId}/transactions")]
    [Authorize(Policy = P0Policies.ReturnsRead)]
    [ProducesResponseType(typeof(IEnumerable<ReturnEligibleTransactionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Transacciones del ciclo con el indicador de elegibilidad usado por la consulta de devoluciones.")]
    public async Task<IActionResult> GetTransactionsByCycle(string cycleId, CancellationToken ct)
    {
        var items = await service.GetTransactionsByCycleAsync(cycleId, ct);
        return Ok(items);
    }

    /// <summary>
    /// Genera el archivo de devoluciones correspondiente a las transacciones seleccionadas.
    /// </summary>
    /// <param name="request">Ciclo original, ciclo de devolución opcional y selección de transacciones con su causal.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Generar archivo de devoluciones")]
    [EndpointDescription("Genera el archivo de devolución para los ítems seleccionados. ReturnCycleId es opcional y, cuando no se informa, el servicio utiliza CycleId. El servicio valida las transacciones y las reglas aplicables antes de persistir las devoluciones generadas y mover las transacciones procesadas al estado ReturnedByEpr. La respuesta exitosa es el archivo descargable. Si una transacción seleccionada ya tiene una devolución generada, el controller responde 409 con ProblemDetails, errorCode y los transactionIds en conflicto.")]
    [HttpPost("generate-file")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo de devoluciones generado y devuelto como descarga.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "Una o más transacciones seleccionadas ya tienen una devolución generada.")]
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
