using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : Controller
{
    private readonly INachaFileBuilder _builder;

    public NachaExportController(INachaFileBuilder builder)
    {
        _builder = builder;
    }

    [HttpGet("{cycleId}")]
    public async Task<IActionResult> Export(int cycleId, CancellationToken ct)
    {
        var fileBytes = await _builder.BuildNachaFileAsync(cycleId, ct);
        var fileName = $"NACHA_{cycleId}_{DateTime.Now:yyyyMMddHHmm}.txt";
        return File(fileBytes, "text/plain", fileName);
    }
}

