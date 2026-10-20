using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-security/operations")]
[Authorize]
public class NachaSecurityOperationsController : ControllerBase
{
    private readonly INachaSecurityOperationService _service;

    public NachaSecurityOperationsController(INachaSecurityOperationService service)
    {
        _service = service;
    }

    /// <summary>Solicita la generación de un artefacto NACHA-M sin encapsulado de sobre digital.</summary>
    /// <param name="request">Solicitud que identifica el ciclo que se debe procesar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Generar artefacto NACHA-M sin sobre digital")]
    [EndpointDescription("Recibe el identificador del ciclo y solicita al servicio la generación sin encapsulado de sobre digital. La acción devuelve 200 con DigitalEnvelopeOperationDto tanto cuando la operación termina correctamente como cuando el servicio registra un fallo funcional dentro del propio resultado; el controller no traduce esos fallos a códigos HTTP adicionales.")]
    [HttpPost("nacha/generate")]
    [OpenApiRequestExample(typeof(NachaGenerateRequestExampleProvider))]
    [Authorize(Policy = P1Policies.NachaSecurityGenerateEncrypted)]
    [ProducesResponseType(typeof(DigitalEnvelopeOperationDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado persistido de la operación de generación NACHA-M, incluido su estado, artefacto y error cuando el servicio registró un fallo.")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GeneratePlainAsync([FromBody] NachaGenerateApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.GeneratePlainAsync(
            new NachaGenerateRequest(request.CycleId, false),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Solicita la generación de un artefacto NACHA-M encapsulado por el servicio de sobre digital.</summary>
    /// <param name="request">Solicitud que identifica el ciclo que se debe procesar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Generar artefacto NACHA-M con sobre digital")]
    [EndpointDescription("Recibe el identificador del ciclo y solicita al servicio la generación con encapsulado de sobre digital. La acción devuelve 200 con DigitalEnvelopeOperationDto tanto cuando la operación termina correctamente como cuando el servicio registra un fallo funcional dentro del propio resultado; el controller no traduce esos fallos a códigos HTTP adicionales.")]
    [HttpPost("nacha/generate-encrypted")]
    [OpenApiRequestExample(typeof(NachaGenerateRequestExampleProvider))]
    [Authorize(Policy = P1Policies.NachaSecurityGenerateEncrypted)]
    [ProducesResponseType(typeof(DigitalEnvelopeOperationDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado persistido de la operación de generación NACHA-M con sobre digital, incluido su estado, artefacto y error cuando el servicio registró un fallo.")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GenerateEncryptedAsync([FromBody] NachaGenerateApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateEncryptedAsync(
            new NachaGenerateRequest(request.CycleId, true),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Procesa manualmente un archivo mediante la operación de cifrado del servicio de sobre digital.</summary>
    /// <param name="file">Archivo multipart que se entrega al servicio. Debe existir y contener al menos un byte.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Cifrar manualmente un archivo")]
    [EndpointDescription("Requiere un archivo no vacío, copia sus bytes y los entrega al servicio de cifrado manual. Si el archivo falta o está vacío responde 400 con el body anónimo code/message; RequestSizeLimit limita la solicitud a 50 MB. Una vez invocado el servicio, devuelve 200 con DigitalEnvelopeOperationDto incluso cuando el servicio registra la operación como fallida dentro del resultado.")]
    [HttpPost("envelope/manual-encrypt")]
    [Authorize(Policy = P1Policies.NachaSecurityManualEncrypt)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(DigitalEnvelopeOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado persistido del cifrado manual, incluido su estado y metadata de artefacto.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "No se informó un archivo o el archivo recibido está vacío; el body actual contiene code y message.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "La solicitud supera el límite de 50 MB aplicado por RequestSizeLimit.")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> ManualEncryptAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { code = "FILE_REQUIRED", message = "Archivo requerido." });
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);

        var result = await _service.ManualEncryptAsync(
            new ManualEnvelopeRequest(file.FileName, ms.ToArray()),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Procesa manualmente un archivo mediante la operación de descifrado del servicio de sobre digital.</summary>
    /// <param name="file">Archivo multipart que se entrega al servicio. Debe existir y contener al menos un byte.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descifrar manualmente un archivo")]
    [EndpointDescription("Requiere un archivo no vacío, copia sus bytes y los entrega al servicio de descifrado manual. Si el archivo falta o está vacío responde 400 con el body anónimo code/message; RequestSizeLimit limita la solicitud a 50 MB. Una vez invocado el servicio, devuelve 200 con DigitalEnvelopeOperationDto incluso cuando el servicio registra la operación como fallida dentro del resultado.")]
    [HttpPost("envelope/manual-decrypt")]
    [Authorize(Policy = P1Policies.NachaSecurityManualDecrypt)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [ProducesResponseType(typeof(DigitalEnvelopeOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado persistido del descifrado manual, incluido su estado y metadata de artefacto.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "No se informó un archivo o el archivo recibido está vacío; el body actual contiene code y message.")]
    [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "La solicitud supera el límite de 50 MB aplicado por RequestSizeLimit.")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> ManualDecryptAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { code = "FILE_REQUIRED", message = "Archivo requerido." });
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);

        var result = await _service.ManualDecryptAsync(
            new ManualEnvelopeRequest(file.FileName, ms.ToArray()),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Consulta una operación de seguridad NACHA por su identificador.</summary>
    /// <param name="operationId">Identificador de operación almacenado por el servicio.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar operación de seguridad NACHA")]
    [EndpointDescription("Busca la operación por operationId. Devuelve 404 con el body anónimo code/message/operationId cuando no existe; cuando existe devuelve el DTO persistido. Al consultar una operación exitosa cuyo artefacto ya venció, el servicio proyecta el estado Expired en la respuesta.")]
    [HttpGet("{operationId}")]
    [Authorize(Policy = P1Policies.NachaSecurityRead)]
    [ProducesResponseType(typeof(DigitalEnvelopeOperationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Operación de seguridad NACHA encontrada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una operación con el operationId indicado; el body actual contiene code, message y operationId.")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByOperationIdAsync(operationId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = "OPERATION_NOT_FOUND", message = "Operación no encontrada.", operationId });
        }

        return Ok(result);
    }

    /// <summary>Lista las operaciones de seguridad NACHA más recientes.</summary>
    /// <param name="take">Cantidad solicitada. El servicio normaliza el valor al rango 1..200 antes de consultar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar auditoría de operaciones de seguridad NACHA")]
    [EndpointDescription("Devuelve las operaciones almacenadas ordenadas por RequestedAtUtc descendente. El parámetro take tiene valor predeterminado 100 y el servicio lo normaliza al rango 1..200; valores fuera de ese rango no producen un status HTTP de error.")]
    [HttpGet("audit")]
    [Authorize(Policy = P1Policies.NachaSecurityRead)]
    [ProducesResponseType(typeof(IReadOnlyList<DigitalEnvelopeOperationDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de operaciones de seguridad NACHA ordenadas desde la más reciente, limitada por el valor normalizado de take.")]
    public async Task<ActionResult<IReadOnlyList<DigitalEnvelopeOperationDto>>> AuditAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ListAuditAsync(take, cancellationToken));
    }

    /// <summary>Autoriza temporalmente la descarga del artefacto asociado a una operación.</summary>
    /// <param name="operationId">Identificador de la operación cuyo artefacto se desea descargar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Autorizar descarga de artefacto de seguridad")]
    [EndpointDescription("Busca la operación, exige el permiso de descarga derivado del tipo de artefacto y solicita al servicio una autorización temporal. Devuelve 404 si la operación no existe, 403 cuando el usuario no posee el permiso de descarga requerido y 400 cuando la descarga de plano está bloqueada por SIGNATURE_VALIDATION_FAILED o cuando el servicio no autoriza la descarga. El éxito devuelve un body anónimo con operationId, authorized=true y expiresAtUtc.")]
    [HttpPost("{operationId}/authorize-download")]
    [Authorize(Policy = P1Policies.NachaSecurityAuthorizeDownload)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Autorización concedida; el body anónimo contiene operationId, authorized y expiresAtUtc.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La descarga no puede autorizarse; el body anónimo contiene code, message y operationId.")]
    [OpenApiResponseDescription(StatusCodes.Status403Forbidden, "El usuario autenticado no posee el permiso de descarga requerido para el artefacto de la operación.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la operación indicada; el body anónimo contiene code, message y operationId.")]
    public async Task<IActionResult> AuthorizeDownloadAsync(string operationId, CancellationToken cancellationToken)
    {
        var operation = await _service.GetByOperationIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            return NotFound(new { code = "OPERATION_NOT_FOUND", message = "Operación no encontrada.", operationId });
        }

        var requiredPermission = ResolveRequiredDownloadPermission(operation);
        if (!HasPermission(requiredPermission))
        {
            return Forbid();
        }

        if (requiredPermission == FineGrainedPermissions.CanDownloadPlainNacha
            && string.Equals(operation.Error?.Code, "SIGNATURE_VALIDATION_FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { code = "SIGNATURE_VALIDATION_FAILED", message = "No está permitida la descarga de plano cuando la firma falla.", operationId });
        }

        var result = await _service.AuthorizeDownloadAsync(operationId, BuildContext(), cancellationToken);
        if (!result.Authorized)
        {
            return BadRequest(new { code = result.Code, message = result.Message, operationId });
        }

        return Ok(new { operationId, authorized = true, expiresAtUtc = result.ExpiresAtUtc });
    }

    /// <summary>Descarga el artefacto de una operación cuando existe una autorización vigente.</summary>
    /// <param name="operationId">Identificador de la operación cuyo artefacto se desea descargar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar artefacto de seguridad autorizado")]
    [EndpointDescription("Busca la operación y exige el permiso de descarga derivado del tipo de artefacto. Devuelve 404 si la operación no existe, 403 cuando falta ese permiso y 400 cuando la descarga de plano está bloqueada por SIGNATURE_VALIDATION_FAILED o cuando no existe una autorización/artefacto disponible. En éxito devuelve el stream, Content-Type y nombre de archivo entregados por el servicio; esos valores dependen del artefacto persistido y no se fuerzan a un media type único en esta operación.")]
    [HttpGet("{operationId}/download")]
    [Authorize(Policy = P1Policies.NachaSecurityRead)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Stream del artefacto con Content-Type y nombre de descarga informados por el descriptor persistido.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La descarga no está autorizada o no está disponible, o la descarga de plano está bloqueada; el body anónimo contiene code, message y operationId.")]
    [OpenApiResponseDescription(StatusCodes.Status403Forbidden, "El usuario autenticado no posee el permiso de descarga requerido para el artefacto de la operación.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la operación indicada; el body anónimo contiene code, message y operationId.")]
    public async Task<IActionResult> DownloadAsync(string operationId, CancellationToken cancellationToken)
    {
        var operation = await _service.GetByOperationIdAsync(operationId, cancellationToken);
        if (operation is null)
        {
            return NotFound(new { code = "OPERATION_NOT_FOUND", message = "Operación no encontrada.", operationId });
        }

        var requiredPermission = ResolveRequiredDownloadPermission(operation);
        if (!HasPermission(requiredPermission))
        {
            return Forbid();
        }

        if (requiredPermission == FineGrainedPermissions.CanDownloadPlainNacha
            && string.Equals(operation.Error?.Code, "SIGNATURE_VALIDATION_FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { code = "SIGNATURE_VALIDATION_FAILED", message = "No está permitida la descarga de plano cuando la firma falla.", operationId });
        }

        var descriptor = await _service.OpenDownloadAsync(operationId, BuildContext(), cancellationToken);
        if (descriptor is null)
        {
            return BadRequest(new { code = "UNAUTHORIZED_DOWNLOAD", message = "Descarga no autorizada o no disponible.", operationId });
        }

        return File(descriptor.Content, descriptor.ContentType, descriptor.FileName);
    }

    private static string ResolveRequiredDownloadPermission(DigitalEnvelopeOperationDto operation)
    {
        var contentType = operation.Artifact.ContentType ?? string.Empty;
        var isPlain = string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase)
                      || operation.OperationType == NachaSecurityOperationType.NachaGeneratePlain
                      || operation.OperationType == NachaSecurityOperationType.ManualEnvelopeDecrypt;

        return isPlain ? FineGrainedPermissions.CanDownloadPlainNacha : FineGrainedPermissions.CanDownloadEnvelope;
    }

    private bool HasPermission(string permission)
        => User.HasClaim("permission", permission)
           || User.HasClaim("permission", "CanManageAch")
           || User.HasClaim("permission", "CanReadAch");

    private OperationRequestContext BuildContext()
    {
        return new OperationRequestContext(User?.Identity?.Name ?? "api", HttpContext?.Connection?.RemoteIpAddress?.ToString());
    }

    /// <summary>Solicitud HTTP para generar un artefacto NACHA-M a partir de un ciclo existente.</summary>
    public sealed class NachaGenerateApiRequest
    {
        /// <summary>Identificador del ciclo que el servicio usa como entrada de la generación.</summary>
        public string CycleId { get; set; } = string.Empty;
    }
}
