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
    public async Task<ActionResult<IReadOnlyList<ManagedDigitalEnvelopeCertificateDto>>> Certificates(
        CancellationToken cancellationToken)
        => Ok(await _service.ListUsableCertificatesAsync(cancellationToken));

    [EndpointSummary("Cifrar archivo mediante sobre digital ACH V32")]
    [EndpointDescription("Recibe multipart/form-data con certificateVersionId y file, cifra en servidor y devuelve el archivo binario con .ENV agregado al nombre completo original.")]
    [HttpPost("encrypt")]
    [Authorize(Policy = P1Policies.DigitalEnvelopeEncrypt)]
    [Consumes("multipart/form-data")]
    [Produces("application/octet-stream", "application/problem+json")]
    [RequestSizeLimit(MaximumFileSize)]
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
                    ResolveActor()),
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
    [EndpointDescription("Recibe multipart/form-data con certificateVersionId y file .ENV, descifra exclusivamente en servidor y devuelve el archivo binario eliminando solo la última extensión .ENV.")]
    [HttpPost("decrypt")]
    [Authorize(Policy = P1Policies.DigitalEnvelopeDecrypt)]
    [Consumes("multipart/form-data")]
    [Produces("application/octet-stream", "application/problem+json")]
    [RequestSizeLimit(MaximumFileSize)]
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
                    ResolveActor()),
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
        if (request.CertificateVersionId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Certificado requerido",
                detail: "Selecciona una versión de certificado válida.",
                extensions: ErrorExtensions("CERTIFICATE_REQUIRED"));
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
            "CERTIFICATE_NOT_FOUND" => StatusCodes.Status404NotFound,
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
            ["errorCode"] = errorCode,
            ["traceId"] = HttpContext.TraceIdentifier
        };

    private static async Task<byte[]> ReadFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    private string ResolveActor()
        => string.IsNullOrWhiteSpace(User.Identity?.Name) ? "api" : User.Identity.Name;

    public sealed class DigitalEnvelopeFileRequest
    {
        public int CertificateVersionId { get; set; }
        public IFormFile? File { get; set; }
    }
}
