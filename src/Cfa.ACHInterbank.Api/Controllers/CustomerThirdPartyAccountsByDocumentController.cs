using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Customers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>Consulta unificada, de solo lectura, de cuentas de terceros por documento del cliente.</summary>
[ApiController]
[Route("api/third-party-accounts/by-customer-document")]
[Authorize]
public sealed class CustomerThirdPartyAccountsByDocumentController : ControllerBase
{
    private readonly ICustomersService _customers;
    private readonly IThirdPartyAccountReadService _accounts;

    public CustomerThirdPartyAccountsByDocumentController(
        ICustomersService customers,
        IThirdPartyAccountReadService accounts)
    {
        _customers = customers;
        _accounts = accounts;
    }

    /// <summary>
    /// Resuelve un cliente por documento y devuelve sus cuentas de terceros junto con
    /// validaciones/prenotificaciones agrupables por cámara, naturaleza y servicio.
    /// Una validación ausente no se presenta como aprobada ni como rechazada.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(CustomerThirdPartyAccountsByDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [FromQuery] string? documentType,
        [FromQuery] string? documentNumber,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(documentNumber))
        {
            return BadRequest(new
            {
                Code = "CUSTOMER_DOCUMENT_REQUIRED",
                Message = "El tipo y número de documento del cliente son obligatorios."
            });
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest(new
            {
                Code = "INVALID_PAGINATION",
                Message = "La página debe ser positiva y el tamaño debe estar entre 1 y 100."
            });
        }

        var customer = await _customers.GetByDocumentAsync(
            documentType.Trim().ToUpperInvariant(),
            documentNumber.Trim(),
            ct);
        if (customer is null)
        {
            return NotFound();
        }

        // Reutiliza exclusivamente el lector canónico con ámbito de Customer.
        // No acepta contexto de elegibilidad suministrado por el consumidor.
        var accounts = await _accounts.GetAsync(new ThirdPartyAccountReadQuery
        {
            CustomerId = customer.Id,
            Page = page,
            PageSize = pageSize,
            EligibilityContext = null
        }, ct);

        return Ok(new CustomerThirdPartyAccountsByDocumentDto
        {
            Customer = customer,
            Accounts = accounts
        });
    }
}
