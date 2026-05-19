using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/responses")]
[Authorize]
public class AchResponsesController : ControllerBase
{
    [HttpPost("process")]
    public async Task<IActionResult> Process(
        [FromBody] ProcesarRespuestaAchRequest request,
        [FromServices] ProcesarRespuestaAchRequestValidator validator,
        [FromServices] ProcesarRespuestaAchApiMapper mapper,
        [FromServices] IProcesarRespuestaAchUseCase useCase,
        CancellationToken ct)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(errors.GroupBy(x => x).ToDictionary(g => g.Key, g => new[] { g.Key })));

        var result = await useCase.ExecuteAsync(mapper.MapRequest(request), ct);
        var response = mapper.MapResponse(result);
        return result.Procesada ? Ok(response) : UnprocessableEntity(response);
    }

    [HttpPost("notifications/send")]
    public async Task<IActionResult> SendNotification(
        [FromBody] NotificarRespuestaAchRequest request,
        [FromServices] NotificarRespuestaAchRequestValidator validator,
        [FromServices] NotificarRespuestaAchApiMapper mapper,
        [FromServices] INotificarRespuestaAchUseCase useCase,
        CancellationToken ct)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(errors.GroupBy(x => x).ToDictionary(g => g.Key, g => new[] { g.Key })));

        var result = await useCase.ExecuteAsync(mapper.MapRequest(request), ct);
        if (!result.Encontrada) return NotFound();
        return Ok(mapper.MapResponse(result));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] AchResponseSearchRequest request, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseRepository repository, CancellationToken ct)
    {
        var result = await repository.SearchAsync(mapper.MapSearchRequest(request), ct);
        return Ok(mapper.MapPagedResult(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseRepository repository, CancellationToken ct)
    {
        var detail = await repository.FindDetailByIdAsync(id, ct);
        if (detail is null) return NotFound();
        return Ok(mapper.MapDetail(detail));
    }

    [HttpGet("{id:guid}/notification-attempts")]
    public async Task<IActionResult> GetAttempts(Guid id, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseNotificationAttemptRepository repository, CancellationToken ct)
    {
        var attempts = await repository.FindPublicByResponseIdAsync(id, ct);
        return Ok(attempts.Select(mapper.MapAttempt).ToList());
    }

    [HttpGet("/api/ach/response-status-mappings")]
    public async Task<IActionResult> GetMappings([FromQuery] string? codigoCamaraCompensacion, [FromQuery] string? tipoRespuesta, [FromQuery] bool? activo, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseStatusMappingRepository repository, CancellationToken ct)
    {
        var tipo = mapper.ParseTipoRespuestaOrNull(tipoRespuesta);
        if (!string.IsNullOrWhiteSpace(tipoRespuesta) && tipo is null)
            return BadRequest(new ProblemDetails { Title = "TipoRespuesta inválido", Detail = "Valores permitidos: Prenota, Transaccion" });

        var items = await repository.ListAsync(codigoCamaraCompensacion, tipo, activo, ct);
        return Ok(items.Select(mapper.MapStatusMapping).ToList());
    }
}
