using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/customers/{customerId:int}/third-parties")]
[Authorize]
public sealed class ThirdPartiesController : ControllerBase
{
    private readonly IThirdPartyAdministrationService _administrationService;
    private readonly IThirdPartyAccountReadService _accountReadService;
    private readonly IThirdPartyAccountRevalidationService _revalidationService;

    public ThirdPartiesController(
        IThirdPartyAdministrationService administrationService,
        IThirdPartyAccountReadService accountReadService,
        IThirdPartyAccountRevalidationService revalidationService)
    {
        _administrationService = administrationService;
        _accountReadService = accountReadService;
        _revalidationService = revalidationService;
    }

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetThirdParties(
        int customerId,
        [FromQuery] string? search,
        [FromQuery] ThirdPartyAdministrativeStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _administrationService.GetThirdPartiesAsync(
            customerId,
            new ThirdPartyAdministrationQuery(search, status, page, pageSize),
            ct));

    [HttpGet("{thirdPartyId:long}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetThirdParty(int customerId, long thirdPartyId, CancellationToken ct = default)
    {
        var result = await _administrationService.GetThirdPartyAsync(customerId, thirdPartyId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> CreateThirdParty(
        int customerId,
        [FromBody] CreateThirdPartyCommand command,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdParty.Create");
        try
        {
            var result = await _administrationService.CreateThirdPartyAsync(customerId, command, ct);
            return result.Created
                ? CreatedAtAction(nameof(GetThirdParty), new { customerId, thirdPartyId = result.Value.ThirdPartyId }, result.Value)
                : Ok(result.Value);
        }
        catch (Exception ex) when (ex is ArgumentException or ThirdPartyAdministrationConflictException)
        {
            return CommandError(ex);
        }
    }

    [HttpPut("{thirdPartyId:long}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UpdateThirdParty(
        int customerId,
        long thirdPartyId,
        [FromBody] UpdateThirdPartyCommand command,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdParty.Update");
        try
        {
            var result = await _administrationService.UpdateThirdPartyAsync(customerId, thirdPartyId, command, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (Exception ex) when (ex is ArgumentException or ThirdPartyAdministrationConflictException)
        {
            return CommandError(ex);
        }
    }

    [HttpPut("{thirdPartyId:long}/status")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> SetThirdPartyStatus(
        int customerId,
        long thirdPartyId,
        [FromBody] SetThirdPartyAdministrativeStatusCommand command,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdParty.Status");
        try
        {
            var result = await _administrationService.SetThirdPartyStatusAsync(customerId, thirdPartyId, command, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CommandError(ex);
        }
    }

    [HttpGet("{thirdPartyId:long}/accounts")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetAccounts(
        int customerId,
        long thirdPartyId,
        [FromQuery] ThirdPartyAdministrativeStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var thirdParty = await _administrationService.GetThirdPartyAsync(customerId, thirdPartyId, ct);
        if (thirdParty is null)
        {
            return NotFound();
        }

        return Ok(await _accountReadService.GetAsync(new ThirdPartyAccountReadQuery
        {
            CustomerId = customerId,
            ThirdPartyId = thirdPartyId,
            AccountStatus = status,
            Page = page,
            PageSize = pageSize
        }, ct));
    }

    [HttpPost("{thirdPartyId:long}/accounts")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> CreateAccount(
        int customerId,
        long thirdPartyId,
        [FromBody] CreateThirdPartyAccountCommand command,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdPartyAccount.Create");
        try
        {
            var result = await _administrationService.CreateAccountAsync(customerId, thirdPartyId, command, ct);
            if (result is null)
            {
                return NotFound();
            }

            return result.Created
                ? CreatedAtAction(nameof(GetAccounts), new { customerId, thirdPartyId }, result.Value)
                : Ok(result.Value);
        }
        catch (Exception ex) when (ex is ArgumentException or ThirdPartyAdministrationConflictException)
        {
            return CommandError(ex);
        }
    }

    [HttpPut("{thirdPartyId:long}/accounts/{thirdPartyAccountId:long}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UpdateAccount(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        [FromBody] UpdateThirdPartyAccountCommand command,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdPartyAccount.Update");
        try
        {
            var result = await _administrationService.UpdateAccountAsync(
                customerId,
                thirdPartyId,
                thirdPartyAccountId,
                command,
                ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CommandError(ex);
        }
    }

    [HttpPut("{thirdPartyId:long}/accounts/{thirdPartyAccountId:long}/status")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> SetAccountStatus(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        [FromBody] SetThirdPartyAdministrativeStatusCommand command,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdPartyAccount.Status");
        try
        {
            var result = await _administrationService.SetAccountStatusAsync(
                customerId,
                thirdPartyId,
                thirdPartyAccountId,
                command,
                ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CommandError(ex);
        }
    }

    [HttpGet("{thirdPartyId:long}/accounts/{thirdPartyAccountId:long}/validation-history")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetValidationHistory(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        CancellationToken ct = default)
    {
        var result = await _accountReadService.GetValidationHistoryAsync(
            thirdPartyAccountId,
            customerId,
            thirdPartyId,
            ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{thirdPartyId:long}/accounts/{thirdPartyAccountId:long}/revalidations")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> RequestRevalidation(
        int customerId,
        long thirdPartyId,
        long thirdPartyAccountId,
        [FromBody] RequestThirdPartyAccountRevalidationCommand command,
        CancellationToken ct = default)
    {
        SetAuditScope("ThirdPartyAccount.Revalidation.Request");
        try
        {
            var result = await _revalidationService.RequestAsync(
                customerId,
                thirdPartyId,
                thirdPartyAccountId,
                command,
                ct);
            if (result is null)
            {
                return NotFound();
            }

            return result.ReusedExistingAttempt ? Ok(result) : Accepted(result);
        }
        catch (Exception ex) when (ex is ArgumentException or ThirdPartyAdministrationConflictException)
        {
            return CommandError(ex);
        }
    }

    private IActionResult CommandError(Exception exception)
    {
        if (exception is ThirdPartyAdministrationConflictException conflict)
        {
            var isNotFound = conflict.Code.EndsWith("_NOT_FOUND", StringComparison.Ordinal);
            var body = new
            {
                code = SafeConflictCode(conflict.Code, isNotFound),
                message = isNotFound
                    ? "El recurso solicitado no existe."
                    : "La operación no pudo completarse por un conflicto."
            };
            return isNotFound ? NotFound(body) : Conflict(body);
        }

        return BadRequest(new { code = "INVALID_REQUEST", message = "La solicitud no es válida." });
    }

    private static string SafeConflictCode(string code, bool isNotFound)
    {
        if (isNotFound)
        {
            return "NOT_FOUND";
        }

        return code switch
        {
            "IDENTITY_METADATA_CONFLICT" => code,
            "DUPLICATE_IDENTITY_CONFLICT" => code,
            "THIRD_PARTY_IDENTITY_CONFLICT" => code,
            _ => "CONFLICT"
        };
    }

    private void SetAuditScope(string action)
    {
        if (ControllerContext.HttpContext is not { } context)
        {
            return;
        }

        // Audit correlation is server-owned. Caller-provided headers are intentionally not copied
        // into persisted audit scope because they are untrusted and may contain PII/account data.
        context.Items[AchDbContext.AuditActionItemKey] = action;
        context.Items[AchDbContext.AuditCorrelationItemKey] = context.TraceIdentifier;
    }
}
