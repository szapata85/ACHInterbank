using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-security/certificates/management")]
[Authorize]
public class CertificateManagementController : ControllerBase
{
    private readonly ICertificateLoadService _loadService;
    private readonly ICertificateCatalogService _catalogService;
    private readonly ICertificateActivationService _activationService;
    private readonly ICertificateValidationService _validationService;
    private readonly ICertificateAuditService _auditService;

    public CertificateManagementController(
        ICertificateLoadService loadService,
        ICertificateCatalogService catalogService,
        ICertificateActivationService activationService,
        ICertificateValidationService validationService,
        ICertificateAuditService auditService)
    {
        _loadService = loadService;
        _catalogService = catalogService;
        _activationService = activationService;
        _validationService = validationService;
        _auditService = auditService;
    }

    [HttpPost("public")]
    [Authorize(Policy = FineGrainedPermissions.CanManageCertificates)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<ActionResult<CertificateVersionApiDto>> UploadPublicAsync([FromForm] UploadPublicCertificateApiRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0) return BadRequest("Archivo requerido.");

        await using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, cancellationToken);

        var dto = await _loadService.LoadPublicCertificateAsync(new LoadPublicCertificateRequest(
            request.Code,
            request.DisplayName,
            request.ClearingHouseId,
            request.Environment,
            request.Purpose,
            request.HolderType,
            ms.ToArray(),
            User?.Identity?.Name ?? "api"), cancellationToken);

        return Ok(ToApiDto(dto));
    }

    [HttpPost("private")]
    [Authorize(Policy = FineGrainedPermissions.CanManageCertificates)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<ActionResult<CertificateVersionApiDto>> UploadPrivateAsync([FromForm] UploadPrivateCertificateApiRequest request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0) return BadRequest("Archivo requerido.");

        await using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, cancellationToken);

        var dto = await _loadService.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
            request.Code,
            request.DisplayName,
            request.ClearingHouseId,
            request.Environment,
            request.Purpose,
            request.HolderType,
            ms.ToArray(),
            request.Password,
            User?.Identity?.Name ?? "api",
            request.StorageMode,
            request.SecretRef), cancellationToken);

        return Ok(ToApiDto(dto));
    }

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IEnumerable<CertificateVersionApiDto>>> ListAsync([FromQuery] int? clearingHouseId, [FromQuery] CertificateEnvironment? environment, [FromQuery] CertificatePurpose? purpose, [FromQuery] CertificateHolderType? holderType, [FromQuery] CertificateStatus? status, CancellationToken cancellationToken)
    {
        var items = await _catalogService.GetCertificatesAsync(new CertificateFilterDto(clearingHouseId, environment, purpose, holderType, status), cancellationToken);
        return Ok(items.Select(ToApiDto));
    }

    [HttpGet("{id:int}/versions")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IEnumerable<CertificateVersionApiDto>>> ListVersionsAsync(int id, CancellationToken cancellationToken)
    {
        var items = await _catalogService.GetVersionsAsync(id, cancellationToken);
        return Ok(items.Select(ToApiDto));
    }

    [HttpPost("versions/{id:int}/activate")]
    [Authorize(Policy = FineGrainedPermissions.CanManageCertificates)]
    public async Task<ActionResult<CertificateVersionApiDto>> ActivateAsync(int id, CancellationToken cancellationToken)
    {
        var dto = await _activationService.ActivateVersionAsync(new ActivateCertificateVersionRequest(id, User?.Identity?.Name ?? "api"), cancellationToken);
        return Ok(ToApiDto(dto));
    }

    [HttpPost("versions/{id:int}/revoke")]
    [Authorize(Policy = FineGrainedPermissions.CanManageCertificates)]
    public async Task<ActionResult<CertificateVersionApiDto>> RevokeAsync(int id, [FromBody] RevokeVersionBody body, CancellationToken cancellationToken)
    {
        var dto = await _activationService.RevokeVersionAsync(new RevokeCertificateVersionRequest(id, User?.Identity?.Name ?? "api", body.Reason ?? "Revoked by API"), cancellationToken);
        return Ok(ToApiDto(dto));
    }

    [HttpPost("versions/{id:int}/validate")]
    [Authorize(Policy = FineGrainedPermissions.CanManageCertificates)]
    public async Task<ActionResult<CertificateValidationResultDto>> ValidateAsync(int id, CancellationToken cancellationToken)
    {
        return Ok(await _validationService.ValidateForActivationAsync(id, cancellationToken));
    }

    [HttpGet("audit")]
    [Authorize(Policy = FineGrainedPermissions.CanViewNachaSecurityAudit)]
    public async Task<ActionResult<IEnumerable<CertificateAuditDto>>> AuditAsync(CancellationToken cancellationToken)
    {
        return Ok(await _auditService.ListLoadAuditsAsync(cancellationToken));
    }

    private static CertificateVersionApiDto ToApiDto(CertificateVersionDto dto)
        => new()
        {
            Id = dto.Id,
            Code = dto.Code,
            DisplayName = dto.DisplayName,
            ClearingHouseId = dto.ClearingHouseId,
            Environment = dto.Environment,
            Purpose = dto.Purpose,
            HolderType = dto.HolderType,
            Status = dto.Status,
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
            RevokedAtUtc = dto.RevokedAtUtc
        };

    private static string? MaskSecret(string? secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef)) return null;
        return secretRef.Length <= 4 ? "****" : $"****{secretRef[^4..]}";
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
        public CertificateStorageMode StorageMode { get; set; } = CertificateStorageMode.ExternalSecretReference;
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
        public int ClearingHouseId { get; set; }
        public CertificateEnvironment Environment { get; set; }
        public CertificatePurpose Purpose { get; set; }
        public CertificateHolderType HolderType { get; set; }
        public CertificateStatus Status { get; set; }
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
    }
}
