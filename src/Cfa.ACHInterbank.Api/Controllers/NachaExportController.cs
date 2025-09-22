using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : ControllerBase
{
    private readonly INachaFileBuilder _nachaBuilder;
    private readonly AchDbContext _context;

    public NachaExportController(INachaFileBuilder nachaBuilder, AchDbContext context)
    {
        _nachaBuilder = nachaBuilder;
        _context = context;
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime date, CancellationToken ct)
    {
        var txs = await _context.AchTransactions
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Where(t => t.CreatedAt.Date == date.Date)
            .ToListAsync(ct);

        if (!txs.Any())
            return NotFound("No hay transacciones para la fecha solicitada.");

        string fileContent = await _nachaBuilder.BuildNachaFileAsync(txs, ct);
        byte[] bytes = Encoding.ASCII.GetBytes(fileContent);

        return File(bytes, "text/plain", $"NACHA_{date:yyyyMMdd}.txt");
    }
}
