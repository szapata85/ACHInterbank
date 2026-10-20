using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/third-party-accounts/backfill")]
[Authorize]
public sealed class ThirdPartyAccountBackfillController : ControllerBase
{
    private readonly IThirdPartyAccountBackfillService _service;

    public ThirdPartyAccountBackfillController(IThirdPartyAccountBackfillService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Run(
        [FromBody] ThirdPartyAccountBackfillRequest request,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdPartyAccount.Backfill");
        try
        {
            return Ok(await _service.RunAsync(request, ct));
        }
        catch (ArgumentException)
        {
            return BadRequest(new
            {
                code = "INVALID_BACKFILL_REQUEST",
                message = "La solicitud de backfill es inválida."
            });
        }
    }

    private void SetAuditScope(string action)
    {
        if (ControllerContext.HttpContext is not { } context)
        {
            return;
        }

        context.Items[AchDbContext.AuditActionItemKey] = action;
        context.Items[AchDbContext.AuditCorrelationItemKey] = context.TraceIdentifier;
    }
}
