using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/transaction-prerequisite-policy")]
[Authorize]
public class TransactionPrerequisitePolicyController : ControllerBase
{
    private readonly ITransactionPrerequisitePolicyService _service;

    public TransactionPrerequisitePolicyController(ITransactionPrerequisitePolicyService service)
    {
        _service = service;
    }

    /// <summary>Previsualiza la política de prerequisitos transaccionales.</summary>
    /// <param name="request">Cámara, tipo de transacción, fecha efectiva y alcance de exportación usados para resolver la política vigente.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Previsualizar prerequisitos transaccionales")]
    [EndpointDescription("Evalúa la política de prerequisitos con los datos suministrados sin persistir cambios. Los tipos que no resuelven a Credit o Debit devuelven 200 con Decision NOT_APPLICABLE; cuando no existe una regla vigente coincidente se devuelve 200 con Decision RULE_NOT_CONFIGURED. Si existe una regla aplicable, el resultado refleja su modo de prenotificación y la configuración de validación de identificación del receptor.")]
    [HttpPost("preview")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(TransactionPrerequisitePreviewResponse), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de la evaluación previa de prerequisitos para los datos suministrados.")]
    public async Task<IActionResult> Preview([FromBody] TransactionPrerequisitePreviewRequest request, CancellationToken ct = default)
        => Ok(await _service.PreviewAsync(request, ct));
}
