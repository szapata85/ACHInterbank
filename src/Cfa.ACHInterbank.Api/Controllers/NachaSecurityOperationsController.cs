using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("nacha/generate")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GeneratePlainAsync([FromBody] NachaGenerateApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.GeneratePlainAsync(
            new NachaGenerateRequest(request.CycleId, false),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("nacha/generate-encrypted")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<DigitalEnvelopeOperationDto>> GenerateEncryptedAsync([FromBody] NachaGenerateApiRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateEncryptedAsync(
            new NachaGenerateRequest(request.CycleId, true),
            BuildContext(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("envelope/manual-encrypt")]
    [Authorize(Policy = "CanReadAch")]
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

    [HttpPost("envelope/manual-decrypt")]
    [Authorize(Policy = "CanReadAch")]
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

    [HttpGet("audit")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IReadOnlyList<DigitalEnvelopeOperationDto>>> AuditAsync([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ListAuditAsync(take, cancellationToken));
    }

    [HttpPost("{operationId}/authorize-download")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> AuthorizeDownloadAsync(string operationId, CancellationToken cancellationToken)
    {
        var result = await _service.AuthorizeDownloadAsync(operationId, BuildContext(), cancellationToken);
        if (!result.Authorized)
        {
            return BadRequest(new { code = result.Code, message = result.Message, operationId });
        }

        return Ok(new { operationId, authorized = true, expiresAtUtc = result.ExpiresAtUtc });
    }

    [HttpGet("{operationId}/download")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> DownloadAsync(string operationId, CancellationToken cancellationToken)
    {
        var descriptor = await _service.OpenDownloadAsync(operationId, BuildContext(), cancellationToken);
        if (descriptor is null)
        {
            return BadRequest(new { code = "UNAUTHORIZED_DOWNLOAD", message = "Descarga no autorizada o no disponible.", operationId });
        }

        return File(descriptor.Content, descriptor.ContentType, descriptor.FileName);
    }

    private OperationRequestContext BuildContext()
    {
        return new OperationRequestContext(User?.Identity?.Name ?? "api", HttpContext?.Connection?.RemoteIpAddress?.ToString());
    }

    public sealed class NachaGenerateApiRequest
    {
        public string CycleId { get; set; } = string.Empty;
    }
}
