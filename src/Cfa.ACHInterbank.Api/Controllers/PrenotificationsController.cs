using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/prenotifications")]
[Authorize]
public sealed class PrenotificationsController : ControllerBase
{
    private readonly IPrenotificationQueryService _queryService;

    public PrenotificationsController(IPrenotificationQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet("by-reference/{reference}")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(PrenotificationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByReference(string reference, CancellationToken ct)
    {
        var item = await _queryService.GetByReferenceAsync(reference, ct);
        return item is null
            ? NotFound(new { message = $"No se encontro la prenotificacion con referencia {reference}." })
            : Ok(item);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(PrenotificationStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _queryService.GetByIdAsync(id, ct);
        return item is null
            ? NotFound(new { message = $"No se encontro la prenotificacion con ID {id}." })
            : Ok(item);
    }
}
