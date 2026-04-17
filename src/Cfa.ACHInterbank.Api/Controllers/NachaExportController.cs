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
    private readonly INachaFileIdentifierMapService _identifierMapService;
    private readonly IAchFileExportAuditService _fileExportAuditService;

    public NachaExportController(
        INachaFileBuilder nachaBuilder,
        ICryptoServiceScoped crypto,
        IAchCycleAppService cycleService,
        IClearingHouseService clearingHouseService,
        IDigitalEnvelopePolicy envelopePolicy,
        INachaFileIdentifierMapService identifierMapService,
        IAchFileExportAuditService fileExportAuditService)
    {
        _nachaBuilder = nachaBuilder;
        _crypto = crypto;
        _cycleService = cycleService;
        _clearingHouseService = clearingHouseService;
        _envelopePolicy = envelopePolicy;
        _identifierMapService = identifierMapService;
        _fileExportAuditService = fileExportAuditService;
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
            string normalizedNachaContent = await NormalizeFileHeaderIdentifierForCenitAsync(nachaContent, clearingHouse, fileName, ct);
            await _fileExportAuditService.RecordGeneratedFileAsync(
                cycle.Id,
                cycle.ClearingHouseId,
                "NACHA",
                fileName,
                CountRecords(normalizedNachaContent),
                CountTransactions(normalizedNachaContent),
                false,
                ct);

            return File(Encoding.ASCII.GetBytes(normalizedNachaContent), "text/plain", fileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Error Fatal ID", StringComparison.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(new
            {
                codigo = "NACHA_VALIDATION_ERROR",
                mensaje = ex.Message,
                cicloId = cycleId
            });
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
            string normalizedNachaContent = await NormalizeFileHeaderIdentifierForCenitAsync(nachaContent, clearingHouse, fileName, ct);
            await _fileExportAuditService.RecordGeneratedFileAsync(
                cycle.Id,
                cycle.ClearingHouseId,
                "NACHA",
                fileName,
                CountRecords(normalizedNachaContent),
                CountTransactions(normalizedNachaContent),
                forceEncryption || _envelopePolicy.ShouldEncrypt(cycle.ClearingHouseId),
                ct);

            if (!forceEncryption && !_envelopePolicy.ShouldEncrypt(cycle.ClearingHouseId))
            {
                return File(Encoding.ASCII.GetBytes(normalizedNachaContent), "text/plain", fileName);
            }

            byte[] digitalEnvelope = await _crypto.CreateEnvelopeAsync(Encoding.ASCII.GetBytes(normalizedNachaContent), fileName);
            string envelopeFileName = $"{fileName}.ENV";

            return File(digitalEnvelope, "application/xml", envelopeFileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Error Fatal ID", StringComparison.OrdinalIgnoreCase))
        {
            return UnprocessableEntity(new
            {
                codigo = "NACHA_VALIDATION_ERROR",
                mensaje = ex.Message,
                cicloId = cycleId
            });
        }
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

    private async Task<string> NormalizeFileHeaderIdentifierForCenitAsync(string nachaContent, ClearingHouseDto clearingHouse, string fileName, CancellationToken ct)
    {
        if (!string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return nachaContent;
        }

        int dailySequence = ResolveDailySequenceFromFileName(fileName);
        char expectedIdentifier = await _identifierMapService.ResolveIdentifierAsync(dailySequence, ct);
        return ReplaceHeaderPosition36(nachaContent, expectedIdentifier);
    }

    private static int ResolveDailySequenceFromFileName(string fileName)
    {
        Match match = Regex.Match(fileName, @"^[^.]+\.(\d{3})\.1$");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Nombre de archivo CENIT inválido: {fileName}.");
        }

        return int.Parse(match.Groups[1].Value);
    }

    private static string ReplaceHeaderPosition36(string nachaContent, char expectedIdentifier)
    {
        if (string.IsNullOrEmpty(nachaContent))
        {
            return nachaContent;
        }

        string[] lines = nachaContent.Split('\n');
        if (lines.Length == 0)
        {
            return nachaContent;
        }

        string firstLine = lines[0].TrimEnd('\r');
        if (firstLine.Length < 36)
        {
            return nachaContent;
        }

        char[] chars = firstLine.ToCharArray();
        chars[35] = expectedIdentifier;
        lines[0] = new string(chars);

        return string.Join('\n', lines);
    }

    private static int CountRecords(string nachaContent)
    {
        return string.IsNullOrEmpty(nachaContent) ? 0 : nachaContent.Length / 106;
    }

    private static int CountTransactions(string nachaContent)
    {
        if (string.IsNullOrEmpty(nachaContent))
        {
            return 0;
        }

        return Enumerable.Range(0, nachaContent.Length / 106)
            .Select(index => nachaContent[index * 106])
            .Count(recordType => recordType == '6');
    }
}
