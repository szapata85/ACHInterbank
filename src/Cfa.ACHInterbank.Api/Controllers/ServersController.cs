using System.Net.Http;
using Cfa.ACHInterbank.Api.Exceptions;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.External.Connections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("/[controller]")]
[TypeFilter(typeof(ExceptionManager))]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)] // Scalar-2D: ruta interna, fuera de OpenAPI por gobierno API
public class ServersController : ControllerBase
{
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    public async Task<IActionResult> HandleRequest([FromServices] ILoadBalancerSingleton loadBalancer, [FromServices] IHttpClientFactory httpClientFactory)
    {
        //var server = loadBalancer.GetNextServer("services");
        var end = await loadBalancer.GetNextServer("servicesWCF");
        using var httpClient = httpClientFactory.CreateClient(ExternalHttpClientNames.HealthProbe);
        using var response = await httpClient.GetAsync(end.Url, HttpContext.RequestAborted);

        var content = await response.Content.ReadAsStringAsync(HttpContext.RequestAborted);
        return Content(content, response.Content.Headers.ContentType?.ToString()!);
    }
}
