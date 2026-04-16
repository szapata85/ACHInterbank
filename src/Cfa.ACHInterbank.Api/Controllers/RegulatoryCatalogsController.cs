using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/regulatory-catalogs")]
public class RegulatoryCatalogsController : ControllerBase
{
    private readonly IAchRegulatoryCatalogService _catalogService;

    public RegulatoryCatalogsController(IAchRegulatoryCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("return-codes")]
    public async Task<IActionResult> GetReturnCodes(CancellationToken ct)
        => Ok(await _catalogService.GetReturnCodesAsync(ct));

    [HttpGet("file-rejection-codes")]
    public async Task<IActionResult> GetFileRejectionCodes(CancellationToken ct)
        => Ok(await _catalogService.GetFileRejectionCodesAsync(ct));

    [HttpGet("transaction-type-policies")]
    public async Task<IActionResult> GetTransactionTypePolicies(CancellationToken ct)
        => Ok(await _catalogService.GetTransactionTypePoliciesAsync(ct));

    [HttpGet("return-policies")]
    public async Task<IActionResult> GetReturnPolicies(CancellationToken ct)
        => Ok(await _catalogService.GetReturnPoliciesAsync(ct));

    [HttpGet("return-of-return-policies")]
    public async Task<IActionResult> GetReturnOfReturnPolicies(CancellationToken ct)
        => Ok(await _catalogService.GetReturnOfReturnPoliciesAsync(ct));

    [HttpGet("prenotification-policies")]
    public async Task<IActionResult> GetPrenotificationPolicies(CancellationToken ct)
        => Ok(await _catalogService.GetPrenotificationPoliciesAsync(ct));
}
