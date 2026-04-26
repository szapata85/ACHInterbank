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

    [EndpointSummary("Generación de archivo NACHA plano")]
    [EndpointDescription("Qué hace: genera artefacto NACHA-M sin cifrado para flujos permitidos. Cuándo se usa: en exportaciones internas o pruebas operativas controladas. Perfil consumidor: equipo ACH con funciones de generación. Permiso requerido: FineGrainedPermissions.CanGenerateNacha. Tipo de operación: modifica información. Genera auditoría: sí, en auditoría de operaciones de seguridad. Riesgos operativos: usar plano fuera de política puede exponer información sensible. Errores esperados: 400 validación, 401/403, 404 operación/ciclo no disponible. Relación ACH/CENIT/NACHA-M: creación de salida NACHA-M en módulo de seguridad. Precauciones para desarrollo u operación: aplicar políticas de manejo seguro de archivos planos.")]
    [HttpPost("nacha/generate")]
    [Authorize(Policy = FineGrainedPermissions.CanGenerateNacha)]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GeneratePlainAsync([FromBody] NachaGenerateApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.GeneratePlainAsync(
            new NachaGenerateRequest(request.CycleId, false),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    [EndpointSummary("Generación de NACHA cifrado")]
    [EndpointDescription("Qué hace: genera artefacto NACHA-M con cifrado de sobre digital. Cuándo se usa: para intercambio seguro con contrapartes o cámaras. Perfil consumidor: operación ACH y seguridad bancaria. Permiso requerido: FineGrainedPermissions.CanGenerateEncryptedNacha. Tipo de operación: modifica información. Genera auditoría: sí, explícita. Riesgos operativos: cifrado con insumos inválidos bloquea despacho interbancario. Errores esperados: 400 validación, 401/403, 404. Relación ACH/CENIT/NACHA-M: cumple requisito de protección de NACHA-M en tránsito. Precauciones para desarrollo u operación: verificar certificados activos antes de generar.")]
    [HttpPost("nacha/generate-encrypted")]
    [Authorize(Policy = FineGrainedPermissions.CanGenerateEncryptedNacha)]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GenerateEncryptedAsync([FromBody] NachaGenerateApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateEncryptedAsync(
            new NachaGenerateRequest(request.CycleId, true),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    [EndpointSummary("Cifrado manual de archivo")]
    [EndpointDescription("Qué hace: cifra manualmente un archivo cargado por operador. Cuándo se usa: en contingencia o validación técnica controlada. Perfil consumidor: seguridad operativa. Permiso requerido: FineGrainedPermissions.CanManualEncryptEnvelope. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: cifrar archivo equivocado puede producir envío inválido. Errores esperados: 400 archivo requerido/invalidación; 401/403. Relación ACH/CENIT/NACHA-M: aplica controles de sobre digital para NACHA-M. Precauciones para desarrollo u operación: confirmar nombre, origen y contenido antes de cifrar.")]
    [HttpPost("envelope/manual-encrypt")]
    [Authorize(Policy = FineGrainedPermissions.CanManualEncryptEnvelope)]
    [RequestSizeLimit(50 * 1024 * 1024)]
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

    [EndpointSummary("Descifrado manual de sobre digital")]
    [EndpointDescription("Qué hace: descifra manualmente un archivo de sobre digital. Cuándo se usa: en análisis de incidentes o soporte interoperabilidad. Perfil consumidor: seguridad y soporte especializado. Permiso requerido: FineGrainedPermissions.CanManualDecryptEnvelope. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: descifrar material no autorizado compromete confidencialidad. Errores esperados: 400 archivo requerido; 401/403. Relación ACH/CENIT/NACHA-M: soporta validación de interoperabilidad NACHA-M/CENIT. Precauciones para desarrollo u operación: resguardar el archivo plano resultante bajo controles estrictos.")]
    [HttpPost("envelope/manual-decrypt")]
    [Authorize(Policy = FineGrainedPermissions.CanManualDecryptEnvelope)]
    [RequestSizeLimit(50 * 1024 * 1024)]
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

    [EndpointSummary("Consulta de operación de seguridad")]
    [EndpointDescription("Qué hace: recupera estado y metadatos de una operación de seguridad digital. Cuándo se usa: para seguimiento de generación/cifrado/descifrado. Perfil consumidor: operación ACH y auditoría. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas. Riesgos operativos: interpretar estado incompleto puede causar descargas no autorizadas. Errores esperados: 404 operación no encontrada; 401/403. Relación ACH/CENIT/NACHA-M: trazabilidad de ciclo de artefactos NACHA-M. Precauciones para desarrollo u operación: verificar código de error antes de habilitar descarga.")]
    [HttpGet("{operationId}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GetByOperationIdAsync(string operationId, CancellationToken cancellationToken)
    {
        var result = await _service.GetByOperationIdAsync(operationId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { code = "OPERATION_NOT_FOUND", message = "Operación no encontrada.", operationId });
        }

        return Ok(result);
    }

    [EndpointSummary("Bitácora de auditoría de seguridad NACHA")]
    [EndpointDescription("Qué hace: lista operaciones de seguridad recientes para control y forénsica. Cuándo se usa: en revisiones de cumplimiento y post-mortem. Perfil consumidor: auditoría y seguridad. Permiso requerido: FineGrainedPermissions.CanViewNachaSecurityAudit. Tipo de operación: solo consulta. Genera auditoría: sí, es fuente directa de auditoría. Riesgos operativos: omitir revisión periódica reduce detección temprana de desvíos. Errores esperados: 401/403 y 400 por parámetro take fuera de rango lógico. Relación ACH/CENIT/NACHA-M: gobierna evidencia de seguridad para NACHA-M. Precauciones para desarrollo u operación: limitar acceso y exportar evidencia bajo cadena de custodia.")]
    [HttpGet("audit")]
    [Authorize(Policy = FineGrainedPermissions.CanViewNachaSecurityAudit)]
    public async Task<ActionResult<IReadOnlyList<DigitalEnvelopeOperationDto>>> AuditAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ListAuditAsync(take, cancellationToken));
    }

    [EndpointSummary("Autorizar descarga de artefacto")]
    [EndpointDescription("Qué hace: emite autorización temporal para descargar artefacto de una operación. Cuándo se usa: antes de descarga por canal controlado. Perfil consumidor: operación ACH con permisos finos. Permiso requerido: CanReadAch + permiso fino de descarga según tipo de artefacto. Tipo de operación: modifica información. Genera auditoría: sí. Riesgos operativos: autorizar de forma indebida habilita exfiltración de información. Errores esperados: 404 operación; 400 firma inválida o autorización no posible; 401/403. Relación ACH/CENIT/NACHA-M: controla liberación de archivos NACHA-M y sobres digitales. Precauciones para desarrollo u operación: validar tipo de artefacto y vigencia antes de autorizar.")]
    [HttpPost("{operationId}/authorize-download")]
    [Authorize(Policy = "CanReadAch")]
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

    [EndpointSummary("Descarga de artefacto autorizado")]
    [EndpointDescription("Qué hace: descarga el contenido de la operación si existe autorización válida. Cuándo se usa: después de autorizar descarga y dentro de vigencia. Perfil consumidor: operación ACH y seguridad. Permiso requerido: CanReadAch + permiso fino de descarga. Tipo de operación: solo consulta. Genera auditoría: sí, por registro de descarga. Riesgos operativos: descargar sin controles puede filtrar datos sensibles. Errores esperados: 404 operación; 400 descarga no autorizada/firma inválida; 401/403. Relación ACH/CENIT/NACHA-M: entrega final de artefactos NACHA-M/ sobre digital. Precauciones para desarrollo u operación: respetar ventanas de autorización y almacenamiento seguro.")]
    [HttpGet("{operationId}/download")]
    [Authorize(Policy = "CanReadAch")]
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

    public sealed class NachaGenerateApiRequest
    {
        public string CycleId { get; set; } = string.Empty;
    }
}
