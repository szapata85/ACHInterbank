using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/responses")]
[Authorize]
public class AchResponsesController : ControllerBase
{
    [HttpPost("process")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
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
    [Authorize(Policy = P1Policies.NachaGenerate)]
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
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> Search([FromQuery] AchResponseSearchRequest request, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseRepository repository, CancellationToken ct)
    {
        var result = await repository.SearchAsync(mapper.MapSearchRequest(request), ct);
        return Ok(mapper.MapPagedResult(result));
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] AchResponseDashboardRequest request,
        [FromServices] AchResponseQueryApiMapper mapper,
        [FromServices] IAchResponseRepository repository,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.TipoRespuesta) && mapper.ParseTipoRespuestaOrNull(request.TipoRespuesta) is null)
            return BadRequest(new ProblemDetails { Title = "Tipo de respuesta inválido", Detail = "Valores permitidos: Prenota, Transaccion" });

        var dashboard = await repository.GetDashboardAsync(mapper.MapDashboardRequest(request), ct);
        return Ok(mapper.MapDashboard(dashboard));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> GetDetail(Guid id, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseRepository repository, CancellationToken ct)
    {
        var detail = await repository.FindDetailByIdAsync(id, ct);
        if (detail is null) return NotFound();
        return Ok(mapper.MapDetail(detail));
    }

    [HttpGet("{id:guid}/notification-attempts")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> GetAttempts(Guid id, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseNotificationAttemptRepository repository, CancellationToken ct)
    {
        var attempts = await repository.FindPublicByResponseIdAsync(id, ct);
        return Ok(attempts.Select(mapper.MapAttempt).ToList());
    }

    [HttpGet("/api/ach/response-status-mappings")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> GetMappings([FromQuery] string? codigoCamaraCompensacion, [FromQuery] string? tipoRespuesta, [FromQuery] bool? activo, [FromServices] AchResponseQueryApiMapper mapper, [FromServices] IAchResponseStatusMappingRepository repository, CancellationToken ct)
    {
        var tipo = mapper.ParseTipoRespuestaOrNull(tipoRespuesta);
        if (!string.IsNullOrWhiteSpace(tipoRespuesta) && tipo is null)
            return BadRequest(new ProblemDetails { Title = "Tipo de respuesta inválido", Detail = "Valores permitidos: Prenota, Transaccion" });

        var items = await repository.ListAsync(codigoCamaraCompensacion, tipo, activo, ct);
        return Ok(items.Select(mapper.MapStatusMapping).ToList());
    }
}
