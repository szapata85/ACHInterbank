using System.Security.Claims;
using Cfa.ACHInterbank.Application.ACH.Claims.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Claims.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ach-claims")]
public sealed class AchClaimsController : ControllerBase
{
    private readonly IClaimCaseService _claimCases;

    public AchClaimsController(IClaimCaseService claimCases)
    {
        _claimCases = claimCases;
    }

    [HttpPost("dev14")]
    [Authorize(Policy = P0Policies.ClaimsCreateDev14)]
    [EndpointSummary("Crear caso Claims DEV14")]
    [ProducesResponseType(typeof(ClaimCaseDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDev14(
        [FromBody] CreateDev14ClaimCaseRequest request,
        CancellationToken ct)
    {
        try
        {
            var created = await _claimCases.CreateDev14Async(request, IdempotencyKey(), ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ClaimCaseConflictException ex)
        {
            return FunctionalProblem(StatusCodes.Status409Conflict, "Conflicto de idempotencia Claims", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return FunctionalProblem(StatusCodes.Status404NotFound, "Entidad ACH no encontrada", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Solicitud DEV14 inválida", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Solicitud DEV14 inválida", ex.Message);
        }
    }

    [HttpGet]
    [Authorize(Policy = P0Policies.ClaimsRead)]
    [EndpointSummary("Consultar casos Claims")]
    [ProducesResponseType(typeof(ClaimCaseQueryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Query([FromQuery] ClaimCaseQuery query, CancellationToken ct)
    {
        try
        {
            return Ok(await _claimCases.QueryAsync(query, ct));
        }
        catch (ArgumentException ex)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Filtros Claims inválidos", ex.Message);
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = P0Policies.ClaimsRead)]
    [EndpointSummary("Consultar detalle de caso Claims")]
    [ProducesResponseType(typeof(ClaimCaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var claimCase = await _claimCases.GetByIdAsync(id, ct);
        return claimCase is null
            ? FunctionalProblem(StatusCodes.Status404NotFound, "Caso Claims no encontrado", $"No existe el caso Claims con id {id}.")
            : Ok(claimCase);
    }

    [HttpGet("by-transaction/{transactionId:int}")]
    [Authorize(Policy = P0Policies.ClaimsRead)]
    [EndpointSummary("Consultar Claims relacionados con una transacción ACH")]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimCaseListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTransaction(int transactionId, CancellationToken ct)
    {
        try
        {
            return Ok(await _claimCases.GetByTransactionAsync(transactionId, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return FunctionalProblem(StatusCodes.Status404NotFound, "Transacción ACH no encontrada", ex.Message);
        }
    }

    [HttpPost("{id:int}/progress")]
    [Authorize(Policy = P0Policies.ClaimsProgress)]
    [EndpointSummary("Registrar avance de caso Claims")]
    [ProducesResponseType(typeof(ClaimCaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> RecordProgress(int id, [FromBody] RecordClaimProgressRequest request, CancellationToken ct)
        => Mutate(
            () => _claimCases.RecordProgressAsync(id, request, Actor(), IdempotencyKey(), ct),
            "No fue posible registrar el avance Claims");

    [HttpPost("{id:int}/resolve")]
    [Authorize(Policy = P0Policies.ClaimsResolve)]
    [EndpointSummary("Resolver y cerrar caso Claims")]
    [ProducesResponseType(typeof(ClaimCaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Resolve(int id, [FromBody] ResolveClaimCaseRequest request, CancellationToken ct)
        => Mutate(
            () => _claimCases.ResolveAsync(id, request, Actor(), IdempotencyKey(), ct),
            "No fue posible resolver el caso Claims");

    [HttpPost("{id:int}/expire")]
    [Authorize(Policy = P0Policies.ClaimsExpire)]
    [EndpointSummary("Cerrar caso Claims por expiración")]
    [ProducesResponseType(typeof(ClaimCaseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> Expire(int id, [FromBody] ExpireClaimCaseRequest request, CancellationToken ct)
        => Mutate(
            () => _claimCases.ExpireAsync(id, request, Actor(), IdempotencyKey(), ct),
            "No fue posible expirar el caso Claims");

    private async Task<IActionResult> Mutate(
        Func<Task<ClaimCaseDetailDto>> action,
        string invalidOperationTitle)
    {
        try
        {
            return Ok(await action());
        }
        catch (ClaimCaseConflictException ex)
        {
            return FunctionalProblem(StatusCodes.Status409Conflict, "Conflicto de idempotencia Claims", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return FunctionalProblem(StatusCodes.Status404NotFound, "Caso Claims no encontrado", ex.Message);
        }
        catch (ArgumentException ex)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, invalidOperationTitle, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, invalidOperationTitle, ex.Message);
        }
    }

    private string Actor()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.Identity?.Name
           ?? $"authenticated-api:{HttpContext.TraceIdentifier}";

    private string? IdempotencyKey()
        => Request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : null;

    private ObjectResult FunctionalProblem(int status, string title, string detail)
        => Problem(statusCode: status, title: title, detail: detail);
}
