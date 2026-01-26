using Cfa.ACHInterbank.Application.Exceptions;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Services.TokenClient.Interfaces;
using Cfa.ACHInterbank.Application.Services.TokenClient.Model;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
[TypeFilter(typeof(ExceptionManager))]
[AllowAnonymous]
public class OauthsController : ControllerBase
{
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("GenerateToken")]
    public async Task<IActionResult> GenerateToken([FromBody] TokenModelClient model, [FromServices] IGenerateToken generateToken, [FromServices] IValidator<TokenModelClient> validator)
    {
        var validate = await validator.ValidateAsync(model);

        if (!validate.IsValid)
            return StatusCode(StatusCodes.Status400BadRequest, ResponseApiService.Response(StatusCodes.Status400BadRequest, validate.Errors));

        var data = await generateToken.GenerateTokenAsync(model);

        return StatusCode(StatusCodes.Status201Created, ResponseApiService.Response(StatusCodes.Status201Created, data));
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("GenerateTokenAsync")]
    public async Task<IActionResult> GenerateTokenAsync([FromBody] string Assertion, [FromServices] IGenerateToken generateToken, [FromServices] IValidator<TokenModelClient> validator)
    {
        //var validate = await validator.ValidateAsync(model);

        //if (!validate.IsValid)
        //    return StatusCode(StatusCodes.Status400BadRequest, ResponseApiService.Response(StatusCodes.Status400BadRequest, validate.Errors));

        var data = await generateToken.GenerateTokenAsync(Assertion);

        return StatusCode(StatusCodes.Status201Created, ResponseApiService.Response(StatusCodes.Status201Created, data));
    }
}

