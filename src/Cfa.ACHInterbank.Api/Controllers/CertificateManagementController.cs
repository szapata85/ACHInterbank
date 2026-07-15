using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-security/certificates/management")]
[Route("api/nacha-security/certificates/management")]
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

    [EndpointSummary("Cargar certificado público")]
    [EndpointDescription("Qué hace: registra una nueva versión de certificado público para uso operativo. Cuándo se usa: en alta/rotación de certificados de intercambio. Perfil consumidor: seguridad y operación ACH. Permiso requerido: FineGrainedPermissions.CanManageCertificates. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: carga de certificado incorrecto rompe validación de firmas/cifrado. Errores esperados: 400 archivo requerido o metadatos inválidos; 401/403. Relación ACH/CENIT/NACHA-M: base de confianza para operaciones NACHA-M seguras. Precauciones para desarrollo u operación: validar emisor, vigencia y ambiente antes de publicar.")]
    [HttpPost("public")]
    [Authorize(Policy = P1Policies.CertificatesUploadPublic)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<CertificateVersionApiDto>> UploadPublicAsync([FromForm] UploadPublicCertificateApiRequest request, CancellationToken cancellationToken)
    {
        var requestError = ValidateUploadRequest(request, [".cer", ".crt", ".pem"]);
        if (requestError is not null) return requestError;

        await using var ms = new MemoryStream();
        await request.File!.CopyToAsync(ms, cancellationToken);
        var rawCertificate = ms.ToArray();

        try
        {
            var dto = await _loadService.LoadPublicCertificateAsync(new LoadPublicCertificateRequest(
                request.Code.Trim(),
                request.DisplayName.Trim(),
                request.ClearingHouseId,
                request.Environment,
                request.Purpose,
                request.HolderType,
                rawCertificate,
                ResolveActor(),
                Path.GetFileName(request.File.FileName)), cancellationToken);

            return Ok(ToApiDto(dto));
        }
        catch (CertificateConflictException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Certificado duplicado", detail: ex.Message);
        }
        catch (CertificateValidationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Certificado público inválido", detail: ex.Message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawCertificate);
        }
    }

    [EndpointSummary("Registrar certificado privado")]
    [EndpointDescription("Qué hace: registra material privado o referencia segura para operaciones criptográficas. Cuándo se usa: durante aprovisionamiento/rotación de llaves. Perfil consumidor: seguridad bancaria. Permiso requerido: FineGrainedPermissions.CanManageCertificates. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: manejo inseguro de clave privada compromete confidencialidad. Errores esperados: 400 archivo/contraseña/secreto inválido; 401/403. Relación ACH/CENIT/NACHA-M: habilita firmado/cifrado de artefactos NACHA-M. Precauciones para desarrollo u operación: usar almacenamiento seguro y mínimo privilegio.")]
    [HttpPost("private")]
    [Authorize(Policy = P1Policies.CertificatesRegisterPrivate)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<CertificateVersionApiDto>> UploadPrivateAsync([FromForm] UploadPrivateCertificateApiRequest request, CancellationToken cancellationToken)
    {
        var requestError = ValidateUploadRequest(request, [".pfx", ".p12"]);
        if (requestError is not null) return requestError;
        if (string.IsNullOrEmpty(request.Password))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Contraseña requerida", detail: "Ingresa la contraseña del archivo PKCS#12.");
        }
        if (request.StorageMode != CertificateStorageMode.DatabaseEncrypted)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Almacenamiento no permitido", detail: "El modo de almacenamiento privado indicado no está permitido.");
        }

        await using var ms = new MemoryStream();
        await request.File!.CopyToAsync(ms, cancellationToken);
        var rawPkcs12 = ms.ToArray();

        try
        {
            var dto = await _loadService.RegisterPrivateCertificateAsync(new RegisterPrivateCertificateRequest(
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
                Path.GetFileName(request.File.FileName)), cancellationToken);

            return Ok(ToApiDto(dto));
        }
        catch (CertificateConflictException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Certificado duplicado", detail: ex.Message);
        }
        catch (CertificateValidationException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Certificado privado inválido", detail: ex.Message);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawPkcs12);
        }
    }

    [EndpointSummary("Consulta de certificados por filtros de gestión")]
    [EndpointDescription("Qué hace: lista certificados gestionados por cámara, ambiente, propósito, titularidad y estado. Cuándo se usa: en gobierno de certificados, diagnóstico de operación y validación previa a generación/cifrado. Perfil consumidor: seguridad bancaria, operación ACH y auditoría. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas de acceso administrativo. Riesgos operativos: aplicar filtros incompletos puede ocultar versión activa o revocada. Errores esperados: 400 por filtros inválidos; 401/403 por autorización. Relación ACH/CENIT/NACHA-M: administra el inventario criptográfico que respalda intercambio seguro NACHA-M. Precauciones para desarrollo u operación: verificar siempre vigencia, estado y ambiente antes de promover cambios operativos.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    public async Task<ActionResult<IEnumerable<CertificateVersionApiDto>>> ListAsync([FromQuery] int? clearingHouseId, [FromQuery] CertificateEnvironment? environment, [FromQuery] CertificatePurpose? purpose, [FromQuery] CertificateHolderType? holderType, [FromQuery] CertificateStatus? status, CancellationToken cancellationToken)
    {
        var items = await _catalogService.GetCertificatesAsync(new CertificateFilterDto(clearingHouseId, environment, purpose, holderType, status), cancellationToken);
        return Ok(items.Select(ToApiDto));
    }

    [EndpointSummary("Versiones de un certificado")]
    [EndpointDescription("Qué hace: muestra historial/versiones de un certificado lógico. Cuándo se usa: en auditoría de rotación o investigación de incidentes. Perfil consumidor: seguridad y auditoría. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí. Riesgos operativos: ignorar historial puede causar activación de versión obsoleta. Errores esperados: 404 id inexistente; 401/403. Relación ACH/CENIT/NACHA-M: trazabilidad de gestión criptográfica NACHA-M. Precauciones para desarrollo u operación: revisar vigencia y estado antes de activar.")]
    [HttpGet("{id:int}/versions")]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    public async Task<ActionResult<IEnumerable<CertificateVersionApiDto>>> ListVersionsAsync(int id, CancellationToken cancellationToken)
    {
        var items = await _catalogService.GetVersionsAsync(id, cancellationToken);
        return Ok(items.Select(ToApiDto));
    }

    [EndpointSummary("Activar versión de certificado")]
    [EndpointDescription("Qué hace: activa versión específica para uso productivo. Cuándo se usa: en ventanas de cambio aprobadas. Perfil consumidor: seguridad operativa. Permiso requerido: FineGrainedPermissions.CanManageCertificates. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: activar versión errónea interrumpe firmas/cifrado. Errores esperados: 400/409 por validación de activación; 401/403. Relación ACH/CENIT/NACHA-M: impacta disponibilidad de operaciones NACHA-M seguras. Precauciones para desarrollo u operación: ejecutar con plan de reversa y validación previa.")]
    [HttpPost("versions/{id:int}/activate")]
    [Authorize(Policy = P1Policies.CertificatesActivate)]
    public async Task<ActionResult<CertificateVersionApiDto>> ActivateAsync(int id, CancellationToken cancellationToken)
    {
        var dto = await _activationService.ActivateVersionAsync(new ActivateCertificateVersionRequest(id, ResolveActor()), cancellationToken);
        return Ok(ToApiDto(dto));
    }

    [EndpointSummary("Revocar versión de certificado")]
    [EndpointDescription("Qué hace: revoca una versión con motivo registrado. Cuándo se usa: ante compromiso, expiración o retiro controlado. Perfil consumidor: seguridad y cumplimiento. Permiso requerido: FineGrainedPermissions.CanManageCertificates. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: revocar sin reemplazo puede detener operación. Errores esperados: 400/409 por estado inválido; 401/403. Relación ACH/CENIT/NACHA-M: control de riesgo criptográfico en intercambio NACHA-M. Precauciones para desarrollo u operación: confirmar versión sustituta antes de revocar.")]
    [HttpPost("versions/{id:int}/revoke")]
    [Authorize(Policy = P1Policies.CertificatesRevoke)]
    public async Task<ActionResult<CertificateVersionApiDto>> RevokeAsync(int id, [FromBody] RevokeVersionBody body, CancellationToken cancellationToken)
    {
        var dto = await _activationService.RevokeVersionAsync(new RevokeCertificateVersionRequest(id, ResolveActor(), body.Reason ?? "Revoked by API"), cancellationToken);
        return Ok(ToApiDto(dto));
    }

    [EndpointSummary("Validar versión para activación")]
    [EndpointDescription("Qué hace: ejecuta validaciones técnicas previas a activar una versión. Cuándo se usa: antes de cambios en productivo. Perfil consumidor: seguridad y operación. Permiso requerido: FineGrainedPermissions.CanManageCertificates. Tipo de operación: modifica información. Genera auditoría: sí, por rastro de validación. Riesgos operativos: omitir validación incrementa riesgo de caída operativa. Errores esperados: 400 por certificado inválido; 401/403. Relación ACH/CENIT/NACHA-M: asegura cumplimiento criptográfico para NACHA-M. Precauciones para desarrollo u operación: no activar si la validación reporta fallas.")]
    [HttpPost("versions/{id:int}/validate")]
    [Authorize(Policy = P1Policies.CertificatesValidate)]
    public async Task<ActionResult<CertificateValidationResultDto>> ValidateAsync(int id, CancellationToken cancellationToken)
    {
        return Ok(await _validationService.ValidateForActivationAsync(id, cancellationToken));
    }

    [EndpointSummary("Auditoría de cargas de certificados")]
    [EndpointDescription("Qué hace: lista eventos de carga y gestión para trazabilidad de cumplimiento. Cuándo se usa: en revisiones regulatorias y forénsicas. Perfil consumidor: auditoría y seguridad. Permiso requerido: FineGrainedPermissions.CanViewNachaSecurityAudit. Tipo de operación: solo consulta. Genera auditoría: sí, fuente primaria de auditoría. Riesgos operativos: no revisar auditoría limita detección de acciones indebidas. Errores esperados: 401/403. Relación ACH/CENIT/NACHA-M: evidencia gobierno de certificados en procesos NACHA-M. Precauciones para desarrollo u operación: preservar integridad de evidencias y control de acceso.")]
    [HttpGet("audit")]
    [Authorize(Policy = P1Policies.CertificatesAudit)]
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
            FileName = dto.FileName,
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

    private string ResolveActor()
        => User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? "api";

    private ActionResult? ValidateUploadRequest(UploadPublicCertificateApiRequest request, IReadOnlyCollection<string> allowedExtensions)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Archivo requerido", detail: "Selecciona un archivo de certificado no vacío.");
        }
        if (request.File.Length > 10 * 1024 * 1024)
        {
            return Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Archivo demasiado grande", detail: "El archivo supera el máximo de 10 MB.");
        }
        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Extensión no permitida", detail: $"Extensiones permitidas: {string.Join(", ", allowedExtensions)}.");
        }
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 120
            || string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 200)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Metadatos inválidos", detail: "Código y nombre son obligatorios y deben respetar sus longitudes máximas.");
        }
        return null;
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
