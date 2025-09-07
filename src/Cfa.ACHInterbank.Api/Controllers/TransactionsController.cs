using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TransactionsController : Controller
{
    private readonly IAchTransactionService _transactionService;

    public TransactionsController(IAchTransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    /// <summary>
    /// Registrar una nueva transacción ACH con addendas opcionales
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var tx = await _transactionService.RegisterTransactionAsync(
            request.Amount,
            request.Reference,
            request.Type,
            request.SourceInstitutionId,
            request.DestinationInstitutionId,
            request.AchCycleId,
            request.Addendas?.Select(a => (a.AddendaType, a.Information)),
            ct
        );

        return CreatedAtAction(nameof(GetTransactionById), new { id = tx.Id }, tx);
    }

    /// <summary>
    /// Obtener transacción por Id
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTransactionById(int id, CancellationToken ct)
    {
        var tx = await _transactionService.GetTransactionsByCycleAsync(id, ct);
        if (tx == null)
            return NotFound();

        return Ok(tx);
    }

    /// <summary>
    /// Listar transacciones por ciclo
    /// </summary>
    [HttpGet("cycle/{cycleId:int}")]
    public async Task<IActionResult> GetTransactionsByCycle(int cycleId, CancellationToken ct)
    {
        var txs = await _transactionService.GetTransactionsByCycleAsync(cycleId, ct);
        return Ok(txs);
    }
}
