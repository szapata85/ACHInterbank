using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Api.Encryption;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
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
    private readonly IExternalFileNamePolicy _externalFileNamePolicy;

    public NachaExportController(
        INachaFileBuilder nachaBuilder,
        ICryptoServiceScoped crypto,
        IAchCycleAppService cycleService,
        IClearingHouseService clearingHouseService,
        IDigitalEnvelopePolicy envelopePolicy,
        INachaFileIdentifierMapService identifierMapService,
        IAchFileExportAuditService fileExportAuditService,
        IExternalFileNamePolicy externalFileNamePolicy)
    {
        _nachaBuilder = nachaBuilder;
        _crypto = crypto;
        _cycleService = cycleService;
        _clearingHouseService = clearingHouseService;
        _envelopePolicy = envelopePolicy;
        _identifierMapService = identifierMapService;
        _fileExportAuditService = fileExportAuditService;
        _externalFileNamePolicy = externalFileNamePolicy;
    }
    [EndpointSummary("Exportar archivo NACHA de un ciclo")]
    [EndpointDescription("Qué hace: genera/retorna archivo NACHA del ciclo con política de nombre externo y control de duplicados. Cuándo se usa: en cierre de ciclo para entrega interbancaria. Perfil consumidor: operación ACH de compensación. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por auditoría de exportación y nombre externo. Riesgos operativos: exportar ciclo no listo produce archivo inconsistente. Errores esperados: 404 ciclo/cámara no encontrada; 409 por política de nombre duplicado; 400 por validación. Relación ACH/CENIT/NACHA-M: salida NACHA-M oficial para flujo ACH/CENIT. Precauciones para desarrollo u operación: verificar estado de ciclo y correlación externa antes de distribuir.")]
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
            string internalFileName = BuildInternalNachaFileName(cycle);
            if (IsEmptyExport(nachaContent))
            {
                return EmptyExport(cycleId);
            }

            var fileNamePolicyResult = await GenerateAndEnforceExternalFileNamePolicyAsync(cycle, clearingHouse, internalFileName, nachaContent, ct);
            string fileName = fileNamePolicyResult.ExternalFileName;
            string normalizedNachaContent = NormalizeFileHeaderIdentifier(nachaContent, fileNamePolicyResult.Components.FileIdModifier);
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
        catch (InvalidOperationException ex)
        {
            return ExportPreconditionFailed(cycleId, ex);
        }
    }
    [EndpointSummary("Exportar NACHA con sobre digital")]
    [EndpointDescription("Qué hace: genera exportación aplicando cifrado según política o forzado explícito. Cuándo se usa: cuando la contraparte exige sobre digital. Perfil consumidor: operación ACH y seguridad. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por auditoría de exportación y seguridad. Riesgos operativos: cifrado inadecuado impide recepción por contraparte. Errores esperados: 400 por parámetros/política; 404 ciclo; 409 por conflictos de nombre. Relación ACH/CENIT/NACHA-M: integra generación NACHA-M con seguridad criptográfica operativa. Precauciones para desarrollo u operación: confirmar certificados activos y política de cifrado vigente.")]
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

            string internalFileName = BuildInternalNachaFileName(cycle);
            if (IsEmptyExport(nachaContent))
            {
                return EmptyExport(cycleId);
            }

            var fileNamePolicyResult = await GenerateAndEnforceExternalFileNamePolicyAsync(cycle, clearingHouse, internalFileName, nachaContent, ct);
            string fileName = fileNamePolicyResult.ExternalFileName;
            string normalizedNachaContent = NormalizeFileHeaderIdentifier(nachaContent, fileNamePolicyResult.Components.FileIdModifier);
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
        catch (InvalidOperationException ex)
        {
            return ExportPreconditionFailed(cycleId, ex);
        }
    }

    private static bool IsEmptyExport(string nachaContent)
        => string.IsNullOrWhiteSpace(nachaContent);

    private UnprocessableEntityObjectResult EmptyExport(string cycleId)
        => UnprocessableEntity(new
        {
            codigo = "NACHA_NO_EXPORTABLE_CONTENT",
            mensaje = "No hay transacciones exportables para el ciclo. No se generó archivo NACHA-M.",
            cicloId = cycleId
        });

    private UnprocessableEntityObjectResult ExportPreconditionFailed(string cycleId, InvalidOperationException ex)
    {
        var message = ex.Message ?? "No fue posible generar el archivo NACHA-M.";
        var code = ResolveNachaExportErrorCode(message);
        var userMessage = code == "NACHA_EXPORT_PREREQUISITE_FAILED"
            ? $"{message} No se generó archivo NACHA-M."
            : message;

        return UnprocessableEntity(new
        {
            codigo = code,
            mensaje = userMessage,
            cicloId = cycleId
        });
    }

    private static string ResolveNachaExportErrorCode(string message)
    {
        var knownCodes = new[]
        {
            "NACHA_PROFILE_NOT_PUBLISHED",
            "NACHA_PROFILE_NOT_EFFECTIVE",
            "NACHA_PROFILE_AMBIGUOUS",
            "NACHA_REQUIRED_RECORD_MISSING",
            "NACHA_REQUIRED_FIELD_MISSING",
            "NACHA_FIELD_SOURCE_NOT_FOUND",
            "NACHA_FIELD_LENGTH_INVALID",
            "NACHA_FIELD_VALIDATION_FAILED",
            "NACHA_CALCULATION_FAILED",
            "NACHA_LEGACY_GENERATION_DISABLED"
        };

        foreach (var code in knownCodes)
        {
            if (message.StartsWith(code, StringComparison.OrdinalIgnoreCase))
            {
                return code;
            }
        }

        if (message.Contains("Error Fatal ID", StringComparison.OrdinalIgnoreCase))
        {
            return "NACHA_VALIDATION_ERROR";
        }

        if (message.Contains("prenotificaci", StringComparison.OrdinalIgnoreCase))
        {
            return "NACHA_EXPORT_PREREQUISITE_FAILED";
        }

        if (message.Contains("no tiene transacciones", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no tiene lotes", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no se encontraron lotes", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no contiene transacciones exportables", StringComparison.OrdinalIgnoreCase))
        {
            return "NACHA_NO_EXPORTABLE_CONTENT";
        }

        return "NACHA_EXPORT_ERROR";
    }

    private static string BuildInternalNachaFileName(AchCycleDto cycle)
        => $"NACHA_{cycle.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.tmp";

    private static string NormalizeFileHeaderIdentifier(string nachaContent, char? expectedIdentifier)
    {
        if (!expectedIdentifier.HasValue)
        {
            return nachaContent;
        }

        return ReplaceHeaderPosition36(nachaContent, expectedIdentifier.Value);
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

    private async Task<ExternalFileNamePolicyResult> GenerateAndEnforceExternalFileNamePolicyAsync(
        AchCycleDto cycle,
        ClearingHouseDto clearingHouse,
        string legacyFileName,
        string nachaContent,
        CancellationToken ct)
    {
        var context = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouse.Id,
            ClearingHouseCode = clearingHouse.Code,
            ClearingHouseOriginCode = clearingHouse.OriginCode,
            CycleId = cycle.Id,
            CycleName = cycle.CycleName,
            CycleNumber = ResolveCycleNumber(cycle.CycleName, cycle.Id),
            ProcessingDate = cycle.ProcessingDate,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            ProvidedExternalFileName = null,
            InternalFileName = legacyFileName,
            NachaContent = nachaContent,
            RequestedBy = User?.Identity?.Name ?? "system"
        };

        var result = await _externalFileNamePolicy.GenerateExternalNameAsync(context, ct);
        if (result.Validation.IsHardBlocked)
        {
            var details = string.Join(" | ", result.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}"));
            throw new InvalidOperationException($"Error Fatal ID: External filename validation failed. {details}");
        }

        return result;
    }

    private static int ResolveCycleNumber(string? cycleName, string cycleId)
    {
        var match = Regex.Match(cycleName ?? string.Empty, @"(?<!\d)(\d+)(?!\d)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var cycleNumber) && cycleNumber > 0)
        {
            return cycleNumber;
        }

        match = Regex.Match(cycleId ?? string.Empty, @"(?<!\d)(\d+)(?!\d)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out cycleNumber) && cycleNumber > 0)
        {
            return cycleNumber;
        }

        throw new InvalidOperationException("No se pudo resolver el numero de ciclo para generar NACHA-M outbound.");
    }
}
