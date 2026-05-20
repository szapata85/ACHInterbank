using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.NachaSecurity;

[Scoped]
public class NachaSecurityOperationService : INachaSecurityOperationService
{
    private readonly AchDbContext _context;
    private readonly INachaFileBuilder _nachaBuilder;
    private readonly IAchCycleAppService _cycleService;
    private readonly IClearingHouseService _clearingHouseService;
    private readonly INachaFileIdentifierMapService _identifierMapService;
    private readonly IExternalFileNamePolicy _externalFileNamePolicy;
    private readonly ICryptoServiceScoped _cryptoService;
    private readonly IOperationArtifactStore _artifactStore;
    private readonly OperationArtifactOptions _artifactOptions;
    private readonly ILogger<NachaSecurityOperationService> _logger;

    public NachaSecurityOperationService(
        AchDbContext context,
        INachaFileBuilder nachaBuilder,
        IAchCycleAppService cycleService,
        IClearingHouseService clearingHouseService,
        INachaFileIdentifierMapService identifierMapService,
        IExternalFileNamePolicy externalFileNamePolicy,
        ICryptoServiceScoped cryptoService,
        IOperationArtifactStore artifactStore,
        IOptions<OperationArtifactOptions> artifactOptions,
        ILogger<NachaSecurityOperationService> logger)
    {
        _context = context;
        _nachaBuilder = nachaBuilder;
        _cycleService = cycleService;
        _clearingHouseService = clearingHouseService;
        _identifierMapService = identifierMapService;
        _externalFileNamePolicy = externalFileNamePolicy;
        _cryptoService = cryptoService;
        _artifactStore = artifactStore;
        _artifactOptions = artifactOptions.Value ?? new OperationArtifactOptions();
        _logger = logger;
    }

    public Task<DigitalEnvelopeOperationDto> GeneratePlainAsync(NachaGenerateRequest request, OperationRequestContext context, CancellationToken cancellationToken = default)
        => GenerateNachaInternalAsync(request, context, false, cancellationToken);

    public Task<DigitalEnvelopeOperationDto> GenerateEncryptedAsync(NachaGenerateRequest request, OperationRequestContext context, CancellationToken cancellationToken = default)
        => GenerateNachaInternalAsync(request, context, true, cancellationToken);

    public async Task<DigitalEnvelopeOperationDto> ManualEncryptAsync(ManualEnvelopeRequest request, OperationRequestContext context, CancellationToken cancellationToken = default)
    {
        var operation = await CreateOperationAsync(NachaSecurityOperationType.ManualEnvelopeEncrypt, context, cancellationToken);

        try
        {
            var envelope = await _cryptoService.CreateEnvelopeAsync(request.FileContent, request.OriginalFileName);
            var envelopeHash = ComputeSha256(envelope);
            var plainHash = ComputeSha256(request.FileContent);
            var artifactPath = await _artifactStore.SaveAsync(operation.OperationId, ".ENV", envelope, cancellationToken);

            operation.Status = NachaSecurityOperationStatus.Success;
            operation.ExternalFileName = $"{request.OriginalFileName}.ENV";
            operation.PlainHashSha256 = plainHash;
            operation.EnvelopeHashSha256 = envelopeHash;
            operation.ArtifactRelativePath = artifactPath;
            operation.ArtifactContentType = "application/xml";
            operation.ArtifactSizeBytes = envelope.Length;
            operation.DownloadAvailable = true;
            operation.DownloadExpiresAtUtc = DateTime.UtcNow.AddMinutes(_artifactOptions.DefaultExpirationMinutes);
            operation.FinishedAtUtc = DateTime.UtcNow;
            operation.RowVersion = NewRowVersion();
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(operation);
        }
        catch (Exception ex)
        {
            MarkAsFailed(operation, "ENVELOPE_ENCRYPT_FAILED", "No fue posible cifrar el archivo.");
            _logger.LogWarning(ex, "Manual encrypt failed. OperationId={OperationId}", operation.OperationId);
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(operation);
        }
    }

    public async Task<DigitalEnvelopeOperationDto> ManualDecryptAsync(ManualEnvelopeRequest request, OperationRequestContext context, CancellationToken cancellationToken = default)
    {
        var operation = await CreateOperationAsync(NachaSecurityOperationType.ManualEnvelopeDecrypt, context, cancellationToken);

        try
        {
            var plain = await _cryptoService.OpenEnvelopeAsync(request.FileContent, request.OriginalFileName);
            var envelopeHash = ComputeSha256(request.FileContent);
            var plainHash = ComputeSha256(plain);
            var defaultName = request.OriginalFileName.EndsWith(".env", StringComparison.OrdinalIgnoreCase)
                ? request.OriginalFileName[..^4]
                : $"{request.OriginalFileName}.txt";
            var artifactPath = await _artifactStore.SaveAsync(operation.OperationId, ".txt", plain, cancellationToken);

            operation.Status = NachaSecurityOperationStatus.Success;
            operation.ExternalFileName = defaultName;
            operation.PlainHashSha256 = plainHash;
            operation.EnvelopeHashSha256 = envelopeHash;
            operation.ArtifactRelativePath = artifactPath;
            operation.ArtifactContentType = "text/plain";
            operation.ArtifactSizeBytes = plain.Length;
            operation.DownloadAvailable = true;
            operation.DownloadExpiresAtUtc = DateTime.UtcNow.AddMinutes(_artifactOptions.DefaultExpirationMinutes);
            operation.FailCloseApplied = true;
            operation.FinishedAtUtc = DateTime.UtcNow;
            operation.RowVersion = NewRowVersion();
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(operation);
        }
        catch (Exception ex)
        {
            MarkAsFailed(operation, "SIGNATURE_VALIDATION_FAILED", "No fue posible validar la firma del sobre digital.");
            operation.FailCloseApplied = true;
            _logger.LogWarning(ex, "Manual decrypt failed. OperationId={OperationId}", operation.OperationId);
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(operation);
        }
    }

    public async Task<DigitalEnvelopeOperationDto?> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken = default)
    {
        var operation = await _context.NachaSecurityOperations.AsNoTracking().FirstOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (operation is null)
        {
            return null;
        }

        if (operation.DownloadExpiresAtUtc.HasValue && operation.DownloadExpiresAtUtc <= DateTime.UtcNow && operation.Status == NachaSecurityOperationStatus.Success)
        {
            operation.Status = NachaSecurityOperationStatus.Expired;
        }

        return ToDto(operation);
    }

    public async Task<IReadOnlyList<DigitalEnvelopeOperationDto>> ListAuditAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var rows = await _context.NachaSecurityOperations
            .AsNoTracking()
            .OrderByDescending(x => x.RequestedAtUtc)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<DownloadAuthorizationResult> AuthorizeDownloadAsync(string operationId, OperationRequestContext context, CancellationToken cancellationToken = default)
    {
        var operation = await _context.NachaSecurityOperations.FirstOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (operation is null)
        {
            return new DownloadAuthorizationResult(false, null, "OPERATION_NOT_FOUND", "Operación no encontrada.");
        }

        if (!operation.DownloadAvailable || string.IsNullOrWhiteSpace(operation.ArtifactRelativePath))
        {
            return new DownloadAuthorizationResult(false, null, "DOWNLOAD_NOT_AVAILABLE", "La operación no tiene descarga disponible.");
        }

        if (operation.Status != NachaSecurityOperationStatus.Success)
        {
            return new DownloadAuthorizationResult(false, null, "OPERATION_NOT_SUCCESS", "La operación no está en estado exitoso.");
        }

        if (operation.DownloadExpiresAtUtc.HasValue && operation.DownloadExpiresAtUtc <= DateTime.UtcNow)
        {
            operation.Status = NachaSecurityOperationStatus.Expired;
            operation.RowVersion = NewRowVersion();
            await _context.SaveChangesAsync(cancellationToken);
            return new DownloadAuthorizationResult(false, null, "DOWNLOAD_EXPIRED", "El artefacto expiró.");
        }

        operation.DownloadAuthorizedUntilUtc = DateTime.UtcNow.AddMinutes(5);
        operation.RowVersion = NewRowVersion();
        await _context.SaveChangesAsync(cancellationToken);
        return new DownloadAuthorizationResult(true, operation.DownloadAuthorizedUntilUtc, null, null);
    }

    public async Task<OperationDownloadDescriptor?> OpenDownloadAsync(string operationId, OperationRequestContext context, CancellationToken cancellationToken = default)
    {
        var operation = await _context.NachaSecurityOperations.FirstOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (operation is null || !operation.DownloadAvailable || string.IsNullOrWhiteSpace(operation.ArtifactRelativePath))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (!operation.DownloadAuthorizedUntilUtc.HasValue || operation.DownloadAuthorizedUntilUtc.Value < now)
        {
            return null;
        }

        if (operation.DownloadExpiresAtUtc.HasValue && operation.DownloadExpiresAtUtc.Value < now)
        {
            operation.Status = NachaSecurityOperationStatus.Expired;
            operation.RowVersion = NewRowVersion();
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }

        var stream = await _artifactStore.OpenReadAsync(operation.ArtifactRelativePath, cancellationToken);
        if (stream is null)
        {
            return null;
        }

        operation.DownloadAuthorizedUntilUtc = null;
        operation.RowVersion = NewRowVersion();
        await _context.SaveChangesAsync(cancellationToken);

        return new OperationDownloadDescriptor(
            operation.ExternalFileName ?? $"{operation.OperationId}.dat",
            operation.ArtifactContentType ?? "application/octet-stream",
            stream,
            operation.DownloadExpiresAtUtc);
    }

    private async Task<DigitalEnvelopeOperationDto> GenerateNachaInternalAsync(
        NachaGenerateRequest request,
        OperationRequestContext context,
        bool encrypted,
        CancellationToken cancellationToken)
    {
        var operation = await CreateOperationAsync(encrypted ? NachaSecurityOperationType.NachaGenerateEncrypted : NachaSecurityOperationType.NachaGeneratePlain, context, cancellationToken);

        try
        {
            var cycle = await _cycleService.GetByIdAsync(request.CycleId, cancellationToken);
            if (cycle is null)
            {
                MarkAsFailed(operation, "CYCLE_NOT_FOUND", "No se encontró el ciclo ACH solicitado.");
                await _context.SaveChangesAsync(cancellationToken);
                return ToDto(operation);
            }

            var clearingHouse = await _clearingHouseService.GetByIdAsync(cycle.ClearingHouseId, cancellationToken);
            if (clearingHouse is null)
            {
                MarkAsFailed(operation, "CLEARING_HOUSE_NOT_FOUND", "No se encontró la cámara compensadora del ciclo.");
                await _context.SaveChangesAsync(cancellationToken);
                return ToDto(operation);
            }

            operation.ClearingHouseId = cycle.ClearingHouseId;

            var nacha = await _nachaBuilder.BuildNachaFileByCycleAsync(request.CycleId, cancellationToken);
            var legacyFileName = BuildNachaFileName(clearingHouse, cycle);
            var normalized = await NormalizeFileHeaderIdentifierForCenitAsync(nacha, clearingHouse, legacyFileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                MarkAsFailed(operation, "NACHA_NO_EXPORTABLE_CONTENT", "No hay transacciones exportables para el ciclo. No se generó archivo NACHA-M.");
                await _context.SaveChangesAsync(cancellationToken);
                return ToDto(operation);
            }

            var filePolicy = await GenerateAndEnforceExternalFileNamePolicyAsync(cycle, clearingHouse, legacyFileName, normalized, context.RequestedBy, cancellationToken);
            var outputFileName = encrypted ? $"{filePolicy.ExternalFileName}.ENV" : filePolicy.ExternalFileName;

            byte[] output;
            string contentType;
            string? plainHash;
            string? envelopeHash;

            if (encrypted)
            {
                output = await _cryptoService.CreateEnvelopeAsync(Encoding.ASCII.GetBytes(normalized), filePolicy.ExternalFileName);
                contentType = "application/xml";
                plainHash = ComputeSha256(Encoding.ASCII.GetBytes(normalized));
                envelopeHash = ComputeSha256(output);
            }
            else
            {
                output = Encoding.ASCII.GetBytes(normalized);
                contentType = "text/plain";
                plainHash = ComputeSha256(output);
                envelopeHash = null;
            }

            var artifactPath = await _artifactStore.SaveAsync(operation.OperationId, encrypted ? ".ENV" : ".txt", output, cancellationToken);
            operation.Status = NachaSecurityOperationStatus.Success;
            operation.ExternalFileName = outputFileName;
            operation.PlainHashSha256 = plainHash;
            operation.EnvelopeHashSha256 = envelopeHash;
            operation.ArtifactRelativePath = artifactPath;
            operation.ArtifactContentType = contentType;
            operation.ArtifactSizeBytes = output.LongLength;
            operation.DownloadAvailable = true;
            operation.DownloadExpiresAtUtc = DateTime.UtcNow.AddMinutes(_artifactOptions.DefaultExpirationMinutes);
            operation.FinishedAtUtc = DateTime.UtcNow;
            operation.RowVersion = NewRowVersion();
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(operation);
        }
        catch (InvalidOperationException ex)
        {
            var code = ResolveNachaGenerationErrorCode(ex.Message);
            var message = code == "NACHA_EXPORT_PREREQUISITE_FAILED"
                ? $"{ex.Message} No se generó archivo NACHA-M."
                : ex.Message;
            MarkAsFailed(operation, code, message);
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(operation);
        }
        catch (Exception ex)
        {
            MarkAsFailed(operation, "NACHA_OPERATION_FAILED", "No fue posible procesar la operación solicitada.");
            _logger.LogError(ex, "Nacha operation failed. OperationId={OperationId}", operation.OperationId);
            await _context.SaveChangesAsync(cancellationToken);
            return ToDto(operation);
        }
    }

    private async Task<NachaSecurityOperation> CreateOperationAsync(NachaSecurityOperationType type, OperationRequestContext context, CancellationToken cancellationToken)
    {
        var row = new NachaSecurityOperation
        {
            OperationId = $"op_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..38],
            OperationType = type,
            Status = NachaSecurityOperationStatus.Running,
            RequestedBy = string.IsNullOrWhiteSpace(context.RequestedBy) ? "system" : context.RequestedBy,
            RequestedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = NewRowVersion()
        };

        _context.NachaSecurityOperations.Add(row);
        await _context.SaveChangesAsync(cancellationToken);
        return row;
    }

    private static void MarkAsFailed(NachaSecurityOperation operation, string code, string message)
    {
        operation.Status = NachaSecurityOperationStatus.Failed;
        operation.ErrorCode = code;
        operation.ErrorMessageSanitized = message.Length > 500 ? message[..500] : message;
        operation.DownloadAvailable = false;
        operation.FinishedAtUtc = DateTime.UtcNow;
        operation.RowVersion = NewRowVersion();
    }

    private static string ResolveNachaGenerationErrorCode(string message)
    {
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

        return "NACHA_OPERATION_FAILED";
    }

    private static DigitalEnvelopeOperationDto ToDto(NachaSecurityOperation x)
    {
        var status = x.Status;
        if (x.DownloadExpiresAtUtc.HasValue && x.DownloadExpiresAtUtc <= DateTime.UtcNow && status == NachaSecurityOperationStatus.Success)
        {
            status = NachaSecurityOperationStatus.Expired;
        }

        return new DigitalEnvelopeOperationDto(
            x.OperationId,
            x.OperationType,
            status,
            x.ClearingHouseId,
            x.RequestedBy,
            x.RequestedAtUtc,
            x.FinishedAtUtc,
            x.FailCloseApplied,
            x.LegacyFallbackUsed,
            new DigitalEnvelopeOperationArtifactDto(
                x.ExternalFileName,
                x.ArtifactContentType,
                x.PlainHashSha256,
                x.EnvelopeHashSha256,
                x.DownloadAvailable,
                x.DownloadExpiresAtUtc,
                x.ArtifactSizeBytes),
            string.IsNullOrWhiteSpace(x.ErrorCode)
                ? null
                : new DigitalEnvelopeOperationErrorDto(x.ErrorCode!, x.ErrorMessageSanitized ?? "Error en operación.", Retryable: false),
            new DigitalEnvelopeCertificateSummaryDto(x.SigningCertificateThumbprintMasked, x.EncryptionCertificateThumbprintMasked, null));
    }

    private static string ComputeSha256(byte[] content)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(content));
    }

    private static byte[] NewRowVersion() => Guid.NewGuid().ToByteArray();

    private static string BuildNachaFileName(ClearingHouseDto clearingHouse, AchCycleDto cycle)
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
        var match = Regex.Match(cycleName, @"\d+");
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

        var lines = nachaContent.Split('\n');
        if (lines.Length == 0)
        {
            return nachaContent;
        }

        var firstLine = lines[0].TrimEnd('\r');
        if (firstLine.Length < 36)
        {
            return nachaContent;
        }

        var chars = firstLine.ToCharArray();
        chars[35] = expectedIdentifier;
        lines[0] = new string(chars);

        return string.Join('\n', lines);
    }

    private async Task<ExternalFileNamePolicyResult> GenerateAndEnforceExternalFileNamePolicyAsync(
        AchCycleDto cycle,
        ClearingHouseDto clearingHouse,
        string legacyFileName,
        string nachaContent,
        string requestedBy,
        CancellationToken ct)
    {
        var isAch = string.Equals(clearingHouse.Code, "ACH", StringComparison.OrdinalIgnoreCase);
        var context = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouse.Id,
            ClearingHouseCode = clearingHouse.Code,
            ClearingHouseOriginCode = clearingHouse.OriginCode,
            CycleId = cycle.Id,
            CycleName = cycle.CycleName,
            ProcessingDate = cycle.ProcessingDate,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            ProvidedExternalFileName = isAch ? null : legacyFileName,
            InternalFileName = legacyFileName,
            NachaContent = nachaContent,
            RequestedBy = requestedBy
        };

        var result = await _externalFileNamePolicy.GenerateExternalNameAsync(context, ct);
        if (result.Validation.IsHardBlocked)
        {
            var details = string.Join(" | ", result.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}"));
            throw new InvalidOperationException($"Error Fatal ID: External filename validation failed. {details}");
        }

        return result;
    }
}
