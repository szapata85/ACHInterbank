using System.Text;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Api.Encryption;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaExportService : INachaExportService
{
    private readonly INachaFileBuilder _nachaBuilder;
    private readonly ICryptoServiceScoped _crypto;
    private readonly IAchCycleAppService _cycleService;
    private readonly IClearingHouseService _clearingHouseService;
    private readonly IDigitalEnvelopePolicy _envelopePolicy;
    private readonly INachaFileIdentifierMapService _identifierMapService;
    private readonly INachaFormatValidator _formatValidator;

    public NachaExportService(
        INachaFileBuilder nachaBuilder,
        ICryptoServiceScoped crypto,
        IAchCycleAppService cycleService,
        IClearingHouseService clearingHouseService,
        IDigitalEnvelopePolicy envelopePolicy,
        INachaFileIdentifierMapService identifierMapService,
        INachaFormatValidator formatValidator)
    {
        _nachaBuilder = nachaBuilder;
        _crypto = crypto;
        _cycleService = cycleService;
        _clearingHouseService = clearingHouseService;
        _envelopePolicy = envelopePolicy;
        _identifierMapService = identifierMapService;
        _formatValidator = formatValidator;
    }

    public async Task<NachaExportResult> ExportAsync(string cycleId, CancellationToken ct = default)
    {
        var export = await BuildPlainExportAsync(cycleId, ct);
        return new NachaExportResult(Encoding.ASCII.GetBytes(export.Content), "text/plain", export.FileName, false);
    }

    public async Task<NachaExportResult> ExportEncryptedAsync(string cycleId, bool forceEncryption = false, CancellationToken ct = default)
    {
        var export = await BuildPlainExportAsync(cycleId, ct);

        if (!forceEncryption && !_envelopePolicy.ShouldEncrypt(export.Cycle.ClearingHouseId))
        {
            return new NachaExportResult(Encoding.ASCII.GetBytes(export.Content), "text/plain", export.FileName, false);
        }

        var digitalEnvelope = await _crypto.CreateEnvelopeAsync(Encoding.ASCII.GetBytes(export.Content), export.FileName);
        return new NachaExportResult(digitalEnvelope, "application/xml", $"{export.FileName}.ENV", true);
    }

    private async Task<(string Content, string FileName, AchCycleDto Cycle, ClearingHouseDto ClearingHouse)> BuildPlainExportAsync(string cycleId, CancellationToken ct)
    {
        var cycle = await _cycleService.GetByIdAsync(cycleId, ct)
            ?? throw new InvalidOperationException($"No existe el ciclo {cycleId}.");

        var clearingHouse = await _clearingHouseService.GetByIdAsync(cycle.ClearingHouseId, ct)
            ?? throw new InvalidOperationException($"No existe la cámara {cycle.ClearingHouseId}.");

        var nachaContent = await _nachaBuilder.BuildNachaFileByCycleAsync(cycleId, ct);
        var fileName = BuildNachaFileName(clearingHouse, cycle);
        var normalizedContent = await NormalizeFileHeaderIdentifierForCenitAsync(nachaContent, clearingHouse, fileName, ct);

        _formatValidator.ValidateOrThrow(normalizedContent, new NachaValidationContext(cycle, clearingHouse));

        return (normalizedContent, fileName, cycle, clearingHouse);
    }

    public static string BuildNachaFileName(ClearingHouseDto clearingHouse, AchCycleDto cycle)
    {
        if (string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            var cycleNumber = GetCenitCycleNumber(cycle.CycleName);
            return $"{clearingHouse.OriginCode}.{cycleNumber}.1";
        }

        return $"NACHA_{cycle.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
    }

    private static string GetCenitCycleNumber(string cycleName)
    {
        var match = Regex.Match(cycleName ?? string.Empty, @"\d+");
        if (!match.Success || !int.TryParse(match.Value, out var cycleNumber))
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

        var dailySequence = ResolveDailySequenceFromFileName(fileName);
        var expectedIdentifier = await _identifierMapService.ResolveIdentifierAsync(dailySequence, ct);
        return ReplaceHeaderPosition36(nachaContent, expectedIdentifier);
    }

    private static int ResolveDailySequenceFromFileName(string fileName)
    {
        var match = Regex.Match(fileName, @"^[^.]+\.(\d{3})\.1$");
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

        if (nachaContent.Length < 36)
        {
            return nachaContent;
        }

        var chars = nachaContent.ToCharArray();
        chars[35] = expectedIdentifier;
        return new string(chars);
    }
}
