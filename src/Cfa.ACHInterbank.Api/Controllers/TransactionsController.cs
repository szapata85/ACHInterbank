using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IAchTransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        IAchTransactionService transactionService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AchTransactionListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? achCycleId,
        [FromQuery] string? achCycleName,
        [FromQuery] DateTime? effectiveDate,
        [FromQuery] int? clearingHouseId,
        CancellationToken ct)
    {
        var transactions = await _transactionService.GetAllAsync(achCycleId, achCycleName, effectiveDate, clearingHouseId, ct);
        return Ok(transactions);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost]
    [ProducesResponseType(typeof(AchTransaction), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTransaction([FromBody] AchTransactionRequest request, CancellationToken ct)
    {
        if (request is null) return BadRequest("El cuerpo de la solicitud no puede estar vacío.");
        if (!request.IsPrenotification && request.Amount <= 0) return BadRequest("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(request.Reference)) return BadRequest("La referencia es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.SourceAccountNumber)) return BadRequest("La cuenta de origen es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.DestinationAccountNumber)) return BadRequest("La cuenta de destino es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.CompanyName)) return BadRequest("El nombre del usuario originador es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.CompanyIdentification)) return BadRequest("La identificación del usuario originador es obligatoria.");

        try
        {
            var tx = await _transactionService.RegisterTransactionAsync(
                amount: request.Amount,
                reference: request.Reference,
                type: request.Type,
                accountType: request.AccountType,
                isPrenotification: request.IsPrenotification,
                destinationInstitutionId: request.DestinationInstitutionId,
                sourceAccountNumber: request.SourceAccountNumber,
                destinationAccountNumber: request.DestinationAccountNumber,
                companyName: request.CompanyName,
                companyIdentification: request.CompanyIdentification,
                companyEntryDescription: request.CompanyEntryDescription,
                recipientIdNumber: request.RecipientIdNumber,
                requiresIdentityValidation: request.RequiresIdentityValidation,
                addendas: request.Addendas,
                ct: ct
            );

            _logger.LogInformation("Transacción ACH creada: {Id}", tx.Id);
            return CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida al registrar transacción");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Regla de negocio incumplida al registrar transacción");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al registrar transacción");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno del servidor" });
        }
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AchTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var tx = await _transactionService.GetTransactionByIdAsync(id, ct);
        if (tx == null)
            return NotFound(new { message = $"No se encontró la transacción con ID {id}" });

        return Ok(tx);
    }
}

// ✅ DTO para solicitud de creación
public class AchTransactionRequest
{
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public TransactionTypeEnum Type { get; set; }
    public AccountTypeEnum AccountType { get; set; } = AccountTypeEnum.Checking;
    public bool IsPrenotification { get; set; }
    public int DestinationInstitutionId { get; set; }
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string? RecipientIdNumber { get; set; }
    public bool RequiresIdentityValidation { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentification { get; set; } = string.Empty;
    public string CompanyEntryDescription { get; set; } = "PAGOS";

    public List<AddendaDto>? Addendas { get; set; }
}
