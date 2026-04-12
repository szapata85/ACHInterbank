using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/integrations/mappingsets")]
[Authorize]
public class IntegrationMappingSetsController : ControllerBase
{
    private readonly IIntegrationMappingSetService _service;

    public IntegrationMappingSetsController(IIntegrationMappingSetService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Get([FromQuery] int? methodId, CancellationToken ct)
        => Ok(await _service.GetByMethodAsync(methodId, ct));

    [HttpGet("published")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetPublished([FromQuery] int methodId, CancellationToken ct)
    {
        var published = await _service.GetPublishedByMethodAsync(methodId, ct);
        return published is null ? NotFound() : Ok(published);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Create([FromBody] CreateIntegrationMappingSetRequest request, CancellationToken ct)
    {
        var created = await _service.CreateDraftAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.UpdateDraftAsync(id, request, ct));

    [HttpPut("{id:guid}/rules")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> UpsertRules(Guid id, [FromBody] UpsertIntegrationMappingRulesRequest request, CancellationToken ct)
        => Ok(await _service.UpsertRulesAsync(id, request, ct));

    [HttpPost("{id:guid}/validate")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Validate(Guid id, [FromBody] ValidateIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.ValidateAsync(id, request, ct));

    [HttpPost("{id:guid}/preview")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Preview(Guid id, [FromBody] PreviewIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.PreviewAsync(id, request, ct));

    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.PublishAsync(id, request, ct));

    [HttpPost("{id:guid}/clone")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Clone(Guid id, [FromBody] CloneIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.CloneAsync(id, request, ct));

    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct)
        => Ok(await _service.GetHistoryAsync(id, ct));
}
