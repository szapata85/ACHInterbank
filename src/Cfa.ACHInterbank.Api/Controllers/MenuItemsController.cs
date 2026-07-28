using Cfa.ACHInterbank.Application.Navigation.Dtos;
using Cfa.ACHInterbank.Application.Navigation.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("navigation/menu-items")]
[Route("api/navigation/menu-items")]
[Authorize(Roles = "Admin")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuItemsService _service;

    public MenuItemsController(IMenuItemsService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemAdminDto>>> GetMenuItemsAsync(CancellationToken cancellationToken)
    {
        var roots = await _service.GetAllAsync(cancellationToken);
        return Ok(roots);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    public async Task<ActionResult<MenuItemAdminDto>> CreateMenuItemAsync([FromBody] SaveMenuItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MenuItemAdminDto>> UpdateMenuItemAsync(int id, [FromBody] SaveMenuItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMenuItemAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
