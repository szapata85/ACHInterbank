using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Cfa.ACHInterbank.Api.OpenApi;
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

    /// <summary>Valida un certificado administrado sin persistirlo y devuelve los metadatos obtenidos por el servicio.</summary>
    /// <param name="request">Uso previsto, cámara opcional, contraseña opcional y archivo del certificado.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Previsualizar certificado administrado")]
    [EndpointDescription("Valida la solicitud y el archivo antes de invocar la previsualización del servicio. El uso debe ser CFA signing/decryption o validación de cámara; para validación de cámara se exige clearingHouseId. Requiere archivo no vacío de hasta 10 MB y una extensión compatible con el uso; para el certificado CFA exige contraseña. Devuelve 400 para validaciones funcionales y 413 cuando el archivo supera el máximo permitido.")]
    [HttpPost("managed/preview")]
    [Authorize(Policy = P1Policies.CertificatesUploadPublic)]
    [RequestSizeLimit(MaximumCertificateSize)]
    [ProducesResponseType(typeof(CertificatePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Previsualización del certificado validado sin persistirlo.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud, el formato del archivo, la contraseña o la validación del certificado no cumplen las reglas ejecutadas por el controller o el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "El archivo informado supera el máximo de 10 MB aceptado por la operación.")]
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

    /// <summary>Guarda un certificado administrado después de validar su uso, archivo y datos asociados.</summary>
    /// <param name="request">Uso previsto, cámara opcional, contraseña opcional y archivo del certificado.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Guardar certificado administrado")]
    [EndpointDescription("Valida la misma solicitud aceptada por la previsualización y persiste el certificado mediante el servicio de carga. Devuelve la versión registrada; una validación funcional produce 400, un certificado duplicado produce 409 y un archivo mayor de 10 MB produce 413.")]
    [HttpPost("managed")]
    [Authorize(Policy = P1Policies.CertificatesRegisterPrivate)]
    [RequestSizeLimit(MaximumCertificateSize)]
    [ProducesResponseType(typeof(CertificateVersionApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versión del certificado administrado registrada por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud o el certificado no superan las validaciones explícitas del controller o del servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "El servicio detectó un conflicto por certificado duplicado.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "El archivo informado supera el máximo de 10 MB aceptado por la operación.")]
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

    /// <summary>Carga un certificado público para la cámara y atributos indicados en el formulario.</summary>
    /// <param name="request">Código, nombre, cámara, ambiente, uso, titular y archivo público.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Cargar certificado público")]
    [EndpointDescription("Acepta archivos públicos CER, CRT o PEM de hasta 10 MB. El controller exige archivo, código y nombre válidos antes de invocar el servicio. Devuelve la versión registrada; las validaciones funcionales producen 400, un duplicado produce 409 y un archivo demasiado grande produce 413.")]
    [HttpPost("public")]
    [Authorize(Policy = P1Policies.CertificatesUploadPublic)]
    [RequestSizeLimit(MaximumCertificateSize)]
    [ProducesResponseType(typeof(CertificateVersionApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versión del certificado público registrada por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El formulario, la extensión o el contenido del certificado no superan las validaciones ejecutadas por el controller o el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "El servicio detectó un conflicto por certificado duplicado.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "El archivo informado supera el máximo de 10 MB aceptado por la operación.")]
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

    /// <summary>Registra un certificado privado desde un archivo PKCS#12 protegido por contraseña.</summary>
    /// <param name="request">Datos de identificación, cámara, ambiente, uso, titular, archivo, contraseña y modo de almacenamiento.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Registrar certificado privado")]
    [EndpointDescription("Acepta archivos PFX o P12 de hasta 10 MB. Además de las validaciones comunes del formulario, exige contraseña y StorageMode DatabaseEncrypted antes de invocar el servicio. Devuelve la versión registrada; las validaciones funcionales producen 400, un duplicado produce 409 y un archivo demasiado grande produce 413.")]
    [HttpPost("private")]
    [Authorize(Policy = P1Policies.CertificatesRegisterPrivate)]
    [RequestSizeLimit(MaximumCertificateSize)]
    [ProducesResponseType(typeof(CertificateVersionApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versión del certificado privado registrada por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El formulario, la contraseña, el modo de almacenamiento o el certificado no superan las validaciones ejecutadas por el controller o el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "El servicio detectó un conflicto por certificado duplicado.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "El archivo informado supera el máximo de 10 MB aceptado por la operación.")]
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

    /// <summary>Consulta el catálogo de certificados aplicando los filtros opcionales informados.</summary>
    /// <param name="clearingHouseId">Identificador opcional de la cámara por la que se filtra el catálogo.</param>
    /// <param name="environment">Ambiente opcional del certificado.</param>
    /// <param name="purpose">Uso opcional del certificado.</param>
    /// <param name="holderType">Tipo opcional de titular.</param>
    /// <param name="status">Estado opcional del certificado.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar catálogo de certificados")]
    [EndpointDescription("Consulta el servicio de catálogo con filtros opcionales por cámara, ambiente, uso, titular y estado. Devuelve las versiones que cumplen el filtro transformadas al contrato de API; la operación no modifica certificados.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    [ProducesResponseType(typeof(IEnumerable<CertificateVersionApiDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de versiones de certificado que cumplen los filtros solicitados.")]
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

    /// <summary>Lista las versiones asociadas al certificado identificado por id.</summary>
    /// <param name="id">Identificador del certificado cuyo historial de versiones se consulta.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar versiones de certificado")]
    [EndpointDescription("Solicita al catálogo las versiones asociadas al identificador indicado y devuelve la colección transformada al contrato de API. El controller no introduce una respuesta 404 cuando la colección está vacía.")]
    [HttpGet("{id:int}/versions")]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    [ProducesResponseType(typeof(IEnumerable<CertificateVersionApiDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de versiones asociadas al identificador de certificado solicitado.")]
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

    /// <summary>Formulario usado para previsualizar o guardar un certificado administrado.</summary>
    public sealed class ManagedCertificateApiRequest
    {
        /// <summary>Uso funcional del certificado seleccionado por la solicitud.</summary>
        public CertificatePurpose Purpose { get; set; }

        /// <summary>Identificador de la cámara; es obligatorio cuando el uso es ClearingHouseValidation.</summary>
        public int? ClearingHouseId { get; set; }

        /// <summary>Contraseña del archivo cuando el uso requiere abrir material privado.</summary>
        public string? Password { get; set; }

        /// <summary>Archivo del certificado enviado como multipart/form-data.</summary>
        public IFormFile? File { get; set; }
    }

    /// <summary>Formulario de registro de un certificado público.</summary>
    public class UploadPublicCertificateApiRequest
    {
        /// <summary>Código funcional asignado al certificado.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Nombre visible asignado al certificado.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Identificador de la cámara asociada al certificado.</summary>
        public int ClearingHouseId { get; set; }

        /// <summary>Ambiente funcional asociado al certificado.</summary>
        public CertificateEnvironment Environment { get; set; }

        /// <summary>Uso funcional asignado al certificado.</summary>
        public CertificatePurpose Purpose { get; set; }

        /// <summary>Tipo de titular del certificado.</summary>
        public CertificateHolderType HolderType { get; set; }

        /// <summary>Archivo público enviado como multipart/form-data.</summary>
        public IFormFile? File { get; set; }
    }

    /// <summary>Formulario de registro de un certificado privado.</summary>
    public sealed class UploadPrivateCertificateApiRequest : UploadPublicCertificateApiRequest
    {
        /// <summary>Contraseña usada para abrir el archivo PKCS#12.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Modo de almacenamiento solicitado; el controller admite DatabaseEncrypted en esta operación.</summary>
        public CertificateStorageMode StorageMode { get; set; } = CertificateStorageMode.DatabaseEncrypted;

        /// <summary>Referencia externa opcional incluida en el contrato heredado del registro privado.</summary>
        public string? SecretRef { get; set; }
    }

    public sealed class RevokeVersionBody
    {
        public string? Reason { get; set; }
    }

    /// <summary>Representación API de una versión de certificado administrado.</summary>
    public sealed class CertificateVersionApiDto
    {
        /// <summary>Identificador de la versión de certificado.</summary>
        public int Id { get; set; }

        /// <summary>Código funcional del certificado.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Nombre visible del certificado.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Nombre simple del archivo asociado.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>Identificador de la entidad financiera asociada cuando aplica.</summary>
        public int? FinancialInstitutionId { get; set; }

        /// <summary>Nombre de la entidad financiera asociada cuando aplica.</summary>
        public string? FinancialInstitutionName { get; set; }

        /// <summary>Identificador de la cámara asociada cuando aplica.</summary>
        public int? ClearingHouseId { get; set; }

        /// <summary>Nombre de la cámara asociada cuando aplica.</summary>
        public string? ClearingHouseName { get; set; }

        /// <summary>Ambiente funcional de la versión.</summary>
        public CertificateEnvironment Environment { get; set; }

        /// <summary>Uso funcional de la versión.</summary>
        public CertificatePurpose Purpose { get; set; }

        /// <summary>Tipo de titular del certificado.</summary>
        public CertificateHolderType HolderType { get; set; }

        /// <summary>Estado persistido de la versión.</summary>
        public CertificateStatus Status { get; set; }

        /// <summary>Estado funcional calculado por el servicio.</summary>
        public CertificateFunctionalStatus FunctionalStatus { get; set; }

        /// <summary>Días restantes calculados por el servicio cuando aplica.</summary>
        public int? DaysRemaining { get; set; }

        /// <summary>Número de versión del certificado.</summary>
        public int VersionNumber { get; set; }

        /// <summary>Subject extraído del certificado.</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>Issuer extraído del certificado.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>Número de serie extraído del certificado.</summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>Thumbprint expuesto por el servicio.</summary>
        public string Thumbprint { get; set; } = string.Empty;

        /// <summary>Huella SHA-256 expuesta por el servicio.</summary>
        public string FingerprintSha256 { get; set; } = string.Empty;

        /// <summary>Fecha inicial de vigencia contenida en el certificado.</summary>
        public DateTime NotBefore { get; set; }

        /// <summary>Fecha final de vigencia contenida en el certificado.</summary>
        public DateTime NotAfter { get; set; }

        /// <summary>Indica si la versión contiene material de llave privada.</summary>
        public bool HasPrivateKey { get; set; }

        /// <summary>Algoritmo de llave pública reportado por el servicio.</summary>
        public string KeyAlgorithm { get; set; } = string.Empty;

        /// <summary>Tamaño de la llave reportado por el servicio.</summary>
        public int KeySize { get; set; }

        /// <summary>Algoritmo de firma reportado por el certificado.</summary>
        public string SignatureAlgorithm { get; set; } = string.Empty;

        /// <summary>Referencia de secreto enmascarada cuando existe.</summary>
        public string? SecretRefMasked { get; set; }

        /// <summary>Fecha y hora UTC de registro de la versión.</summary>
        public DateTime UploadedAtUtc { get; set; }

        /// <summary>Actor que registró la versión.</summary>
        public string UploadedBy { get; set; } = string.Empty;

        /// <summary>Fecha y hora UTC de activación cuando existe.</summary>
        public DateTime? ActivatedAtUtc { get; set; }

        /// <summary>Fecha y hora UTC de revocación cuando existe.</summary>
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>Motivo de revocación cuando existe.</summary>
        public string? RevocationReason { get; set; }

        /// <summary>Actor que revocó la versión cuando existe.</summary>
        public string? RevokedBy { get; set; }

        /// <summary>Indica si el servicio permite eliminar la versión según su estado y uso.</summary>
        public bool CanDelete { get; set; }
    }
}
