using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("return-reasons")]
[Authorize]
public class ReturnReasonsController : ControllerBase
{
    private readonly IReturnReasonService _service;

    public ReturnReasonsController(IReturnReasonService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(ct));
}
