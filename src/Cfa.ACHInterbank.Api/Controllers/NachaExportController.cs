using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : ControllerBase
{
    private readonly INachaExportService _exportService;

    public NachaExportController(INachaExportService exportService)
    {
        _exportService = exportService;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("{cycleId}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Export(string cycleId, CancellationToken ct)
    {
        try
        {
            var export = await _exportService.ExportAsync(cycleId, ct);
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No existe"))
        {
            return NotFound(new { message = ex.Message });
        }
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("{cycleId}/sobre-digital")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> ExportEncrypted(string cycleId, [FromQuery] bool forceEncryption = false, CancellationToken ct = default)
    {
        try
        {
            var export = await _exportService.ExportEncryptedAsync(cycleId, forceEncryption, ct);
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No existe"))
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
