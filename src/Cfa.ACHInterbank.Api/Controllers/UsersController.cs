using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUsersService _service;

    public UsersController(IUsersService service)
    {
        _service = service;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserSummaryDto>>> GetUsersAsync(
        [FromQuery] string? search,
        [FromQuery] Guid? roleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await _service.GetUsersAsync(new UserQueryRequest
        {
            Search = search,
            RoleId = roleId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(response);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet("validate-email-domain")]
    public async Task<ActionResult<bool>> ValidateEmailDomainAsync(
        [FromQuery] string email,
        CancellationToken cancellationToken = default)
    {
        var isValid = await _service.ValidateEmailDomainAsync(email, cancellationToken);
        return Ok(isValid);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _service.GetUserAsync(id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> CreateUserAsync([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);

        return result.Status switch
        {
            UserOperationStatus.ValidationError => BadRequest(result.Message),
            UserOperationStatus.Conflict => Conflict(result.Message),
            UserOperationStatus.Success => CreatedAtAction(nameof(GetUserAsync), new { id = result.User!.Id }, result.User),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> UpdateUserAsync(Guid id, [FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);

        return result.Status switch
        {
            UserOperationStatus.ValidationError => BadRequest(result.Message),
            UserOperationStatus.Conflict => Conflict(result.Message),
            UserOperationStatus.NotFound => NotFound(),
            UserOperationStatus.Success => Ok(result.User),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRolesAsync(Guid id, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AssignRolesAsync(id, request, cancellationToken);

        return result.Status switch
        {
            UserOperationStatus.ValidationError => BadRequest(result.Message),
            UserOperationStatus.NotFound => NotFound(),
            UserOperationStatus.Success => NoContent(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DeactivateAsync(id, cancellationToken);

        return result switch
        {
            UserOperationStatus.NotFound => NotFound(),
            UserOperationStatus.Success => NoContent(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
