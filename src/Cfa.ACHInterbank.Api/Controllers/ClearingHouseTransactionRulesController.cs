using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/clearing-house-transaction-rules")]
[Authorize]
public class ClearingHouseTransactionRulesController : ControllerBase
{
    private readonly IClearingHouseTransactionRuleService _service;

    public ClearingHouseTransactionRulesController(IClearingHouseTransactionRuleService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<IActionResult> Get(
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? transactionNature,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
        => Ok(await _service.GetAsync(clearingHouseId, transactionNature, includeInactive, ct));

    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var rule = await _service.GetByIdAsync(id, ct);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> Create([FromBody] CreateClearingHouseTransactionRuleRequest request, CancellationToken ct = default)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClearingHouseTransactionRuleRequest request, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/activate")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> Activate(int id, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.SetActiveAsync(id, true, ct));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.SetActiveAsync(id, false, ct));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }
}
