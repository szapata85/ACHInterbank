using Cfa.ACHInterbank.Application.Customers.Dtos;
using Cfa.ACHInterbank.Application.Customers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomersService _service;

    public CustomersController(ICustomersService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene el listado de clientes registrados.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IEnumerable<CustomerSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var customers = await _service.GetAllAsync(ct);
        return Ok(customers);
    }

    /// <summary>
    /// Obtiene el detalle de un cliente por identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var customer = await _service.GetByIdAsync(id, ct);

        if (customer is null)
        {
            return NotFound();
        }

        return Ok(customer);
    }

    /// <summary>
    /// Registra un nuevo cliente.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveCustomerRequest request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequest("El cuerpo de la solicitud no puede estar vacío.");
        }

        try
        {
            var response = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Actualiza la información de un cliente existente.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(CustomerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] SaveCustomerRequest request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return BadRequest("El cuerpo de la solicitud no puede estar vacío.");
        }

        try
        {
            var response = await _service.UpdateAsync(id, request, ct);
            if (response is null)
            {
                return NotFound();
            }

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Elimina un cliente por identificador.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
