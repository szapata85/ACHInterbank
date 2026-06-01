using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/nacha/config-profiles")]
[Authorize]
public sealed class NachaConfigProfilesReadOnlyController : ControllerBase
{
    private readonly INachaConfigProfileReadModelService _service;

    public NachaConfigProfilesReadOnlyController(INachaConfigProfileReadModelService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaConfigProfilesDashboardReadModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _service.GetDashboardAsync(cancellationToken));

    [HttpGet]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigProfileReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfiles(CancellationToken cancellationToken)
        => Ok(await _service.GetProfilesAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaConfigProfileDetailReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(int id, CancellationToken cancellationToken)
    {
        var profile = await _service.GetProfileAsync(id, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("by-code/{profileCode}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaConfigProfileDetailReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileByCode(string profileCode, CancellationToken cancellationToken)
    {
        var profile = await _service.GetProfileByCodeAsync(profileCode, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("{id:int}/variants")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigProfileVariantReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVariants(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetVariantsAsync(id, cancellationToken));

    [HttpGet("{id:int}/fields")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigProfileFieldReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFields(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetFieldsAsync(id, cancellationToken));
}
