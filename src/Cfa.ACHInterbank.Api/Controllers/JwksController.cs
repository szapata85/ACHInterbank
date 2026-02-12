using Cfa.ACHInterbank.Application.Exceptions;
using Cfa.ACHInterbank.Application.External.ClientAssertion;
using Cfa.ACHInterbank.Application.External.JwksService;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Services.ClientAssertion.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("/oauth2")]
[TypeFilter(typeof(ExceptionManager))]
[AllowAnonymous]
public class JwksController : ControllerBase
{
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>
    [HttpGet("jwks")]
    public async Task<IActionResult> GetJwks([FromServices] IJwksServiceScoped jwksService)
    {
        var data = jwksService.GetPublicJwks();
        return data.Success ? Ok(data.Result) : StatusCode(StatusCodes.Status500InternalServerError, ResponseApiService.Response(StatusCodes.Status500InternalServerError));
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("TokenClientAssertions")]
    public async Task<IActionResult> TokenClientAssertions([FromServices] IGetTokenWithClientAssertionScoped getToken)
    {
        var data = getToken.GenerateClientAssertion();
        return StatusCode(StatusCodes.Status201Created, ResponseApiService.Response(StatusCodes.Status201Created, data));
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost("client-assertion")]
    public async Task<IActionResult> Authenticate([FromBody] string request, [FromServices] IClientAssertionValidatorScoped getToken)
    {
        if (getToken.ValidateAssertionAsync(request))
        {
            return Ok(new { message = "Authentication successful" });
        }

        return Unauthorized(new { message = "Invalid client assertion" });
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost("Genearte-client-assertion")]
    public async Task<IActionResult> GenerateClientAssertion([FromServices] IGetTokenWithClientAssertionScoped getToken)
    {
        var data = await getToken.GenerateClientAssertion();
        return data.Success ? Ok(data.Result) : BadRequest(data.Errors);
    }

}
