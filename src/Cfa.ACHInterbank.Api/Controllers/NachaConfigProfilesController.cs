using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-config")]
[Authorize]
public sealed class NachaConfigProfilesController : ControllerBase
{
    private readonly INachaConfigProfileQueryService _query;
    private readonly INachaConfigProfileCommandService _command;
    private readonly INachaConfigValidationService _validation;
    private readonly INachaConfigPublicationService _publication;
    private readonly INachaConfigHistoryService _history;
    private readonly INachaConfigPreviewService _preview;
    private readonly INachaConfigOperationalTestService _operationalTest;

    public NachaConfigProfilesController(
        INachaConfigProfileQueryService query,
        INachaConfigProfileCommandService command,
        INachaConfigValidationService validation,
        INachaConfigPublicationService publication,
        INachaConfigHistoryService history,
        INachaConfigPreviewService preview,
        INachaConfigOperationalTestService operationalTest)
    {
        _query = query;
        _command = command;
        _validation = validation;
        _publication = publication;
        _history = history;
        _preview = preview;
        _operationalTest = operationalTest;
    }

    /// <summary>Lista los perfiles de configuración NACHA persistidos.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar perfiles de configuración NACHA")]
    [EndpointDescription("Devuelve todos los perfiles de configuración NACHA persistidos, ordenados por UpdatedAt descendente. Cada elemento expone identificación, códigos de estado/cámara/flujo/dirección/servicio, versión, vigencia y RowVersion codificado en Base64; la acción no aplica filtros adicionales.")]
    [HttpGet("perfiles")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigProfileListItemDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de perfiles de configuración NACHA ordenada por la última actualización persistida.")]
    public async Task<ActionResult<IReadOnlyList<NachaConfigProfileListItemDto>>> GetProfiles(CancellationToken ct)
        => Ok(await _query.GetProfilesAsync(ct));

    /// <summary>Consulta los catálogos usados para filtrar configuración NACHA.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar catálogos de filtro NACHA")]
    [EndpointDescription("Devuelve estados de configuración y catálogos de cámaras, flujos, direcciones, clases de servicio, registros, tipos de origen y tipos de regla. Las opciones se obtienen desde la configuración persistida; las cámaras, flujos, direcciones y servicios solo incluyen registros activos.")]
    [HttpGet("catalogos-filtro")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(NachaConfigFilterCatalogsDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Catálogos persistidos disponibles para filtros y compatibilidad de la configuración NACHA.")]
    public async Task<ActionResult<NachaConfigFilterCatalogsDto>> GetFilterCatalogs(CancellationToken ct)
        => Ok(await _query.GetFilterCatalogsAsync(ct));

    /// <summary>Consulta catálogos operativos NACHA con dependencias de contexto.</summary>
    /// <param name="camaraCode">Código opcional de cámara para limitar servicios válidos.</param>
    /// <param name="flujoCode">Código opcional de flujo para limitar la dirección predeterminada cuando el catálogo la define.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar catálogos operativos NACHA")]
    [EndpointDescription("Devuelve catálogos humanizados desde las tablas de configuración. Cuando se informa camaraCode, las clases de servicio se limitan a las específicas de esa cámara y a las globales; cuando se informa flujoCode con dirección predeterminada, solo se devuelve esa dirección activa.")]
    [HttpGet("catalogos-operativos")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(NachaConfigFilterCatalogsDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Catálogos operativos NACHA humanizados y filtrados por el contexto informado.")]
    public async Task<ActionResult<NachaConfigFilterCatalogsDto>> GetOperationalCatalogs(
        [FromQuery] string? camaraCode,
        [FromQuery] string? flujoCode,
        CancellationToken ct)
        => Ok(await _query.GetOperationalCatalogsAsync(camaraCode, flujoCode, ct));

    /// <summary>Consulta el detalle de un perfil de configuración NACHA.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar perfil de configuración NACHA")]
    [EndpointDescription("Obtiene el perfil identificado por id con sus registros, variantes de layout, campos y reglas. Los registros se ordenan por secuencia; las variantes por código de registro y prioridad; los campos por posición inicial y las reglas por identificador. Devuelve 404 sin body cuando el perfil no existe.")]
    [HttpGet("perfiles/{id:int}")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(NachaConfigProfileDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle del perfil de configuración NACHA solicitado, incluyendo registros, variantes, campos y reglas persistidos.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un perfil de configuración NACHA con el identificador indicado.")]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> GetProfile(int id, CancellationToken ct)
    {
        var detail = await _query.GetProfileDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Consulta el read model de trabajo humanizado de un perfil NACHA.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar workspace de configuración NACHA")]
    [EndpointDescription("Devuelve el perfil con nombres funcionales persistidos para estado, cámara, flujo, dirección, servicio, registros, variantes, campos, tipos de origen y tipos de regla. Conserva los códigos técnicos para trazabilidad y expone si el estado permite edición.")]
    [HttpGet("perfiles/{id:int}/workspace")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(NachaConfigWorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Workspace humanizado del perfil solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un perfil de configuración NACHA con el identificador indicado.")]
    public async Task<ActionResult<NachaConfigWorkspaceDto>> GetWorkspace(int id, CancellationToken ct)
    {
        var workspace = await _query.GetWorkspaceAsync(id, ct);
        return workspace is null ? NotFound() : Ok(workspace);
    }

    /// <summary>Consulta el editor operativo contextual de un campo NACHA-M.</summary>
    [EndpointSummary("Consultar editor operativo de campo NACHA")]
    [EndpointDescription("Devuelve el campo solicitado, sus reglas existentes y opciones de origen/formato/alineación disponibles para la misma cámara. Las rutas internas, expresiones y configuraciones técnicas se conservan solo como información avanzada de lectura.")]
    [HttpGet("perfiles/{id:int}/fields/{fieldId:int}/editor")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(NachaConfigFieldEditorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Editor operativo contextual del campo solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el perfil o el campo no pertenece al perfil indicado.")]
    public async Task<ActionResult<NachaConfigFieldEditorDto>> GetFieldEditor(int id, int fieldId, CancellationToken ct)
    {
        var editor = await _query.GetFieldEditorAsync(id, fieldId, ct);
        return editor is null ? NotFound() : Ok(editor);
    }

    [HttpPost("perfiles")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> CreateDraft([FromBody] NachaConfigCreateDraftRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync<NachaConfigProfileDetailDto>(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            var created = await _command.CreateDraftAsync(request, actor, ct);
            return CreatedAtAction(nameof(GetProfile), new { id = created.Id }, created);
        });
    }

    [HttpPut("perfiles/{id:int}")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> UpdateDraft(int id, [FromBody] NachaConfigUpdateProfileRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync<NachaConfigProfileDetailDto>(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            var updated = await _command.UpdateDraftAsync(id, request, actor, ct);
            return updated is null ? NotFound() : Ok(updated);
        });
    }

    [HttpPost("perfiles/{id:int}/clonar")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> Clone(int id, [FromBody] NachaConfigCloneProfileRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync<NachaConfigProfileDetailDto>(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            var cloned = await _command.CloneProfileAsync(id, request, actor, ct);
            return cloned is null ? NotFound() : Ok(cloned);
        });
    }

    [HttpPost("perfiles/{id:int}/validar")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<NachaConfigValidationResultDto>> Validate(int id, CancellationToken ct)
        => Ok(await _validation.ValidateBeforePublishAsync(id, ct));

    /// <summary>Prueba una configuración NACHA-M sin publicarla ni generar efectos operativos.</summary>
    [EndpointSummary("Probar configuración NACHA")]
    [EndpointDescription("Reutiliza la validación integral, la resolución contextual y el renderer oficial para construir una vista previa sintética de solo lectura. No publica, no persiste archivos, no despacha, no usa MFT/SOAP y no modifica configuraciones publicadas.")]
    [HttpPost("perfiles/{id:int}/probar")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(NachaConfigOperationalTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado humanizado de validación, prueba de selección y vista previa estructural/contenido sintético.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el perfil de configuración NACHA indicado.")]
    public async Task<ActionResult<NachaConfigOperationalTestResultDto>> TestConfiguration(int id, CancellationToken ct)
    {
        var result = await _operationalTest.TestAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("perfiles/{id:int}/publicar")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<NachaConfigPublicationResultDto>> Publish(int id, [FromBody] NachaConfigStateTransitionRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync<NachaConfigPublicationResultDto>(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            var result = await _publication.PublishAsync(id, actor, request.ExpectedRowVersion, ct);
            if (result.Publicado)
            {
                return Ok(result);
            }

            return UnprocessableEntity(new NachaConfigApiErrorDto
            {
                ErrorCode = "PUBLISH_BLOCKED",
                Message = result.Mensaje
            });
        });
    }

    [HttpPost("perfiles/{id:int}/inactivar")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> Inactivate(int id, [FromBody] NachaConfigStateTransitionRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            return await _command.InactivateProfileAsync(id, actor, request.ExpectedRowVersion, ct) ? Ok() : NotFound();
        });
    }

    [HttpPost("perfiles/{id:int}/archivar")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> Archive(int id, [FromBody] NachaConfigStateTransitionRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            return await _command.ArchiveProfileAsync(id, actor, request.ExpectedRowVersion, ct) ? Ok() : NotFound();
        });
    }

    /// <summary>Consulta los cambios reales pendientes antes de publicar una configuración NACHA.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Revisar cambios pendientes de configuración NACHA")]
    [EndpointDescription("Compara el perfil con la configuración que reemplaza usando los datos persistidos y devuelve únicamente diferencias reales humanizadas. No modifica ni publica la configuración.")]
    [HttpGet("perfiles/{id:int}/cambios")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(NachaConfigChangeReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Comparación humanizada del borrador frente a su configuración base.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el perfil de configuración NACHA indicado.")]
    public async Task<ActionResult<NachaConfigChangeReviewDto>> Changes(int id, CancellationToken ct)
    {
        var result = await _history.GetChangeReviewAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Consulta el historial de cambios de un perfil NACHA.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar historial de configuración NACHA")]
    [EndpointDescription("Devuelve los cambios históricos ordenados del más reciente al más antiguo, con acción, elemento, versión, resumen y detalle humanizados desde la auditoría persistida. La ausencia de cambios se representa como una colección vacía.")]
    [HttpGet("perfiles/{id:int}/historial")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigHistoryItemDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección humanizada, posiblemente vacía, de eventos históricos del perfil ordenados del más reciente al más antiguo.")]
    public async Task<ActionResult<IReadOnlyList<NachaConfigHistoryItemDto>>> History(int id, CancellationToken ct)
        => Ok(await _history.GetHistoryAsync(id, ct));

    /// <summary>Consulta snapshots históricos de un perfil NACHA.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar snapshots de configuración NACHA")]
    [EndpointDescription("Devuelve los snapshots históricos cuyo ProfileId coincide con id, ordenados por CreatedAtUtc descendente. La ausencia de snapshots se representa como una colección vacía; la acción no traduce ese caso a 404.")]
    [HttpGet("perfiles/{id:int}/snapshots")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigSnapshotItemDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección, posiblemente vacía, de snapshots históricos del perfil ordenados del más reciente al más antiguo.")]
    public async Task<ActionResult<IReadOnlyList<NachaConfigSnapshotItemDto>>> Snapshots(int id, CancellationToken ct)
        => Ok(await _history.GetSnapshotsAsync(id, ct));

    [HttpPut("perfiles/{id:int}/records/secuencia")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> UpdateSequence(int id, [FromBody] NachaConfigRecordSequenceUpdateRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            return await _command.UpdateRecordSequenceAsync(id, request, actor, ct) ? Ok() : NotFound();
        });
    }

    [HttpPut("perfiles/{id:int}/variantes/{variantId:int}")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> UpdateVariant(int id, int variantId, [FromBody] NachaConfigLayoutVariantEditDto request, CancellationToken ct)
    {
        return await ExecuteHandledAsync(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            return await _command.UpdateLayoutVariantAsync(id, variantId, request, actor, ct) ? Ok() : NotFound();
        });
    }

    [HttpPut("perfiles/{id:int}/fields/{fieldId:int}")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> UpdateField(int id, int fieldId, [FromBody] NachaConfigLayoutFieldEditDto request, CancellationToken ct)
    {
        return await ExecuteHandledAsync(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            return await _command.UpdateLayoutFieldAsync(id, fieldId, request, actor, ct) ? Ok() : NotFound();
        });
    }

    [HttpPut("perfiles/{id:int}/fields/{fieldId:int}/editor")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> UpdateFieldEditor(int id, int fieldId, [FromBody] NachaConfigFieldEditorUpdateRequest request, CancellationToken ct)
    {
        return await ExecuteHandledAsync(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            return await _command.UpdateFieldEditorAsync(id, fieldId, request, actor, ct) ? Ok() : NotFound();
        });
    }

    [HttpPut("perfiles/{id:int}/rules/{ruleId:int}")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> UpdateRule(int id, int ruleId, [FromBody] NachaConfigFieldRuleEditDto request, CancellationToken ct)
    {
        return await ExecuteHandledAsync(async () =>
        {
            var actor = User?.Identity?.Name ?? "system";
            return await _command.UpdateFieldRuleAsync(id, ruleId, request, actor, ct) ? Ok() : NotFound();
        });
    }

    [HttpPost("resolver-preview")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<NachaConfigResolverPreviewResultDto>> ResolverPreview([FromBody] NachaConfigResolverPreviewRequest request, CancellationToken ct)
        => Ok(await _preview.PreviewResolverAsync(request, ct));

    private async Task<ActionResult> ExecuteHandledAsync(Func<Task<ActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (NachaConfigException ex)
        {
            return StatusCode(ex.HttpStatusCode, new NachaConfigApiErrorDto
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                CurrentRowVersion = ex.CurrentRowVersion
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new NachaConfigApiErrorDto
            {
                ErrorCode = "INVALID_OPERATION",
                Message = ex.Message
            });
        }
    }

    private async Task<ActionResult<T>> ExecuteHandledAsync<T>(Func<Task<ActionResult<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (NachaConfigException ex)
        {
            return StatusCode(ex.HttpStatusCode, new NachaConfigApiErrorDto
            {
                ErrorCode = ex.ErrorCode,
                Message = ex.Message,
                CurrentRowVersion = ex.CurrentRowVersion
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new NachaConfigApiErrorDto
            {
                ErrorCode = "INVALID_OPERATION",
                Message = ex.Message
            });
        }
    }
}
