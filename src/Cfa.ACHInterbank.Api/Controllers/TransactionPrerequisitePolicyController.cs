using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("preview")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Preview([FromBody] TransactionPrerequisitePreviewRequest request, CancellationToken ct = default)
        => Ok(await _service.PreviewAsync(request, ct));
}
