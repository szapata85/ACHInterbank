using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/customer-third-parties")]
[Authorize]
public class CustomerThirdPartiesController : ControllerBase
{
    private readonly ICustomerThirdPartyAppService _service;

    public CustomerThirdPartiesController(ICustomerThirdPartyAppService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] string? destinationAccountNumber,
        [FromQuery] string? recipientIdNumber,
        [FromQuery] int? destinationInstitutionId,
        [FromQuery] string? sourceAccountNumber,
        [FromQuery] CustomerThirdPartyStatusEnum? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.GetAsync(new CustomerThirdPartyQuery
        {
            Search = search,
            DestinationAccountNumber = destinationAccountNumber,
            RecipientIdNumber = recipientIdNumber,
            DestinationInstitutionId = destinationInstitutionId,
            SourceAccountNumber = sourceAccountNumber,
            Status = status,
            Page = page,
            PageSize = pageSize
        }, ct);

        return Ok(result);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateCustomerThirdPartyStatusRequest request, CancellationToken ct = default)
    {
        var updated = await _service.UpdateStatusAsync(id, request.Status, request.ValidationMessage, ct);
        return Ok(updated);
    }
}
