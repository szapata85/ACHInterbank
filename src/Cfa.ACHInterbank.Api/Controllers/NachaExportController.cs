using System.Globalization;
using Cfa.ACHInterbank.Api.OpenApi;
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
using System.IO.Compression;

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
    private readonly ICenitGatewayTransportAdapter? _cenitGatewayTransport;

    public NachaExportController(
        INachaFileBuilder nachaBuilder,
        INachaExportDigitalEnvelopeService digitalEnvelope,
        IAchCycleAppService cycleService,
        IClearingHouseService clearingHouseService,
        IDigitalEnvelopePolicy envelopePolicy,
        INachaFileIdentifierMapService identifierMapService,
        IAchFileExportAuditService fileExportAuditService,
        IExternalFileNamePolicy externalFileNamePolicy,
        ILogger<NachaExportController> logger,
        IOperationalTimeSnapshotProvider operationalTimeProvider,
        ICenitGatewayTransportAdapter? cenitGatewayTransport = null)
    {
        _nachaBuilder = nachaBuilder;
        _digitalEnvelope = digitalEnvelope;
        _cycleService = cycleService;
        _clearingHouseService = clearingHouseService;
        _envelopePolicy = envelopePolicy;
        _identifierMapService = identifierMapService;
        _fileExportAuditService = fileExportAuditService;
        _externalFileNamePolicy = externalFileNamePolicy;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operationalTimeProvider = operationalTimeProvider ?? throw new ArgumentNullException(nameof(operationalTimeProvider));
        _cenitGatewayTransport = cenitGatewayTransport;
    }
    /// <summary>Genera y descarga los artefactos NACHA-M asociados a un ciclo ACH.</summary>
    /// <param name="cycleId">Identificador del ciclo ACH cuya exportación NACHA-M se genera.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar archivo NACHA-M generado para un ciclo")]
    [EndpointDescription("Busca el ciclo y su cámara, genera los artefactos NACHA-M y aplica la política de nombre externo antes de devolverlos. Con un único artefacto responde text/plain y usa el ExternalFileName resuelto como nombre de descarga; con varios artefactos responde application/zip y usa el primer nombre externo seguido de .zip. Si no existe el ciclo o su cámara responde 404 sin body. Si no hay contenido exportable o una validación/generación controlada falla responde 422 con ProblemDetails.")]
    [HttpGet("{cycleId}")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("text/plain", "application/zip", "application/problem+json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo de descarga: text/plain para un único artefacto con Content-Disposition basado en el ExternalFileName resuelto; application/zip para múltiples artefactos con nombre <primer ExternalFileName>.zip.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el ciclo solicitado o no existe la cámara asociada al ciclo; la respuesta no tiene body.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "No existe contenido exportable o una validación/precondición controlada impide generar el archivo; el body es ProblemDetails.")]
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

            string internalFileName = BuildInternalNachaFileName(cycle);
            var buildResult = await BuildFilesAsync(cycle, clearingHouse, ct);
            if (buildResult.Files.Count == 0)
            {
                return EmptyExport(cycleId);
            }

            var files = new List<(string FileName, byte[] Content)>();
            var generatedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < buildResult.Files.Count; index++)
            {
                var artifact = buildResult.Files[index];
                if (IsEmptyExport(artifact.Content))
                {
                    return EmptyExport(cycleId);
                }

                var partitionKey = buildResult.Files.Count == 1
                    ? null
                    : $"{artifact.ProfileIdentity}|{index + 1}";
                var fileNamePolicyResult = await GenerateAndEnforceExternalFileNamePolicyAsync(
                    cycle,
                    clearingHouse,
                    internalFileName,
                    artifact.Content,
                    partitionKey,
                    ct);
                var fileName = fileNamePolicyResult.ExternalFileName;
                EnsureUniqueFileName(generatedFileNames, fileName);
                var normalized = NormalizeFileHeaderIdentifier(artifact.Content, fileNamePolicyResult.Components.FileIdModifier);
                var contentSha256 = ComputeSha256(normalized);
                await _fileExportAuditService.RecordGeneratedFileAsync(
                    cycle.Id,
                    cycle.ClearingHouseId,
                    "NACHA",
                    fileName,
                    CountRecords(normalized),
                    CountTransactions(normalized),
                    false,
                    artifact.AchTransactionIds,
                    contentSha256,
                    ct);
                var bytes = Encoding.ASCII.GetBytes(normalized);
                if (string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase)
                    && _cenitGatewayTransport?.Enabled == true)
                {
                    await _cenitGatewayTransport.HandoffOutboundAsync(fileName, bytes, ct);
                }
                files.Add((fileName, bytes));
            }

            return files.Count == 1
                ? File(files[0].Content, "text/plain", files[0].FileName)
                : File(BuildZip(files), "application/zip", $"{files[0].FileName}.zip");
        }
        catch (NachaGenerationException ex)
        {
            return ExportPreconditionFailed(cycleId, ex);
        }
        catch (InvalidOperationException ex) when (TryResolveNachaExportErrorCode(ex.Message, out var code))
        {
            return ExportPreconditionFailed(cycleId, code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NACHA_EXPORT_UNEXPECTED_FAILURE CycleId={CycleId}", cycleId);
            throw;
        }
    }
    /// <summary>Genera y descarga la exportación NACHA-M del ciclo aplicando la política de sobre digital.</summary>
    /// <param name="cycleId">Identificador del ciclo ACH cuya exportación NACHA-M se genera.</param>
    /// <param name="forceEncryption">Indica si la solicitud fuerza el cifrado, además del cifrado que pueda exigir la política de sobre digital.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar exportación NACHA-M con política de sobre digital")]
    [EndpointDescription("Genera los artefactos NACHA-M del ciclo y aplica cifrado cuando forceEncryption es true o la política de sobre digital lo requiere. Un único artefacto sin cifrar se devuelve como text/plain con el nombre externo resuelto; un único artefacto cifrado usa el ContentType y FileName entregados por el servicio de sobre digital (la implementación vigente devuelve application/octet-stream y agrega .ENV al nombre de entrada). Si se generan varios artefactos, la respuesta es application/zip con nombre basado en el primer archivo devuelto. Para un único archivo cifrado también se emiten X-Cryptographic-Profile y X-Plaintext-SHA256. Los errores controlados de sobre digital se traducen a 400, 413 o 422; ciclo/cámara inexistente produce 404 sin body.")]
    [HttpGet("{cycleId}/sobre-digital")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("text/plain", "application/octet-stream", "application/zip", "application/problem+json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo de descarga con Content-Disposition dinámico: text/plain cuando no se cifra, application/octet-stream para el único sobre cifrado de la implementación vigente, o application/zip cuando hay múltiples artefactos. En el caso cifrado de un solo archivo se emiten X-Cryptographic-Profile y X-Plaintext-SHA256.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La operación de sobre digital produjo un error controlado que no corresponde a tamaño ni a contenido no procesable; el body es ProblemDetails.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el ciclo solicitado o no existe la cámara asociada al ciclo; la respuesta no tiene body.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "El servicio de sobre digital rechazó el contenido por exceder el tamaño permitido; el body es ProblemDetails.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "No existe contenido exportable, falla una precondición de generación o el sobre digital/certificado produce un error controlado no procesable; el body es ProblemDetails.")]
    public async Task<IActionResult> ExportEncrypted(string cycleId, [FromQuery] bool forceEncryption = false, CancellationToken ct = default)
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

            string internalFileName = BuildInternalNachaFileName(cycle);
            var buildResult = await BuildFilesAsync(cycle, clearingHouse, ct);
            if (buildResult.Files.Count == 0)
            {
                return EmptyExport(cycleId);
            }

            var shouldEncrypt = forceEncryption || _envelopePolicy.ShouldEncrypt(cycle.ClearingHouseId);
            var files = new List<(string FileName, byte[] Content, string ContentType)>();
            var generatedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < buildResult.Files.Count; index++)
            {
                var artifact = buildResult.Files[index];
                if (IsEmptyExport(artifact.Content))
                {
                    return EmptyExport(cycleId);
                }

                var partitionKey = buildResult.Files.Count == 1
                    ? null
                    : $"{artifact.ProfileIdentity}|{index + 1}";
                var fileNamePolicyResult = await GenerateAndEnforceExternalFileNamePolicyAsync(
                    cycle,
                    clearingHouse,
                    internalFileName,
                    artifact.Content,
                    partitionKey,
                    ct);
                var fileName = fileNamePolicyResult.ExternalFileName;
                var normalized = NormalizeFileHeaderIdentifier(artifact.Content, fileNamePolicyResult.Components.FileIdModifier);
                var contentSha256 = ComputeSha256(normalized);

                if (!shouldEncrypt)
                {
                    EnsureUniqueFileName(generatedFileNames, fileName);
                    await _fileExportAuditService.RecordGeneratedFileAsync(
                        cycle.Id,
                        cycle.ClearingHouseId,
                        "NACHA",
                        fileName,
                        CountRecords(normalized),
                        CountTransactions(normalized),
                        false,
                        artifact.AchTransactionIds,
                        contentSha256,
                        ct);
                    files.Add((fileName, Encoding.ASCII.GetBytes(normalized), "text/plain"));
                    continue;
                }

                var plainBytes = Encoding.ASCII.GetBytes(normalized);
                try
                {
                    var envelope = await _digitalEnvelope.EncryptAsync(
                        cycle.ClearingHouseId,
                        fileName,
                        plainBytes,
                        User?.Identity?.Name ?? "system",
                        ct);
                    EnsureUniqueFileName(generatedFileNames, envelope.FileName);
                    await _fileExportAuditService.RecordGeneratedFileAsync(
                        cycle.Id,
                        cycle.ClearingHouseId,
                        "NACHA",
                        fileName,
                        CountRecords(normalized),
                        CountTransactions(normalized),
                        true,
                        artifact.AchTransactionIds,
                        contentSha256,
                        ct);
                    if (ControllerContext.HttpContext is not null && buildResult.Files.Count == 1)
                    {
                        Response.Headers["X-Cryptographic-Profile"] = envelope.CryptographicProfile;
                        Response.Headers["X-Plaintext-SHA256"] = contentSha256;
                    }
                    files.Add((envelope.FileName, envelope.Content, envelope.ContentType));
                }
                finally
                {
                    System.Security.Cryptography.CryptographicOperations.ZeroMemory(plainBytes);
                }
            }

            if (files.Count == 1)
            {
                return File(files[0].Content, files[0].ContentType, files[0].FileName);
            }

            return File(
                BuildZip(files.Select(file => (file.FileName, file.Content)).ToArray()),
                "application/zip",
                $"{files[0].FileName}.zip");
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
        string? partitionKey,
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
            IdempotencyKey = string.IsNullOrWhiteSpace(partitionKey)
                ? $"NACHA_OUT|CH:{clearingHouse.Id}|CYCLE:{cycle.Id}"
                : $"NACHA_OUT|CH:{clearingHouse.Id}|CYCLE:{cycle.Id}|PARTITION:{partitionKey}",
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

    private async Task<NachaFileBuildResult> BuildFilesAsync(
        AchCycleDto cycle,
        ClearingHouseDto clearingHouse,
        CancellationToken ct)
    {
        if (string.Equals(clearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return await _nachaBuilder.BuildNachaFilesByCycleAsync(cycle.Id, ct);
        }

        return new NachaFileBuildResult(
        [
            await _nachaBuilder.BuildNachaFileArtifactByCycleAsync(cycle.Id, ct)
        ]);
    }

    private static byte[] BuildZip(IReadOnlyCollection<(string FileName, byte[] Content)> files)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.FileName, CompressionLevel.NoCompression);
                using var entryStream = entry.Open();
                entryStream.Write(file.Content);
            }
        }

        return output.ToArray();
    }

    private static void EnsureUniqueFileName(ISet<string> generatedFileNames, string fileName)
    {
        if (!generatedFileNames.Add(fileName))
        {
            throw new NachaGenerationException(
                "NACHA_OUTPUT_FILENAME_DUPLICATED",
                $"El nombre externo '{fileName}' fue asignado a más de un archivo generado.");
        }
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
