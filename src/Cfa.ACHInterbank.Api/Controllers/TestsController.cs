using Cfa.ACHInterbank.Application.Helpers.Filters;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Services.Transaction.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[Authorize]
[ApiController]
[Route("/[controller]")]
public class TestsController : ControllerBase
{
    private readonly ILoggerManagerTransient _logger;


    public TestsController(ILoggerManagerTransient logger)
    {
         
    }

    [HttpGet]
    //[PostFilter]
    //[SwaggerRequestExample]
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>
    public IActionResult Get(string data, [FromServices] ITestTransient test, [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        int i = 1;
        var request = httpContextAccessor.HttpContext!.Request;
        //_logger.LogInfo($"Inicio Trama método Get con la data: {data}");
        try
        {
            test.Get(request, "Esto es un mensaje de prueba para codificar en base 64");
            var output = 20 / i;
            //_logger.LogInfo($"Resultado operación: {20} / {i} = {output}");   
        }
        catch (Exception ex)
        {
            //_logger.LogError($"Esto es un error de excepción: {ex.Message}");
        }

        return NoContent();
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("Prueba")]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok();
    }
}
