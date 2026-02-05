using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            var failure = Result<UserSummaryDto>.Failure("USER_NOT_FOUND", "Usuario no encontrado", ErrorType.NotFound);
            return NotFound(ResponseApiService.Response(StatusCodes.Status404NotFound, failure));
        }

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, Result<UserSummaryDto>.Success(user)));
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> CreateUserAsync([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        var operation = await _service.CreateAsync(request, cancellationToken);
        var result = operation.ToResult();

        return operation.Status switch
        {
            UserOperationStatus.ValidationError => BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, result)),
            UserOperationStatus.Conflict => Conflict(ResponseApiService.Response(StatusCodes.Status409Conflict, result)),
            UserOperationStatus.Success => CreatedAtAction(nameof(GetUserAsync), new { id = operation.User!.Id }, ResponseApiService.Response(StatusCodes.Status201Created, result)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ResponseApiService.Response(StatusCodes.Status500InternalServerError, Result.Failure("USER_UNEXPECTED", "Error inesperado", ErrorType.Unexpected)))
        };
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> UpdateUserAsync(Guid id, [FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        var operation = await _service.UpdateAsync(id, request, cancellationToken);
        var result = operation.ToResult();

        return operation.Status switch
        {
            UserOperationStatus.ValidationError => BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, result)),
            UserOperationStatus.Conflict => Conflict(ResponseApiService.Response(StatusCodes.Status409Conflict, result)),
            UserOperationStatus.NotFound => NotFound(ResponseApiService.Response(StatusCodes.Status404NotFound, result)),
            UserOperationStatus.Success => Ok(ResponseApiService.Response(StatusCodes.Status200OK, result)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ResponseApiService.Response(StatusCodes.Status500InternalServerError, Result.Failure("USER_UNEXPECTED", "Error inesperado", ErrorType.Unexpected)))
        };
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRolesAsync(Guid id, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var operation = await _service.AssignRolesAsync(id, request, cancellationToken);
        var result = operation.ToResult();

        return operation.Status switch
        {
            UserOperationStatus.ValidationError => BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, result)),
            UserOperationStatus.NotFound => NotFound(ResponseApiService.Response(StatusCodes.Status404NotFound, result)),
            UserOperationStatus.Success => NoContent(),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ResponseApiService.Response(StatusCodes.Status500InternalServerError, Result.Failure("USER_UNEXPECTED", "Error inesperado", ErrorType.Unexpected)))
        };
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var operation = await _service.DeactivateAsync(id, cancellationToken);
        var result = operation.ToResult();

        return operation switch
        {
            UserOperationStatus.NotFound => NotFound(ResponseApiService.Response(StatusCodes.Status404NotFound, result)),
            UserOperationStatus.Success => NoContent(),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ResponseApiService.Response(StatusCodes.Status500InternalServerError, Result.Failure("USER_UNEXPECTED", "Error inesperado", ErrorType.Unexpected)))
        };
    }
}
