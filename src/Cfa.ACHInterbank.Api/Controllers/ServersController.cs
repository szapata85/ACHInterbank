using System.Net.Http;
using Cfa.ACHInterbank.Application.Exceptions;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("/[controller]")]
[TypeFilter(typeof(ExceptionManager))]
[AllowAnonymous]
public class ServersController : ControllerBase
{
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<IActionResult> HandleRequest([FromServices] ILoadBalancerSingleton loadBalancer, [FromServices] HttpClient httpClient)
    {
        //var server = loadBalancer.GetNextServer("services");
        var end = await loadBalancer.GetNextServer("servicesWCF");
        var response = await httpClient.GetAsync(end.Url);

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, response.Content.Headers.ContentType?.ToString()!);
    }
}
