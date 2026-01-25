using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly AchDbContext _context;

    public CustomersController(AchDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene el listado de clientes registrados.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IEnumerable<CustomerSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Select(c => new CustomerSummaryDto
            {
                Id = c.Id,
                DocumentType = c.DocumentType,
                DocumentNumber = c.DocumentNumber,
                AccountNumber = c.AccountNumber,
                PersonType = c.PersonType,
                CompanyName = c.CompanyName,
                FullName = string.Join(" ", new[]
                {
                    c.FirstName,
                    c.MiddleName,
                    c.LastName,
                    c.SecondLastName
                }.Where(part => !string.IsNullOrWhiteSpace(part)))
            })
            .OrderBy(c => c.FullName)
            .ToListAsync(ct);

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
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CustomerDetailDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                MiddleName = c.MiddleName,
                LastName = c.LastName,
                SecondLastName = c.SecondLastName,
                Gender = c.Gender,
                PersonType = c.PersonType,
                CompanyName = c.CompanyName,
                DocumentType = c.DocumentType,
                DocumentNumber = c.DocumentNumber,
                AccountNumber = c.AccountNumber
            })
            .FirstOrDefaultAsync(ct);

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

        var validation = ValidateRequest(request);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            return BadRequest(validation);
        }

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            LastName = request.LastName.Trim(),
            SecondLastName = request.SecondLastName?.Trim(),
            Gender = request.Gender?.Trim(),
            PersonType = request.PersonType.Trim(),
            CompanyName = request.CompanyName?.Trim(),
            DocumentType = request.DocumentType.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            AccountNumber = request.AccountNumber.Trim()
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);

        var response = new CustomerDetailDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            MiddleName = customer.MiddleName,
            LastName = customer.LastName,
            SecondLastName = customer.SecondLastName,
            Gender = customer.Gender,
            PersonType = customer.PersonType,
            CompanyName = customer.CompanyName,
            DocumentType = customer.DocumentType,
            DocumentNumber = customer.DocumentNumber,
            AccountNumber = customer.AccountNumber
        };

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, response);
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

        var validation = ValidateRequest(request);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            return BadRequest(validation);
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return NotFound();
        }

        customer.FirstName = request.FirstName.Trim();
        customer.MiddleName = request.MiddleName?.Trim();
        customer.LastName = request.LastName.Trim();
        customer.SecondLastName = request.SecondLastName?.Trim();
        customer.Gender = request.Gender?.Trim();
        customer.PersonType = request.PersonType.Trim();
        customer.CompanyName = request.CompanyName?.Trim();
        customer.DocumentType = request.DocumentType.Trim();
        customer.DocumentNumber = request.DocumentNumber.Trim();
        customer.AccountNumber = request.AccountNumber.Trim();

        await _context.SaveChangesAsync(ct);

        var response = new CustomerDetailDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            MiddleName = customer.MiddleName,
            LastName = customer.LastName,
            SecondLastName = customer.SecondLastName,
            Gender = customer.Gender,
            PersonType = customer.PersonType,
            CompanyName = customer.CompanyName,
            DocumentType = customer.DocumentType,
            DocumentNumber = customer.DocumentNumber,
            AccountNumber = customer.AccountNumber
        };

        return Ok(response);
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
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return NotFound();
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? ValidateRequest(SaveCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            return "El tipo de documento es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            return "El número de documento es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            return "La cuenta es obligatoria.";
        }

        if (string.IsNullOrWhiteSpace(request.PersonType))
        {
            return "El tipo de persona es obligatorio.";
        }

        var personType = request.PersonType.Trim().ToUpperInvariant();
        if (personType == "PJ" && string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return "La razón social es obligatoria para persona jurídica.";
        }

        if (personType == "PN" && string.IsNullOrWhiteSpace(request.FirstName))
        {
            return "El nombre es obligatorio para persona natural.";
        }

        if (personType == "PN" && string.IsNullOrWhiteSpace(request.LastName))
        {
            return "El apellido es obligatorio para persona natural.";
        }

        return null;
    }
}

public record CustomerSummaryDto
{
    public int Id { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string PersonType { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string FullName { get; init; } = string.Empty;
}

public record CustomerDetailDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string? SecondLastName { get; init; }
    public string? Gender { get; init; }
    public string PersonType { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
}

public record SaveCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string? SecondLastName { get; init; }
    public string? Gender { get; init; }
    public string PersonType { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentNumber { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
}
