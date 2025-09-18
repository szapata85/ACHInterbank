using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class FinancialInstitutionController : Controller
{
    private readonly IFinancialInstitutionService _service;

    public FinancialInstitutionController(IFinancialInstitutionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(includeInactive, ct));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        => Ok(await _service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FinancialInstitutionDto dto, CancellationToken ct = default)
        => Ok(await _service.CreateAsync(dto, ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] FinancialInstitutionDto dto, CancellationToken ct = default)
    {
        if (id != dto.Id) return BadRequest();
        return Ok(await _service.UpdateAsync(dto, ct));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] FinancialInstitutionStatus status, CancellationToken ct = default)
    {
        await _service.SetStatusAsync(id, status, ct);
        return NoContent();
    }
}
