using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/password-rules")]
[Authorize]
public class PasswordRulesController : ControllerBase
{
    private readonly IPasswordRulesService _service;

    public PasswordRulesController(IPasswordRulesService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<PasswordRulesDto>> GetRulesAsync(CancellationToken cancellationToken)
    {
        var rules = await _service.GetAsync(cancellationToken);
        return Ok(rules);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut]
    public async Task<ActionResult<PasswordRulesDto>> SaveRulesAsync(
        [FromBody] PasswordRulesDto request,
        CancellationToken cancellationToken)
    {
        var rules = await _service.SaveAsync(request, cancellationToken);
        return Ok(rules);
    }
}
