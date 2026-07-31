using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-security/certificates/management")]
[Route("api/nacha-security/certificates/management")]
[Authorize]
public class CertificateManagementController : ControllerBase
{
    private const long MaximumCertificateSize = 10L * 1024 * 1024;
    private readonly ICertificateLoadService _loadService;
    private readonly ICertificateCatalogService _catalogService;
    private readonly ICertificateActivationService _activationService;
    private readonly ICertificateValidationService _validationService;
    private readonly ICertificateAuditService _auditService;
    private readonly ICertificateDeletionService _deletionService;

    public CertificateManagementController(
        ICertificateLoadService loadService,
        ICertificateCatalogService catalogService,
        ICertificateActivationService activationService,
        ICertificateValidationService validationService,
        ICertificateAuditService auditService,
        ICertificateDeletionService deletionService)
    {
        _loadService = loadService;
        _catalogService = catalogService;
        _activationService = activationService;
        _validationService = validationService;
        _auditService = auditService;
        _deletionService = deletionService;
    }

    [EndpointSummary("Verificar un certificado antes de guardarlo")]
    [HttpPost("managed/preview")]
    [Authorize(Policy = P1Policies.CertificatesUploadPublic)]
    [RequestSizeLimit(MaximumCertificateSize)]
    public async Task<ActionResult<CertificatePreviewDto>> PreviewManagedAsync(
        [FromForm] ManagedCertificateApiRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateManagedRequest(request);
        if (validation is not null) return validation;

        var content = await ReadFileAsync(request.File!, cancellationToken);
        try
        {
            return Ok(await _loadService.PreviewManagedCertificateAsync(
                new PreviewManagedCertificateRequest(
                    request.Purpose,
                    request.ClearingHouseId,
                    content,
                    request.Password,
                    Path.GetFileName(request.File!.FileName)),
                cancellationToken));
        }
        catch (CertificateValidationException exception)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "No fue posible verificar el certificado", exception.Message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(content);
        }
    }

    [EndpointSummary("Guardar un certificado administrado")]
    [HttpPost("managed")]
    [Authorize(Policy = P1Policies.CertificatesRegisterPrivate)]
    [RequestSizeLimit(MaximumCertificateSize)]
    public async Task<ActionResult<CertificateVersionApiDto>> SaveManagedAsync(
        [FromForm] ManagedCertificateApiRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateManagedRequest(request);
        if (validation is not null) return validation;

        var content = await ReadFileAsync(request.File!, cancellationToken);
        try
        {
            var result = await _loadService.SaveManagedCertificateAsync(
                new SaveManagedCertificateRequest(
                    request.Purpose,
                    request.ClearingHouseId,
                    content,
                    request.Password,
                    Path.GetFileName(request.File!.FileName),
                    ResolveActor()),
                cancellationToken);
            return Ok(ToApiDto(result));
        }
        catch (CertificateConflictException exception)
        {
            return FunctionalProblem(StatusCodes.Status409Conflict, "Certificado duplicado", exception.Message);
        }
        catch (CertificateValidationException exception)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "No fue posible guardar el certificado", exception.Message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(content);
        }
    }

    [EndpointSummary("Cargar certificado público")]
    [HttpPost("public")]
    [Authorize(Policy = P1Policies.CertificatesUploadPublic)]
    [RequestSizeLimit(MaximumCertificateSize)]
    public async Task<ActionResult<CertificateVersionApiDto>> UploadPublicAsync(
        [FromForm] UploadPublicCertificateApiRequest request,
        CancellationToken cancellationToken)
    {
        var requestError = ValidateLegacyUploadRequest(request, [".cer", ".crt", ".pem"]);
        if (requestError is not null) return requestError;

        var rawCertificate = await ReadFileAsync(request.File!, cancellationToken);
        try
        {
            var dto = await _loadService.LoadPublicCertificateAsync(
                new LoadPublicCertificateRequest(
                    request.Code.Trim(),
                    request.DisplayName.Trim(),
                    request.ClearingHouseId,
                    request.Environment,
                    request.Purpose,
                    request.HolderType,
                    rawCertificate,
                    ResolveActor(),
                    Path.GetFileName(request.File!.FileName)),
                cancellationToken);
            return Ok(ToApiDto(dto));
        }
        catch (CertificateConflictException exception)
        {
            return FunctionalProblem(StatusCodes.Status409Conflict, "Certificado duplicado", exception.Message);
        }
        catch (CertificateValidationException exception)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Certificado público inválido", exception.Message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawCertificate);
        }
    }

    [EndpointSummary("Registrar certificado privado")]
    [HttpPost("private")]
    [Authorize(Policy = P1Policies.CertificatesRegisterPrivate)]
    [RequestSizeLimit(MaximumCertificateSize)]
    public async Task<ActionResult<CertificateVersionApiDto>> UploadPrivateAsync(
        [FromForm] UploadPrivateCertificateApiRequest request,
        CancellationToken cancellationToken)
    {
        var requestError = ValidateLegacyUploadRequest(request, [".pfx", ".p12"]);
        if (requestError is not null) return requestError;
        if (string.IsNullOrEmpty(request.Password))
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Contraseña requerida", "Ingresa la contraseña del archivo PFX.");
        }
        if (request.StorageMode != CertificateStorageMode.DatabaseEncrypted)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Almacenamiento no permitido", "El material privado debe guardarse con protección de la aplicación.");
        }

        var rawPkcs12 = await ReadFileAsync(request.File!, cancellationToken);
        try
        {
            var dto = await _loadService.RegisterPrivateCertificateAsync(
                new RegisterPrivateCertificateRequest(
                    request.Code.Trim(),
                    request.DisplayName.Trim(),
                    request.ClearingHouseId,
                    request.Environment,
                    request.Purpose,
                    request.HolderType,
                    rawPkcs12,
                    request.Password,
                    ResolveActor(),
                    request.StorageMode,
                    request.SecretRef,
                    Path.GetFileName(request.File!.FileName)),
                cancellationToken);
            return Ok(ToApiDto(dto));
        }
        catch (CertificateConflictException exception)
        {
            return FunctionalProblem(StatusCodes.Status409Conflict, "Certificado duplicado", exception.Message);
        }
        catch (CertificateValidationException exception)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Certificado privado inválido", exception.Message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawPkcs12);
        }
    }

    [EndpointSummary("Consultar certificados")]
    [HttpGet]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    public async Task<ActionResult<IEnumerable<CertificateVersionApiDto>>> ListAsync(
        [FromQuery] int? clearingHouseId,
        [FromQuery] CertificateEnvironment? environment,
        [FromQuery] CertificatePurpose? purpose,
        [FromQuery] CertificateHolderType? holderType,
        [FromQuery] CertificateStatus? status,
        CancellationToken cancellationToken)
    {
        var items = await _catalogService.GetCertificatesAsync(
            new CertificateFilterDto(clearingHouseId, environment, purpose, holderType, status),
            cancellationToken);
        return Ok(items.Select(ToApiDto));
    }

    [HttpGet("{id:int}/versions")]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    public async Task<ActionResult<IEnumerable<CertificateVersionApiDto>>> ListVersionsAsync(
        int id,
        CancellationToken cancellationToken)
        => Ok((await _catalogService.GetVersionsAsync(id, cancellationToken)).Select(ToApiDto));

    [HttpPost("versions/{id:int}/activate")]
    [Authorize(Policy = P1Policies.CertificatesActivate)]
    public async Task<ActionResult<CertificateVersionApiDto>> ActivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(ToApiDto(await _activationService.ActivateVersionAsync(
                new ActivateCertificateVersionRequest(id, ResolveActor()),
                cancellationToken)));
        }
        catch (Exception exception) when (exception is CertificateValidationException or InvalidOperationException)
        {
            return FunctionalProblem(StatusCodes.Status409Conflict, "No fue posible activar el certificado", exception.Message);
        }
    }

    [HttpPost("versions/{id:int}/revoke")]
    [Authorize(Policy = P1Policies.CertificatesRevoke)]
    public async Task<ActionResult<CertificateVersionApiDto>> RevokeAsync(
        int id,
        [FromBody] RevokeVersionBody body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.Reason))
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Motivo requerido", "Ingresa el motivo de la revocación.");
        }

        try
        {
            return Ok(ToApiDto(await _activationService.RevokeVersionAsync(
                new RevokeCertificateVersionRequest(id, ResolveActor(), body.Reason),
                cancellationToken)));
        }
        catch (CertificateValidationException exception)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "No fue posible revocar el certificado", exception.Message);
        }
    }

    [HttpDelete("versions/{id:int}")]
    [Authorize(Policy = P1Policies.CertificatesRevoke)]
    public async Task<ActionResult<DeleteCertificateVersionResultDto>> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _deletionService.DeleteVersionAsync(
                new DeleteCertificateVersionRequest(id, ResolveActor()),
                cancellationToken));
        }
        catch (CertificateConflictException exception)
        {
            return FunctionalProblem(StatusCodes.Status409Conflict, "No es posible eliminar el certificado", exception.Message);
        }
        catch (CertificateValidationException exception)
        {
            return FunctionalProblem(StatusCodes.Status404NotFound, "Certificado no encontrado", exception.Message);
        }
    }

    [HttpPost("versions/{id:int}/validate")]
    [Authorize(Policy = P1Policies.CertificatesValidate)]
    public async Task<ActionResult<CertificateValidationResultDto>> ValidateAsync(
        int id,
        CancellationToken cancellationToken)
        => Ok(await _validationService.ValidateForActivationAsync(id, cancellationToken));

    [HttpGet("audit")]
    [Authorize(Policy = P1Policies.CertificatesAudit)]
    public async Task<ActionResult<IEnumerable<CertificateAuditDto>>> AuditAsync(
        CancellationToken cancellationToken)
        => Ok(await _auditService.ListLoadAuditsAsync(cancellationToken));

    private ActionResult? ValidateManagedRequest(ManagedCertificateApiRequest request)
    {
        if (request.Purpose is not (CertificatePurpose.CfaSigningAndDecryption or CertificatePurpose.ClearingHouseValidation))
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Uso requerido", "Selecciona el uso del certificado.");
        }
        if (request.Purpose == CertificatePurpose.ClearingHouseValidation && !request.ClearingHouseId.HasValue)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Cámara requerida", "Selecciona la cámara compensadora propietaria del certificado.");
        }
        if (request.File is null || request.File.Length == 0)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Archivo requerido", "Selecciona un archivo para continuar.");
        }
        if (request.File.Length > MaximumCertificateSize)
        {
            return FunctionalProblem(StatusCodes.Status413PayloadTooLarge, "Archivo demasiado grande", "El archivo supera el máximo de 10 MB.");
        }

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        var validExtension = request.Purpose == CertificatePurpose.CfaSigningAndDecryption
            ? extension is ".pfx" or ".p12"
            : extension is ".cer" or ".crt" or ".pem";
        if (!validExtension)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Formato no permitido", "El certificado no es compatible con el uso seleccionado.");
        }
        if (request.Purpose == CertificatePurpose.CfaSigningAndDecryption && string.IsNullOrEmpty(request.Password))
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Contraseña requerida", "Ingresa la contraseña del certificado.");
        }

        return null;
    }

    private ActionResult? ValidateLegacyUploadRequest(
        UploadPublicCertificateApiRequest request,
        IReadOnlyCollection<string> allowedExtensions)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Archivo requerido", "Selecciona un archivo para continuar.");
        }
        if (request.File.Length > MaximumCertificateSize)
        {
            return FunctionalProblem(StatusCodes.Status413PayloadTooLarge, "Archivo demasiado grande", "El archivo supera el máximo de 10 MB.");
        }
        if (!allowedExtensions.Contains(Path.GetExtension(request.File.FileName).ToLowerInvariant()))
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Formato no permitido", $"Formatos permitidos: {string.Join(", ", allowedExtensions)}.");
        }
        if (string.IsNullOrWhiteSpace(request.Code)
            || request.Code.Length > 120
            || string.IsNullOrWhiteSpace(request.DisplayName)
            || request.DisplayName.Length > 200)
        {
            return FunctionalProblem(StatusCodes.Status400BadRequest, "Información inválida", "El código y el nombre son obligatorios.");
        }
        return null;
    }

    private ObjectResult FunctionalProblem(int status, string title, string detail)
        => Problem(
            statusCode: status,
            title: title,
            detail: detail);

    private static async Task<byte[]> ReadFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    private static CertificateVersionApiDto ToApiDto(CertificateVersionDto dto)
        => new()
        {
            Id = dto.Id,
            Code = dto.Code,
            DisplayName = dto.DisplayName,
            FileName = dto.FileName,
            FinancialInstitutionId = dto.FinancialInstitutionId,
            FinancialInstitutionName = dto.FinancialInstitutionName,
            ClearingHouseId = dto.ClearingHouseId,
            ClearingHouseName = dto.ClearingHouseName,
            Environment = dto.Environment,
            Purpose = dto.Purpose,
            HolderType = dto.HolderType,
            Status = dto.Status,
            FunctionalStatus = dto.FunctionalStatus,
            DaysRemaining = dto.DaysRemaining,
            VersionNumber = dto.VersionNumber,
            Subject = dto.Subject,
            Issuer = dto.Issuer,
            SerialNumber = dto.SerialNumber,
            Thumbprint = dto.Thumbprint,
            FingerprintSha256 = dto.FingerprintSha256,
            NotBefore = dto.NotBefore,
            NotAfter = dto.NotAfter,
            HasPrivateKey = dto.HasPrivateKey,
            KeyAlgorithm = dto.KeyAlgorithm,
            KeySize = dto.KeySize,
            SignatureAlgorithm = dto.SignatureAlgorithm,
            SecretRefMasked = MaskSecret(dto.SecretRef),
            UploadedAtUtc = dto.UploadedAtUtc,
            UploadedBy = dto.UploadedBy,
            ActivatedAtUtc = dto.ActivatedAtUtc,
            RevokedAtUtc = dto.RevokedAtUtc,
            RevocationReason = dto.RevocationReason,
            RevokedBy = dto.RevokedBy,
            CanDelete = dto.CanDelete
        };

    private static string? MaskSecret(string? secretRef)
        => string.IsNullOrWhiteSpace(secretRef)
            ? null
            : secretRef.Length <= 4
                ? "****"
                : $"****{secretRef[^4..]}";

    private string ResolveActor()
        => User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
           ?? User.FindFirstValue(ClaimTypes.Name)
           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
           ?? "api";

    public sealed class ManagedCertificateApiRequest
    {
        public CertificatePurpose Purpose { get; set; }
        public int? ClearingHouseId { get; set; }
        public string? Password { get; set; }
        public IFormFile? File { get; set; }
    }

    public class UploadPublicCertificateApiRequest
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int ClearingHouseId { get; set; }
        public CertificateEnvironment Environment { get; set; }
        public CertificatePurpose Purpose { get; set; }
        public CertificateHolderType HolderType { get; set; }
        public IFormFile? File { get; set; }
    }

    public sealed class UploadPrivateCertificateApiRequest : UploadPublicCertificateApiRequest
    {
        public string Password { get; set; } = string.Empty;
        public CertificateStorageMode StorageMode { get; set; } = CertificateStorageMode.DatabaseEncrypted;
        public string? SecretRef { get; set; }
    }

    public sealed class RevokeVersionBody
    {
        public string? Reason { get; set; }
    }

    public sealed class CertificateVersionApiDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int? FinancialInstitutionId { get; set; }
        public string? FinancialInstitutionName { get; set; }
        public int? ClearingHouseId { get; set; }
        public string? ClearingHouseName { get; set; }
        public CertificateEnvironment Environment { get; set; }
        public CertificatePurpose Purpose { get; set; }
        public CertificateHolderType HolderType { get; set; }
        public CertificateStatus Status { get; set; }
        public CertificateFunctionalStatus FunctionalStatus { get; set; }
        public int? DaysRemaining { get; set; }
        public int VersionNumber { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Thumbprint { get; set; } = string.Empty;
        public string FingerprintSha256 { get; set; } = string.Empty;
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        public bool HasPrivateKey { get; set; }
        public string KeyAlgorithm { get; set; } = string.Empty;
        public int KeySize { get; set; }
        public string SignatureAlgorithm { get; set; } = string.Empty;
        public string? SecretRefMasked { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime? ActivatedAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string? RevocationReason { get; set; }
        public string? RevokedBy { get; set; }
        public bool CanDelete { get; set; }
    }
}
