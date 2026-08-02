using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Api.Encryption;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : ControllerBase
{
    private readonly INachaFileBuilder _nachaBuilder;
    private readonly INachaExportDigitalEnvelopeService _digitalEnvelope;
    private readonly IAchCycleAppService _cycleService;
    private readonly IClearingHouseService _clearingHouseService;
    private readonly IDigitalEnvelopePolicy _envelopePolicy;
    private readonly INachaFileIdentifierMapService _identifierMapService;
    private readonly IAchFileExportAuditService _fileExportAuditService;
    private readonly IExternalFileNamePolicy _externalFileNamePolicy;
    private readonly ILogger<NachaExportController> _logger;
    private readonly IOperationalTimeSnapshotProvider _operationalTimeProvider;

    public NachaExportController(
        INachaFileBuilder nachaBuilder,
        INachaExportDigitalEnvelopeService digitalEnvelope,
        IAchCycleAppService cycleService,
        IClearingHouseService clearingHouseService,
        IDigitalEnvelopePolicy envelopePolicy,
        INachaFileIdentifierMapService identifierMapService,
        IAchFileExportAuditService fileExportAuditService,
        IExternalFileNamePolicy externalFileNamePolicy,
        ILogger<NachaExportController>? logger = null,
        IOperationalTimeSnapshotProvider? operationalTimeProvider = null)
    {
        _nachaBuilder = nachaBuilder;
        _digitalEnvelope = digitalEnvelope;
        _cycleService = cycleService;
        _clearingHouseService = clearingHouseService;
        _envelopePolicy = envelopePolicy;
        _identifierMapService = identifierMapService;
        _fileExportAuditService = fileExportAuditService;
        _externalFileNamePolicy = externalFileNamePolicy;
        _logger = logger ?? NullLogger<NachaExportController>.Instance;
        _operationalTimeProvider = operationalTimeProvider
            ?? new Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.OperationalTimeSnapshotProvider();
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

            var artifact = await _nachaBuilder.BuildNachaFileArtifactByCycleAsync(cycleId, ct);
            string nachaContent = artifact.Content;
            string internalFileName = BuildInternalNachaFileName(cycle);
            if (IsEmptyExport(nachaContent))
            {
                return EmptyExport(cycleId);
            }

            var fileNamePolicyResult = await GenerateAndEnforceExternalFileNamePolicyAsync(cycle, clearingHouse, internalFileName, nachaContent, ct);
            string fileName = fileNamePolicyResult.ExternalFileName;
            string normalizedNachaContent = NormalizeFileHeaderIdentifier(nachaContent, fileNamePolicyResult.Components.FileIdModifier);
            string contentSha256 = ComputeSha256(normalizedNachaContent);
            await _fileExportAuditService.RecordGeneratedFileAsync(
                cycle.Id,
                cycle.ClearingHouseId,
                "NACHA",
                fileName,
                CountRecords(normalizedNachaContent),
                CountTransactions(normalizedNachaContent),
                false,
                artifact.AchTransactionIds,
                contentSha256,
                ct);

            return File(Encoding.ASCII.GetBytes(normalizedNachaContent), "text/plain", fileName);
        }
        catch (NachaGenerationException ex)
        {
            return ExportPreconditionFailed(cycleId, ex);
        }
        catch (InvalidOperationException ex) when (TryResolveNachaExportErrorCode(ex.Message, out var code))
        {
            return ExportPreconditionFailed(cycleId, code, ex.Message);
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

            var artifact = await _nachaBuilder.BuildNachaFileArtifactByCycleAsync(cycleId, ct);
            string nachaContent = artifact.Content;
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
            string contentSha256 = ComputeSha256(normalizedNachaContent);
            var shouldEncrypt = forceEncryption || _envelopePolicy.ShouldEncrypt(cycle.ClearingHouseId);
            if (!shouldEncrypt)
            {
                await _fileExportAuditService.RecordGeneratedFileAsync(
                    cycle.Id,
                    cycle.ClearingHouseId,
                    "NACHA",
                    fileName,
                    CountRecords(normalizedNachaContent),
                    CountTransactions(normalizedNachaContent),
                    false,
                    artifact.AchTransactionIds,
                    contentSha256,
                    ct);
                return File(Encoding.ASCII.GetBytes(normalizedNachaContent), "text/plain", fileName);
            }

            var plainBytes = Encoding.ASCII.GetBytes(normalizedNachaContent);
            try
            {
                var envelope = await _digitalEnvelope.EncryptAsync(
                    cycle.ClearingHouseId,
                    fileName,
                    plainBytes,
                    User?.Identity?.Name ?? "system",
                    ct);
                await _fileExportAuditService.RecordGeneratedFileAsync(
                    cycle.Id,
                    cycle.ClearingHouseId,
                    "NACHA",
                    fileName,
                    CountRecords(normalizedNachaContent),
                    CountTransactions(normalizedNachaContent),
                    true,
                    artifact.AchTransactionIds,
                    contentSha256,
                    ct);
                if (ControllerContext.HttpContext is not null)
                {
                    Response.Headers["X-Cryptographic-Profile"] = envelope.CryptographicProfile;
                    Response.Headers["X-Plaintext-SHA256"] = contentSha256;
                }
                return File(envelope.Content, envelope.ContentType, envelope.FileName);
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
        catch (ManagedDigitalEnvelopeException ex)
        {
            return DigitalEnvelopeFailed(cycleId, ex);
        }
        catch (NachaGenerationException ex)
        {
            return ExportPreconditionFailed(cycleId, ex);
        }
        catch (InvalidOperationException ex) when (TryResolveNachaExportErrorCode(ex.Message, out var code))
        {
            return ExportPreconditionFailed(cycleId, code, ex.Message);
        }
    }

    private static bool IsEmptyExport(string nachaContent)
        => string.IsNullOrWhiteSpace(nachaContent);

    private static string ComputeSha256(string content)
        => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(content)));

    private UnprocessableEntityObjectResult EmptyExport(string cycleId)
        => UnprocessableEntity(CreateProblemDetails(
            StatusCodes.Status422UnprocessableEntity,
            "Ciclo sin contenido exportable",
            "No hay transacciones exportables para el ciclo. No se generó archivo NACHA-M.",
            "NACHA_NO_EXPORTABLE_CONTENT",
            cycleId));

    private UnprocessableEntityObjectResult ExportPreconditionFailed(string cycleId, string code, string message)
    {
        var userMessage = code == "NACHA_EXPORT_PREREQUISITE_FAILED"
            ? $"{message} No se generó archivo NACHA-M."
            : message;

        _logger.LogWarning(
            "NACHA export rejected. CycleId={CycleId} Phase={Phase} ErrorCode={ErrorCode}",
            cycleId,
            "Generation",
            code);
        return UnprocessableEntity(CreateProblemDetails(
            StatusCodes.Status422UnprocessableEntity,
            "No fue posible generar el archivo NACHA-M",
            userMessage,
            code,
            cycleId));
    }

    private UnprocessableEntityObjectResult ExportPreconditionFailed(
        string cycleId,
        NachaGenerationException exception)
    {
        var fieldDisplayName = ResolveFieldDisplayName(exception.FieldName);
        var detail = exception.Code == "NACHA_FIELD_RULE_FAILED" && fieldDisplayName is not null
            ? $"Una de las transacciones no tiene registrado {ResolveFieldPhrase(fieldDisplayName)}. "
              + "Este dato es obligatorio para construir el registro de detalle. "
              + "Corrige la información del destinatario y vuelve a intentar."
            : exception.UserMessage;

        _logger.LogWarning(
            "NACHA export rejected. CycleId={CycleId} Phase={Phase} ErrorCode={ErrorCode} RuleId={RuleId}",
            cycleId,
            "Generation",
            exception.Code,
            exception.RuleId);

        var support = new Dictionary<string, object?>
        {
            ["ruleId"] = exception.RuleId,
            ["chamber"] = exception.Chamber,
            ["recordType"] = exception.RecordType,
            ["fieldCode"] = exception.FieldName,
            ["fieldDisplayName"] = fieldDisplayName,
            ["startPosition"] = exception.StartPosition,
            ["expectedLength"] = exception.ExpectedLength,
            ["reason"] = exception.Cause
        };

        return UnprocessableEntity(CreateProblemDetails(
            StatusCodes.Status422UnprocessableEntity,
            "No fue posible generar el archivo NACHA-M",
            detail,
            exception.Code,
            cycleId,
            support));
    }

    private IActionResult DigitalEnvelopeFailed(string cycleId, ManagedDigitalEnvelopeException exception)
    {
        var status = exception.ErrorCode switch
        {
            "CERTIFICATE_NOT_FOUND" or "CERTIFICATE_INACTIVE" or "CERTIFICATE_EXPIRED" or "CERTIFICATE_NOT_YET_VALID"
                or "CERTIFICATE_PURPOSE_INVALID" or "CERTIFICATE_PRIVATE_KEY_REQUIRED"
                or "CERTIFICATE_PRIVATE_KEY_UNAVAILABLE" or "SIGNING_CERTIFICATE_NOT_FOUND"
                or "DIGITAL_ENVELOPE_CONFIGURATION_INVALID"
                => StatusCodes.Status422UnprocessableEntity,
            "FILE_TOO_LARGE" => StatusCodes.Status413PayloadTooLarge,
            "ENVELOPE_INVALID" or "ENVELOPE_INTEGRITY_INVALID" or "SIGNED_CONTENT_INVALID"
                => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
        _logger.LogWarning(
            "NACHA envelope export rejected. CycleId={CycleId} Phase={Phase} ErrorCode={ErrorCode}",
            cycleId,
            "DigitalEnvelope",
            exception.ErrorCode);
        return StatusCode(status, CreateProblemDetails(
            status,
            "No fue posible proteger el archivo NACHA-M",
            exception.Message,
            exception.ErrorCode,
            cycleId));
    }

    private ProblemDetails CreateProblemDetails(
        int status,
        string title,
        string detail,
        string code,
        string cycleId,
        IReadOnlyDictionary<string, object?>? support = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"urn:achinterbank:errors:{code.ToLowerInvariant().Replace('_', '-')}",
            Instance = HttpContext?.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["errorCode"] = code;
        problem.Extensions["cycleId"] = cycleId;
        problem.Extensions["traceId"] = HttpContext?.TraceIdentifier ?? System.Diagnostics.Activity.Current?.Id ?? "unavailable";
        if (support is not null)
        {
            foreach (var item in support.Where(item => item.Value is not null))
            {
                problem.Extensions[item.Key] = item.Value;
            }
        }
        return problem;
    }

    private static string? ResolveFieldDisplayName(string? fieldCode)
        => fieldCode?.ToUpperInvariant() switch
        {
            "INDIVIDUALNAME" => "Nombre del receptor",
            "RECEIVERNAME" => "Nombre del receptor",
            "COMPANYNAME" => "Nombre de la entidad originadora",
            "RECEIVINGDFIIDENTIFICATION" => "Entidad receptora",
            "DFIACCOUNTNUMBER" => "Cuenta receptora",
            _ => null
        };

    private static string ResolveFieldPhrase(string fieldDisplayName)
        => fieldDisplayName.StartsWith("Nombre", StringComparison.OrdinalIgnoreCase)
            ? $"el {fieldDisplayName.ToLowerInvariant()}"
            : $"la {fieldDisplayName.ToLowerInvariant()}";

    private static bool TryResolveNachaExportErrorCode(string message, out string code)
    {
        var knownCodes = new[]
        {
            "NACHA_PROFILE_NOT_PUBLISHED",
            "NACHA_PROFILE_NOT_EFFECTIVE",
            "NACHA_PROFILE_AMBIGUOUS",
            "NACHA_CLEARING_HOUSE_PROFILE_NOT_CONFIGURED",
            "NACHA_CLEARING_HOUSE_PROFILE_AMBIGUOUS",
            "NACHA_REQUIRED_RECORD_MISSING",
            "NACHA_REQUIRED_FIELD_MISSING",
            "NACHA_FIELD_SOURCE_NOT_FOUND",
            "NACHA_FIELD_LENGTH_INVALID",
            "NACHA_FIELD_VALIDATION_FAILED",
            "NACHA_CALCULATION_FAILED",
            "NACHA_LEGACY_GENERATION_DISABLED"
        };

        foreach (var knownCode in knownCodes)
        {
            if (message.StartsWith(knownCode, StringComparison.OrdinalIgnoreCase))
            {
                code = knownCode;
                return true;
            }
        }

        if (message.Contains("Error Fatal ID", StringComparison.OrdinalIgnoreCase))
        {
            code = "NACHA_VALIDATION_ERROR";
            return true;
        }

        if (message.Contains("prenotificaci", StringComparison.OrdinalIgnoreCase))
        {
            code = "NACHA_EXPORT_PREREQUISITE_FAILED";
            return true;
        }

        if (message.Contains("no tiene transacciones", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no tiene lotes", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no se encontraron lotes", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no contiene transacciones exportables", StringComparison.OrdinalIgnoreCase))
        {
            code = "NACHA_NO_EXPORTABLE_CONTENT";
            return true;
        }

        code = string.Empty;
        return false;
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
        Match match = Regex.Match(fileName, @"^[^.]+\.(\d{3})\.\d+$");
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
        var operationalSnapshot = _operationalTimeProvider.GetOrCreate(
            $"NACHA:{cycle.Id}",
            DateOnly.FromDateTime(cycle.ProcessingDate),
            TimeOnly.FromTimeSpan(cycle.CutoffTime));
        var context = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouse.Id,
            ClearingHouseCode = clearingHouse.Code,
            ClearingHouseOriginCode = clearingHouse.OriginCode,
            CycleId = cycle.Id,
            CycleName = cycle.CycleName,
            CycleNumber = ResolveCycleNumber(cycle.CycleName),
            ProcessingDate = operationalSnapshot.BogotaTimestamp,
            OperationalTimeSnapshot = operationalSnapshot,
            IdempotencyKey = $"NACHA_OUT|CH:{clearingHouse.Id}|CYCLE:{cycle.Id}",
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

    private static int ResolveCycleNumber(string? cycleName)
    {
        if (TryResolveUniquePositiveCycleNumber(cycleName, out var cycleNumber))
        {
            return cycleNumber;
        }

        throw new InvalidOperationException("No se pudo resolver un numero de ciclo positivo unico desde CycleName para generar NACHA-M outbound.");
    }

    private static bool TryResolveUniquePositiveCycleNumber(string? value, out int cycleNumber)
    {
        cycleNumber = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var matches = Regex.Matches(value, @"(?<!\d)(?<cycle>\d+)(?!\d)");
        if (matches.Count == 0)
        {
            return false;
        }

        var positiveValues = new List<int>();
        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups["cycle"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return false;
            }

            if (parsedValue > 0)
            {
                positiveValues.Add(parsedValue);
            }
        }

        if (positiveValues.Count != 1)
        {
            return false;
        }

        cycleNumber = positiveValues[0];
        return true;
    }
}
