using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Api.Encryption;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : ControllerBase
{
    private readonly INachaFileBuilder _nachaBuilder;
    private readonly ICryptoServiceScoped _crypto;
    private readonly IAchCycleAppService _cycleService;
    private readonly IDigitalEnvelopePolicy _envelopePolicy;

    public NachaExportController(
        INachaFileBuilder nachaBuilder,
        ICryptoServiceScoped crypto,
        IAchCycleAppService cycleService,
        IDigitalEnvelopePolicy envelopePolicy)
    {
        _nachaBuilder = nachaBuilder;
        _crypto = crypto;
        _cycleService = cycleService;
        _envelopePolicy = envelopePolicy;
    }

    [HttpGet("{cycleId}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Export(string cycleId, CancellationToken ct)
    {
        string nachaContent = await _nachaBuilder.BuildNachaFileByCycleAsync(cycleId, ct);
        string fileName = $"NACHA_{cycleId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

        return File(Encoding.ASCII.GetBytes(nachaContent), "text/plain", fileName);
    }

    [HttpGet("{cycleId}/sobre-digital")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> ExportEncrypted(string cycleId, [FromQuery] bool forceEncryption = false, CancellationToken ct = default)
    {
        AchCycleDto? cycle = await _cycleService.GetByIdAsync(cycleId, ct);
        if (cycle is null)
        {
            return NotFound();
        }

        string nachaContent = await _nachaBuilder.BuildNachaFileByCycleAsync(cycleId, ct);
        string fileName = $"NACHA_{cycleId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

        if (!forceEncryption && !_envelopePolicy.ShouldEncrypt(cycle.ClearingHouseId))
        {
            return File(Encoding.ASCII.GetBytes(nachaContent), "text/plain", fileName);
        }

        byte[] digitalEnvelope = await _crypto.CreateEnvelopeAsync(Encoding.ASCII.GetBytes(nachaContent), fileName);
        string envelopeFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.ENV";

        return File(digitalEnvelope, "application/xml", envelopeFileName);
    }
}
