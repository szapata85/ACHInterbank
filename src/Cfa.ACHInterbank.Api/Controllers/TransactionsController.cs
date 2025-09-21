using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
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

    [HttpPost]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionDto dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Mapear addendas si llegan
        IEnumerable<(string addendaType, string information)>? addendas = null;
        if (dto.Addendas != null && dto.Addendas.Any())
        {
            addendas = dto.Addendas.Select(a => (a.AddendaType, a.Information));
        }

        var result = await _transactionService.RegisterTransactionAsync(
            amount: dto.Amount,
            reference: dto.Reference,
            type: dto.Type,
            destinationInstitutionId: dto.DestinationInstitutionId,
            sourceAccountNumber: dto.SourceAccountNumber,
            destinationAccountNumber: dto.DestinationAccountNumber,
            addendas: addendas,
            ct: ct);

        return CreatedAtAction(nameof(GetTransactionById), new { id = result.Id }, result);
    }


    /// <summary>
    /// Obtener transacción por Id
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTransactionById(int id, CancellationToken ct)
    {
        var tx = await _transactionService.GetTransactionsByCycleAsync(id, true, ct);
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
        var txs = await _transactionService.GetTransactionsByCycleAsync(cycleId, true, ct);
        return Ok(txs);
    }
}
