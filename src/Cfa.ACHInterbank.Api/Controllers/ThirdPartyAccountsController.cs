using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/third-party-accounts")]
[Authorize]
public sealed class ThirdPartyAccountsController : ControllerBase
{
    private readonly IThirdPartyAccountReadService _readService;

    public ThirdPartyAccountsController(IThirdPartyAccountReadService readService)
    {
        _readService = readService;
    }

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] int? customerId,
        [FromQuery] long? thirdPartyId,
        [FromQuery] long? thirdPartyAccountId,
        [FromQuery] int? financialInstitutionId,
        [FromQuery] ThirdPartyAdministrativeStatus? thirdPartyStatus,
        [FromQuery] ThirdPartyAdministrativeStatus? accountStatus,
        [FromQuery] int? eligibilityClearingHouseId,
        [FromQuery] TransactionNature? eligibilityTransactionNature,
        [FromQuery] DateTime? eligibilityEffectiveDate,
        [FromQuery] string? eligibilityServiceType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (customerId is null or <= 0)
        {
            return InvalidReadRequest("CUSTOMER_SCOPE_REQUIRED", "La consulta requiere un contexto de cliente válido.");
        }

        if (HasClientEligibilityContext(
                eligibilityClearingHouseId,
                eligibilityTransactionNature,
                eligibilityEffectiveDate,
                eligibilityServiceType))
        {
            return InvalidReadRequest(
                "CLIENT_ELIGIBILITY_CONTEXT_NOT_ALLOWED",
                "La elegibilidad transaccional se resuelve exclusivamente con contexto del backend.");
        }

        var result = await _readService.GetAsync(new ThirdPartyAccountReadQuery
        {
            Search = search,
            CustomerId = customerId,
            ThirdPartyId = thirdPartyId,
            ThirdPartyAccountId = thirdPartyAccountId,
            FinancialInstitutionId = financialInstitutionId,
            ThirdPartyStatus = thirdPartyStatus,
            AccountStatus = accountStatus,
            Page = page,
            PageSize = pageSize,
            EligibilityContext = null
        }, ct);

        return Ok(result);
    }

    [HttpGet("{thirdPartyAccountId:long}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetById(
        long thirdPartyAccountId,
        [FromQuery] int? customerId,
        [FromQuery] int? eligibilityClearingHouseId,
        [FromQuery] TransactionNature? eligibilityTransactionNature,
        [FromQuery] DateTime? eligibilityEffectiveDate,
        [FromQuery] string? eligibilityServiceType,
        CancellationToken ct = default)
    {
        if (thirdPartyAccountId <= 0)
        {
            return NotFound();
        }

        if (customerId is null or <= 0)
        {
            return InvalidReadRequest("CUSTOMER_SCOPE_REQUIRED", "La consulta requiere un contexto de cliente válido.");
        }

        if (HasClientEligibilityContext(
                eligibilityClearingHouseId,
                eligibilityTransactionNature,
                eligibilityEffectiveDate,
                eligibilityServiceType))
        {
            return InvalidReadRequest(
                "CLIENT_ELIGIBILITY_CONTEXT_NOT_ALLOWED",
                "La elegibilidad transaccional se resuelve exclusivamente con contexto del backend.");
        }

        var scoped = await _readService.GetAsync(new ThirdPartyAccountReadQuery
        {
            CustomerId = customerId,
            ThirdPartyAccountId = thirdPartyAccountId,
            Page = 1,
            PageSize = 1,
            EligibilityContext = null
        }, ct);

        var result = scoped.Items.SingleOrDefault();
        return result is null ? NotFound() : Ok(result);
    }

    private BadRequestObjectResult InvalidReadRequest(string code, string message)
        => BadRequest(new { Code = code, Message = message });

    private static bool HasClientEligibilityContext(
        int? clearingHouseId,
        TransactionNature? transactionNature,
        DateTime? effectiveDate,
        string? serviceType)
        => clearingHouseId.HasValue
           || transactionNature.HasValue
           || effectiveDate.HasValue
           || !string.IsNullOrWhiteSpace(serviceType);
}
