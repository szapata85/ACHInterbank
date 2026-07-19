using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("navigation")]
[Route("api/navigation")]
[Authorize]
public class NavigationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NavigationController(IMediator mediator)
    {
        _mediator = mediator;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("menu")]
    public async Task<ActionResult<IList<MenuItemDto>>> GetMenuAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMenuForCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }
}
