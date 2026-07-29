using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/clearing-houses/{clearingHouseId:int}/transaction-policies")]
[Authorize]
public sealed class ClearingHouseTransactionPoliciesController : ControllerBase
{
    private readonly IClearingHouseTransactionRuleService _rules;
    private readonly ITransactionPrerequisitePolicyService _policy;

    public ClearingHouseTransactionPoliciesController(
        IClearingHouseTransactionRuleService rules,
        ITransactionPrerequisitePolicyService policy)
    {
        _rules = rules;
        _policy = policy;
    }

    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<IActionResult> GetVersions(
        int clearingHouseId,
        [FromQuery] TransactionTypeEnum? transactionType,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.GetVersionsAsync(clearingHouseId, transactionType, ct)));

    [HttpGet("current")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<IActionResult> GetCurrent(
        int clearingHouseId,
        [FromQuery] TransactionTypeEnum transactionType,
        [FromQuery] DateTime? effectiveAt,
        CancellationToken ct)
        => await ExecuteAsync(async () =>
        {
            var policy = await _rules.GetCurrentAsync(
                clearingHouseId,
                transactionType,
                effectiveAt?.Date ?? DateTime.UtcNow.Date,
                ct);
            return policy is null ? NotFound() : Ok(policy);
        });

    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<IActionResult> GetById(int clearingHouseId, int id, CancellationToken ct)
        => await ExecuteAsync(async () =>
        {
            var policy = await _rules.GetByIdAsync(clearingHouseId, id, ct);
            return policy is null ? NotFound() : Ok(policy);
        });

    [HttpPost]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> CreateVersion(
        int clearingHouseId,
        [FromBody] CreateClearingHouseTransactionPolicyVersionRequest request,
        CancellationToken ct)
        => await ExecuteAsync(async () =>
        {
            var created = await _rules.CreateVersionAsync(clearingHouseId, request, ct);
            return CreatedAtAction(nameof(GetById), new { clearingHouseId, id = created.Id }, created);
        });

    [HttpPatch("{id:int}/metadata")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> UpdateMetadata(
        int clearingHouseId,
        int id,
        [FromBody] UpdateClearingHouseTransactionPolicyMetadataRequest request,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.UpdateMetadataAsync(clearingHouseId, id, request, ct)));

    [HttpPost("{id:int}/close")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> CloseVersion(
        int clearingHouseId,
        int id,
        [FromBody] CloseClearingHouseTransactionPolicyVersionRequest request,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.CloseVersionAsync(clearingHouseId, id, request, ct)));

    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> ActivateVersion(int clearingHouseId, int id, CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.ActivateVersionAsync(clearingHouseId, id, ct)));

    [HttpPost("preview")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<IActionResult> Preview(
        int clearingHouseId,
        [FromBody] TransactionPrerequisitePreviewRequest request,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _policy.PreviewAsync(request with { ClearingHouseId = clearingHouseId }, ct)));

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new
            {
                code = "CLEARING_HOUSE_TRANSACTION_POLICY_INVALID",
                message = ex.Message
            });
        }
    }
}
