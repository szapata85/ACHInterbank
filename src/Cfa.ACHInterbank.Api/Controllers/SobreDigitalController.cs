using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/nacha-security/digital-envelope")]
public sealed class SobreDigitalController : ControllerBase
{
    private const long MaximumFileSize = 50L * 1024 * 1024;
    private readonly IManagedDigitalEnvelopeService _service;

    public SobreDigitalController(IManagedDigitalEnvelopeService service)
    {
        _service = service;
    }

    [EndpointSummary("Certificados utilizables para sobre digital")]
    [EndpointDescription("Lista versiones activas y vigentes registradas en DigitalCertificates/DigitalCertificateVersions que pueden participar en cifrado o descifrado.")]
    [HttpGet("certificates")]
    [Authorize(Policy = P1Policies.CertificatesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ManagedDigitalEnvelopeCertificateDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de versiones de certificado activas y vigentes que el servicio considera utilizables para operaciones de sobre digital.")]
    public async Task<ActionResult<IReadOnlyList<ManagedDigitalEnvelopeCertificateDto>>> Certificates(
        CancellationToken cancellationToken)
        => Ok(await _service.ListUsableCertificatesAsync(cancellationToken));

    [EndpointSummary("Cifrar archivo mediante sobre digital ACH V32")]
    [EndpointDescription("Recibe multipart/form-data con certificateVersionId, operationMode, file y clearingHouseId opcional. Tras las validaciones del controller, invoca el servicio de cifrado y devuelve el archivo binario con .ENV agregado al nombre original; en éxito publica X-Cryptographic-Profile. Los errores controlados se devuelven como ProblemDetails.")]
    [HttpPost("encrypt")]
    [Authorize(Policy = P1Policies.DigitalEnvelopeEncrypt)]
    [Consumes("multipart/form-data")]
    [Produces("application/octet-stream", "application/problem+json")]
    [RequestSizeLimit(MaximumFileSize)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo cifrado devuelto como application/octet-stream con nombre de descarga y header X-Cryptographic-Profile informados por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud o la operación criptográfica no cumplen una validación controlada que el controller traduce a solicitud inválida.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No se encontró el certificado requerido para completar la operación de cifrado.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La versión seleccionada no puede utilizarse por estado, vigencia o propósito, o no está disponible el certificado privado de firma requerido para completar el cifrado.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "El archivo supera el máximo de 50 MB aceptado por la operación.")]
    public async Task<IActionResult> Encrypt([FromForm] DigitalEnvelopeFileRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request, requireEnvelopeExtension: false);
        if (validation is not null) return validation;

        var content = await ReadFileAsync(request.File!, cancellationToken);
        try
        {
            var result = await _service.EncryptAsync(
                new ManagedDigitalEnvelopeRequest(
                    request.CertificateVersionId,
                    request.File!.FileName,
                    content,
                    ResolveActor(),
                    request.ClearingHouseId,
                    request.OperationMode),
                cancellationToken);
            Response.Headers["X-Cryptographic-Profile"] = result.CryptographicProfile;
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (ManagedDigitalEnvelopeException ex)
        {
            return ToProblem(ex);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(content);
        }
    }

    [EndpointSummary("Descifrar archivo de sobre digital ACH V32")]
    [EndpointDescription("Recibe multipart/form-data con file .ENV, operationMode y certificateVersionId o clearingHouseId según la selección disponible. Tras las validaciones del controller, invoca el servicio de descifrado y devuelve el archivo recuperado eliminando la última extensión .ENV; en éxito publica X-Cryptographic-Profile. Los errores controlados se devuelven como ProblemDetails.")]
    [HttpPost("decrypt")]
    [Authorize(Policy = P1Policies.DigitalEnvelopeDecrypt)]
    [Consumes("multipart/form-data")]
    [Produces("application/octet-stream", "application/problem+json")]
    [RequestSizeLimit(MaximumFileSize)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo descifrado devuelto como application/octet-stream con nombre de descarga y header X-Cryptographic-Profile informados por el servicio.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud o la operación criptográfica no cumplen una validación controlada que el controller traduce a solicitud inválida.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No se encontró el certificado requerido para completar la operación de descifrado.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La versión requerida no puede utilizarse por estado, vigencia, propósito, disponibilidad de llave privada o correspondencia con el sobre recibido.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "El archivo supera el máximo de 50 MB aceptado por la operación.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "El sobre digital, su contenido firmado o la validación criptográfica produce un error controlado que el controller clasifica como no procesable.")]
    public async Task<IActionResult> Decrypt([FromForm] DigitalEnvelopeFileRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateRequest(request, requireEnvelopeExtension: true);
        if (validation is not null) return validation;

        var content = await ReadFileAsync(request.File!, cancellationToken);
        try
        {
            var result = await _service.DecryptAsync(
                new ManagedDigitalEnvelopeRequest(
                    request.CertificateVersionId,
                    request.File!.FileName,
                    content,
                    ResolveActor(),
                    request.ClearingHouseId,
                    request.OperationMode),
                cancellationToken);
            Response.Headers["X-Cryptographic-Profile"] = result.CryptographicProfile;
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (ManagedDigitalEnvelopeException ex)
        {
            return ToProblem(ex);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(content);
        }
    }

    [HttpPost("testRSA")]
    [Authorize(Policy = P1Policies.DigitalEnvelopeTest)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public void testRSA([FromServices] IRsaKeyProvider rsaKeyService)
    {
        using var certificate = rsaKeyService.ObtenerCertificate("CertCrypt");
    }

    private ActionResult? ValidateRequest(DigitalEnvelopeFileRequest request, bool requireEnvelopeExtension)
    {
        if (request.CertificateVersionId <= 0
            && (!requireEnvelopeExtension || request.ClearingHouseId is not > 0))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Certificado requerido",
                detail: "Selecciona una versión de certificado válida.",
                extensions: ErrorExtensions("CERTIFICATE_REQUIRED"));
        }
        if (!string.Equals(request.OperationMode, "LIVE", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Modo LIVE requerido",
                detail: "Selecciona el Modo LIVE antes de procesar el archivo.",
                extensions: ErrorExtensions("LIVE_MODE_REQUIRED"));
        }
        if (request.File is null || request.File.Length == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Archivo requerido",
                detail: "Selecciona un archivo no vacío.",
                extensions: ErrorExtensions("FILE_EMPTY"));
        }
        if (request.File.Length > MaximumFileSize)
        {
            return Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Archivo demasiado grande",
                detail: "El archivo supera el máximo permitido de 50 MB.",
                extensions: ErrorExtensions("FILE_TOO_LARGE"));
        }
        if (requireEnvelopeExtension && !request.File.FileName.EndsWith(".ENV", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Extensión inválida",
                detail: "El archivo para descifrar debe terminar en .ENV.",
                extensions: ErrorExtensions("ENVELOPE_EXTENSION_REQUIRED"));
        }
        return null;
    }

    private IActionResult ToProblem(ManagedDigitalEnvelopeException exception)
    {
        var status = exception.ErrorCode switch
        {
            "CERTIFICATE_NOT_FOUND" or "CFA_CERTIFICATE_NOT_FOUND" or "CLEARING_HOUSE_CERTIFICATE_NOT_FOUND"
                => StatusCodes.Status404NotFound,
            "CERTIFICATE_INACTIVE" or "CERTIFICATE_EXPIRED" or "CERTIFICATE_NOT_YET_VALID"
                or "CERTIFICATE_PURPOSE_INVALID" or "CERTIFICATE_PRIVATE_KEY_REQUIRED"
                or "CERTIFICATE_PRIVATE_KEY_UNAVAILABLE" or "CERTIFICATE_MISMATCH"
                or "SIGNING_CERTIFICATE_NOT_FOUND" => StatusCodes.Status409Conflict,
            "FILE_TOO_LARGE" => StatusCodes.Status413PayloadTooLarge,
            "ENVELOPE_INVALID" or "ENVELOPE_INTEGRITY_INVALID" or "ENVELOPE_ALGORITHM_UNSUPPORTED"
                or "SIGNED_CONTENT_INVALID" or "SIGNATURE_VALIDATION_FAILED"
                or "PLAINTEXT_SIZE_INVALID" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            statusCode: status,
            title: "Operación de sobre digital no completada",
            detail: exception.Message,
            extensions: ErrorExtensions(exception.ErrorCode));
    }

    private Dictionary<string, object?> ErrorExtensions(string errorCode)
        => new()
        {
            ["code"] = errorCode,
            ["errorCode"] = errorCode
        };

    private static async Task<byte[]> ReadFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    private string ResolveActor()
        => string.IsNullOrWhiteSpace(User.Identity?.Name) ? "api" : User.Identity.Name;

    /// <summary>Formulario multipart utilizado por las operaciones de cifrado y descifrado de sobre digital.</summary>
    public sealed class DigitalEnvelopeFileRequest
    {
        /// <summary>Identificador de la versión de certificado seleccionada. Para descifrado puede omitirse cuando se informa una cámara válida y el servicio resuelve el certificado aplicable.</summary>
        public int CertificateVersionId { get; set; }

        /// <summary>Identificador opcional de la cámara utilizado como contexto por el servicio de sobre digital.</summary>
        public int? ClearingHouseId { get; set; }

        /// <summary>Modo operacional solicitado. El controller acepta únicamente el valor LIVE sin distinguir mayúsculas de minúsculas.</summary>
        public string OperationMode { get; set; } = string.Empty;

        /// <summary>Archivo multipart a procesar. Debe ser no vacío y no superar 50 MB; para descifrado el nombre debe terminar en .ENV.</summary>
        public IFormFile? File { get; set; }
    }
}
