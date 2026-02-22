using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Api.Encryption;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : ControllerBase
{
    private readonly INachaFileBuilder _nachaBuilder;
    private readonly ICryptoServiceScoped _crypto;
    private readonly IAchCycleAppService _cycleService;
    private readonly IClearingHouseService _clearingHouseService;
    private readonly IDigitalEnvelopePolicy _envelopePolicy;

    public NachaExportController(
        INachaFileBuilder nachaBuilder,
        ICryptoServiceScoped crypto,
        IAchCycleAppService cycleService,
        IClearingHouseService clearingHouseService,
        IDigitalEnvelopePolicy envelopePolicy)
    {
        _nachaBuilder = nachaBuilder;
        _crypto = crypto;
        _cycleService = cycleService;
        _clearingHouseService = clearingHouseService;
        _envelopePolicy = envelopePolicy;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet("{cycleId}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Export(string cycleId, CancellationToken ct)
    {
        AchCycleDto? cycle = await _cycleService.GetByIdAsync(cycleId, ct);
        if (cycle is null)
        {
            return NotFound();
        }

        ClearingHouseDto? clearingHouse = await _clearingHouseService.GetByIdAsync(cycle.ClearingHouseId, ct);
        if (clearingHouse is null)
        {
            return NotFound();
        }

        string nachaContent = await _nachaBuilder.BuildNachaFileByCycleAsync(cycleId, ct);
        string fileName = BuildNachaFileName(clearingHouse, cycle);

        return File(Encoding.ASCII.GetBytes(nachaContent), "text/plain", fileName);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

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
        ClearingHouseDto? clearingHouse = await _clearingHouseService.GetByIdAsync(cycle.ClearingHouseId, ct);
        if (clearingHouse is null)
        {
            return NotFound();
        }

        string fileName = BuildNachaFileName(clearingHouse, cycle);

        if (!forceEncryption && !_envelopePolicy.ShouldEncrypt(cycle.ClearingHouseId))
        {
            return File(Encoding.ASCII.GetBytes(nachaContent), "text/plain", fileName);
        }

        byte[] digitalEnvelope = await _crypto.CreateEnvelopeAsync(Encoding.ASCII.GetBytes(nachaContent), fileName);
        string envelopeFileName = $"{fileName}.ENV";

        return File(digitalEnvelope, "application/xml", envelopeFileName);
    }

    private static string BuildNachaFileName(ClearingHouseDto clearingHouse, AchCycleDto cycle)
    {
        if (string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            string cycleNumber = GetCenitCycleNumber(cycle.CycleName);
            return $"{clearingHouse.OriginCode}.{cycleNumber}.1";
        }

        return $"NACHA_{cycle.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
    }

    private static string GetCenitCycleNumber(string cycleName)
    {
        Match match = Regex.Match(cycleName, @"\d+");
        if (!match.Success || !int.TryParse(match.Value, out int cycleNumber))
        {
            return "001";
        }

        return cycleNumber.ToString("D3");
    }
}
