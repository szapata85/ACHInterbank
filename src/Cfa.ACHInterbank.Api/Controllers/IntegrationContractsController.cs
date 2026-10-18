using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public sealed class IntegrationContractsController : ControllerBase
{
    private readonly IIntegrationContractVersionService _service;
    private readonly IIntegrationContractArtifactAnalyzer _artifactAnalyzer;
    private readonly IIntegrationContractArtifactImportService _artifactImportService;

    public IntegrationContractsController(
        IIntegrationContractVersionService service,
        IIntegrationContractArtifactAnalyzer artifactAnalyzer,
        IIntegrationContractArtifactImportService artifactImportService)
    {
        _service = service;
        _artifactAnalyzer = artifactAnalyzer;
        _artifactImportService = artifactImportService;
    }

    [EndpointSummary("Consultar versiones contractuales por método")]
    [EndpointDescription("Lista las versiones contractuales persistidas para un método de integración sin modificar su estado ni runtime SOAP.")]
    [HttpGet("methods/{methodId:int}/contracts")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<IntegrationContractVersionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versiones contractuales del método solicitado.")]
    public async Task<IActionResult> GetByMethod(int methodId, CancellationToken ct)
        => Ok(await _service.GetByMethodAsync(methodId, ct));

    [EndpointSummary("Consultar detalle de versión contractual")]
    [EndpointDescription("Devuelve metadata contractual y parámetros del snapshot versionado. Las versiones históricas se consultan sin mutación.")]
    [HttpGet("contracts/{contractVersionId:guid}")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IntegrationContractVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de la versión contractual y sus parámetros.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe la versión contractual solicitada.")]
    public async Task<IActionResult> GetById(Guid contractVersionId, CancellationToken ct)
    {
        var result = await _service.GetDetailAsync(contractVersionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("Crear versión contractual manual")]
    [EndpointDescription("Crea una nueva versión contractual ManualApproved en estado Draft. No importa WSDL/XSD y no activa ni modifica versiones existentes.")]
    [HttpPost("methods/{methodId:int}/contracts")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IntegrationContractVersionDto), StatusCodes.Status201Created)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Nueva versión contractual Draft creada sin cutover.")]
    public async Task<IActionResult> Create(
        int methodId,
        [FromBody] CreateIntegrationContractVersionRequest request,
        CancellationToken ct)
    {
        var created = await _service.CreateDraftAsync(methodId, request, ct);
        return CreatedAtAction(nameof(GetById), new { contractVersionId = created.Id }, created);
    }

    [EndpointSummary("Clonar versión contractual")]
    [EndpointDescription("Versiona de forma no destructiva un contrato existente copiando su contrato técnico y parámetros a una nueva versión Draft completamente versionada.")]
    [HttpPost("methods/{methodId:int}/contracts/clone")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IntegrationContractVersionDetailDto), StatusCodes.Status201Created)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Clon Draft creado; la versión de origen permanece intacta.")]
    public async Task<IActionResult> Clone(
        int methodId,
        [FromBody] CloneIntegrationContractVersionRequest request,
        CancellationToken ct)
    {
        var created = await _service.CloneAsync(methodId, request, ct);
        return CreatedAtAction(nameof(GetById), new { contractVersionId = created.Contract.Id }, created);
    }

    [EndpointSummary("Actualizar metadata funcional de parámetro contractual")]
    [EndpointDescription("Actualiza únicamente nombre, descripción, ayuda, categoría y ejemplo de un parámetro de una versión editable. Conserva nombres/tipos técnicos y exige ExpectedRevision del contrato.")]
    [HttpPut("contracts/{contractVersionId:guid}/parameters/{parameterId:long}/presentation")]
    [Authorize(Policy = P1Policies.IntegrationContractsManage)]
    [ProducesResponseType(typeof(IntegrationContractParameterPresentationResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Metadata funcional actualizada y revisión contractual avanzada.")]
    public async Task<IActionResult> UpdatePresentation(
        Guid contractVersionId,
        long parameterId,
        [FromBody] UpdateIntegrationContractParameterPresentationRequest request,
        CancellationToken ct)
        => Ok(await _service.UpdateParameterPresentationAsync(contractVersionId, parameterId, request, ct));

    [EndpointSummary("Comparar versiones contractuales")]
    [EndpointDescription("Produce un diff determinista entre dos versiones del mismo método y clasifica cada cambio como MappingRequired o RequiresDevelopment.")]
    [HttpGet("contracts/{leftId:guid}/compare/{rightId:guid}")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IntegrationContractDiffDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Diff contractual con resumen e impacto por cambio.")]
    public async Task<IActionResult> Compare(Guid leftId, Guid rightId, CancellationToken ct)
        => Ok(await _service.CompareAsync(leftId, rightId, ct));

    [EndpointSummary("Activar ContractVersion + MappingSet de forma atómica")]
    [EndpointDescription("Activa una ContractVersion Validated junto con un MappingSet Draft de esa misma versión. Revalida compatibilidad/cobertura y revisiones antes de retirar el par vigente; cualquier fallo revierte toda la transición.")]
    [HttpPost("contracts/{contractVersionId:guid}/activate")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IntegrationContractMappingTransitionResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "ContractVersion y MappingSet activados atómicamente; el par vigente anterior quedó retirado/archivado.")]
    public async Task<IActionResult> Activate(
        Guid contractVersionId,
        [FromBody] ActivateIntegrationContractVersionRequest request,
        CancellationToken ct)
        => Ok(await _service.ActivateAsync(contractVersionId, request, ct));

    [EndpointSummary("Rollback contractual no destructivo")]
    [EndpointDescription("Reactiva atómicamente una ContractVersion Retired y su MappingSet Archived históricos después de revalidar compatibilidad, cobertura y revisiones de ambos pares. No clona ni reescribe el historial.")]
    [HttpPost("contracts/{targetContractVersionId:guid}/rollback")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IntegrationContractMappingTransitionResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versión histórica reactivada y versión vigente retirada de forma atómica.")]
    public async Task<IActionResult> Rollback(
        Guid targetContractVersionId,
        [FromBody] RollbackIntegrationContractVersionRequest request,
        CancellationToken ct)
        => Ok(await _service.RollbackAsync(targetContractVersionId, request, ct));

    [EndpointSummary("Analizar WSDL/XSD local de forma segura")]
    [EndpointDescription("Analiza un artefacto local sin persistirlo ni resolver referencias externas. DTD/XXE y estructuras no soportadas se rechazan fail-closed.")]
    [HttpPost("contracts/analyze")]
    [Authorize(Policy = P1Policies.IntegrationContractsManage)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(IntegrationContractArtifactLimits.MaxMultipartRequestBytes)]
    [ProducesResponseType(typeof(IntegrationContractArtifactAnalysisDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado determinista del análisis local; Importable=false contiene causas explícitas de rechazo.")]
    public async Task<IActionResult> AnalyzeArtifact(
        [FromForm] AnalyzeIntegrationContractArtifactForm request,
        CancellationToken ct)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "File es obligatorio." });
        }

        if (request.File.Length > IntegrationContractArtifactLimits.MaxArtifactBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = $"El artefacto excede el límite de {IntegrationContractArtifactLimits.MaxArtifactBytes} bytes."
            });
        }

        var content = await ReadArtifactAsync(request.File, ct);
        if (content is null)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = $"El artefacto excede el límite de {IntegrationContractArtifactLimits.MaxArtifactBytes} bytes."
            });
        }

        var result = await _artifactAnalyzer.AnalyzeAsync(
            new AnalyzeIntegrationContractArtifactRequest(
                request.File.FileName,
                content,
                request.OperationName,
                request.StandaloneXsdSoapAction),
            ct);
        return Ok(result);
    }

    [EndpointSummary("Previsualizar diff de WSDL/XSD contra versión base")]
    [EndpointDescription("Repite el análisis seguro sin persistir y calcula el diff con la misma autoridad backend usada por el lifecycle contractual. No crea ni activa versiones.")]
    [HttpPost("methods/{methodId:int}/contracts/preview/{baseContractVersionId:guid}")]
    [Authorize(Policy = P1Policies.IntegrationContractsManage)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(IntegrationContractArtifactLimits.MaxMultipartRequestBytes)]
    [ProducesResponseType(typeof(IntegrationContractArtifactPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Análisis seguro y diff previo; Diff=null cuando el artefacto no es importable.")]
    public async Task<IActionResult> PreviewArtifact(
        int methodId,
        Guid baseContractVersionId,
        [FromForm] AnalyzeIntegrationContractArtifactForm request,
        CancellationToken ct)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "File es obligatorio." });
        }

        if (request.File.Length > IntegrationContractArtifactLimits.MaxArtifactBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = $"El artefacto excede el límite de {IntegrationContractArtifactLimits.MaxArtifactBytes} bytes."
            });
        }

        var content = await ReadArtifactAsync(request.File, ct);
        if (content is null)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = $"El artefacto excede el límite de {IntegrationContractArtifactLimits.MaxArtifactBytes} bytes."
            });
        }

        var result = await _artifactImportService.PreviewDraftAsync(
            methodId,
            baseContractVersionId,
            new AnalyzeIntegrationContractArtifactRequest(
                request.File.FileName,
                content,
                request.OperationName,
                request.StandaloneXsdSoapAction),
            ct);
        return Ok(result);
    }

    [EndpointSummary("Importar WSDL/XSD local a Draft")]
    [EndpointDescription("Repite el análisis seguro y persiste únicamente un nuevo snapshot contractual Draft. Nunca valida ni activa automáticamente la versión importada.")]
    [HttpPost("methods/{methodId:int}/contracts/import")]
    [Authorize(Policy = P1Policies.IntegrationContractsManage)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(IntegrationContractArtifactLimits.MaxMultipartRequestBytes)]
    [ProducesResponseType(typeof(IntegrationContractArtifactImportResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IntegrationContractArtifactAnalysisDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Nueva versión contractual importada en Draft, sin cutover ni activación automática.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "Artefacto rechazado de forma fail-closed o solicitud inválida.")]
    public async Task<IActionResult> ImportArtifact(
        int methodId,
        [FromForm] ImportIntegrationContractArtifactForm request,
        CancellationToken ct)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "File es obligatorio." });
        }

        if (request.File.Length > IntegrationContractArtifactLimits.MaxArtifactBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = $"El artefacto excede el límite de {IntegrationContractArtifactLimits.MaxArtifactBytes} bytes."
            });
        }

        var content = await ReadArtifactAsync(request.File, ct);
        if (content is null)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = $"El artefacto excede el límite de {IntegrationContractArtifactLimits.MaxArtifactBytes} bytes."
            });
        }

        try
        {
            var created = await _artifactImportService.ImportDraftAsync(
                methodId,
                new ImportIntegrationContractArtifactRequest(
                    request.VersionLabel,
                    request.File.FileName,
                    content,
                    request.OperationName,
                    request.StandaloneXsdSoapAction,
                    request.Notes,
                    ResolveActor()),
                ct);

            return CreatedAtAction(
                nameof(GetById),
                new { contractVersionId = created.Contract.Contract.Id },
                created);
        }
        catch (IntegrationContractArtifactRejectedException ex)
        {
            return BadRequest(ex.Analysis);
        }
    }

    private static async Task<byte[]?> ReadArtifactAsync(IFormFile file, CancellationToken ct)
    {
        await using var stream = new MemoryStream((int)Math.Min(file.Length, IntegrationContractArtifactLimits.MaxArtifactBytes + 1L));
        await file.CopyToAsync(stream, ct);
        return stream.Length > IntegrationContractArtifactLimits.MaxArtifactBytes
            ? null
            : stream.ToArray();
    }

    private string ResolveActor()
        => User?.Identity?.Name
           ?? User?.Claims.FirstOrDefault(claim => claim.Type.EndsWith("/name", StringComparison.OrdinalIgnoreCase))?.Value
           ?? User?.Claims.FirstOrDefault(claim => claim.Type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value
           ?? "api";
}

public sealed class AnalyzeIntegrationContractArtifactForm
{
    public IFormFile? File { get; set; }
    public string? OperationName { get; set; }
    public string? StandaloneXsdSoapAction { get; set; }
}

public sealed class ImportIntegrationContractArtifactForm
{
    public string VersionLabel { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
    public string? OperationName { get; set; }
    public string? StandaloneXsdSoapAction { get; set; }
    public string? Notes { get; set; }
}
