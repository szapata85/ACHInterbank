using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : ControllerBase
{
    private readonly INachaFileBuilder _nachaBuilder;

    public NachaExportController(INachaFileBuilder nachaBuilder)
    {
        _nachaBuilder = nachaBuilder;
    }

    [HttpGet("{cycleId:int}")]
    public async Task<IActionResult> Export(int cycleId, CancellationToken ct)
    {
        string nachaContent = await _nachaBuilder.BuildNachaFileByCycleAsync(cycleId, ct);
        string fileName = $"NACHA_{cycleId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

        return File(Encoding.ASCII.GetBytes(nachaContent), "text/plain", fileName);
    }
}
