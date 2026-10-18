using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Exceptions;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/integrations/mappingsets")]
[Authorize]
public class IntegrationMappingSetsController : ControllerBase
{
    private readonly IIntegrationMappingSetService _service;

    public IntegrationMappingSetsController(IIntegrationMappingSetService service)
    {
        _service = service;
    }

    /// <summary>Consulta los mapping sets de integración disponibles.</summary>
    /// <param name="methodId">Identificador opcional del método de integración por el que se filtran los mapping sets.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar mapping sets de integración")]
    [EndpointDescription("Consulta los mapping sets persistidos y, cuando methodId se informa, limita el resultado a ese método de integración. El servicio devuelve los elementos ordenados desde la actualización más reciente e incluye sus reglas.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<IntegrationMappingSetDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de mapping sets que cumplen el filtro opcional por método de integración.")]
    public async Task<IActionResult> Get([FromQuery] int? methodId, CancellationToken ct)
        => Ok(await _service.GetByMethodAsync(methodId, ct));

    /// <summary>Obtiene el mapping set publicado más reciente de un método de integración.</summary>
    /// <param name="methodId">Identificador del método de integración.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Obtener mapping set publicado por método")]
    [EndpointDescription("Busca mapping sets en estado Published para el methodId indicado y devuelve el de mayor versión. Si no existe uno publicado para ese método, responde 404 sin body.")]
    [HttpGet("published")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping set publicado de mayor versión para el método solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un mapping set publicado para el método indicado; la respuesta no tiene body.")]
    public async Task<IActionResult> GetPublished([FromQuery] int methodId, CancellationToken ct)
    {
        var published = await _service.GetPublishedByMethodAsync(methodId, ct);
        return published is null ? NotFound() : Ok(published);
    }

    /// <summary>Obtiene un mapping set de integración por identificador.</summary>
    /// <param name="id">Identificador del mapping set.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Obtener mapping set de integración")]
    [EndpointDescription("Obtiene el mapping set identificado por id junto con sus reglas. Si no existe, responde 404 sin body.")]
    [HttpGet("{id:guid}")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping set encontrado con sus reglas asociadas.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un mapping set con el identificador indicado; la respuesta no tiene body.")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Obtiene el read model funcional de relación de información para un mapping set.</summary>
    /// <param name="id">Identificador del mapping set.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar workspace de relación de información")]
    [EndpointDescription("Devuelve nombres funcionales, estados por parámetro y conteos de cobertura calculados por el backend. El detalle técnico se entrega como metadata secundaria y la consulta no modifica el mapping set.")]
    [HttpGet("{id:guid}/workspace")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IntegrationMappingWorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Read model funcional del mapping set solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe el mapping set solicitado; la respuesta no tiene body.")]
    public async Task<IActionResult> GetWorkspace(Guid id, CancellationToken ct)
    {
        var workspace = await _service.GetWorkspaceAsync(id, ct);
        return workspace is null ? NotFound() : Ok(workspace);
    }

    /// <summary>Crea un mapping set en estado borrador.</summary>
    /// <param name="request">Datos del método, nombre, notas y actor que crea el borrador.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear borrador de mapping set")]
    [EndpointDescription("Crea un mapping set para un método de integración activo. El servicio inicializa el registro en estado Draft, activo y versión 0, registra el evento de creación y devuelve el recurso creado mediante CreatedAtAction.")]
    [HttpPost]
    [Authorize(Policy = P1Policies.IntegrationMappingsManage)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status201Created)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Mapping set borrador creado y devuelto con su contrato persistido.")]
    public async Task<IActionResult> Create([FromBody] CreateIntegrationMappingSetRequest request, CancellationToken ct)
    {
        var created = await _service.CreateDraftAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Actualiza metadata de un mapping set borrador.</summary>
    /// <param name="id">Identificador del mapping set a actualizar.</param>
    /// <param name="request">Nombre, notas, estado activo y actor de la actualización.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar borrador de mapping set")]
    [EndpointDescription("Actualiza nombre, notas e indicador de actividad de un mapping set que el servicio exige esté en estado Draft, y registra el evento de actualización en su historia.")]
    [HttpPut("{id:guid}")]
    [Authorize(Policy = P1Policies.IntegrationMappingsManage)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping set borrador actualizado con su estado persistido actual.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.UpdateDraftAsync(id, request, ct));

    /// <summary>Crea o actualiza reglas de un mapping set borrador.</summary>
    /// <param name="id">Identificador del mapping set cuyas reglas se modifican.</param>
    /// <param name="request">Actor y colección de reglas a crear o actualizar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar reglas de mapping set")]
    [EndpointDescription("Crea o actualiza reglas de un mapping set Draft. El servicio exige ParameterId activos del mismo método, no admite SourceKind Expression ni ConditionExpression, normaliza Priority a un mínimo de 1 y registra el cambio en la historia del mapping set.")]
    [HttpPut("{id:guid}/rules")]
    [Authorize(Policy = P1Policies.IntegrationMappingsManage)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping set borrador devuelto con las reglas persistidas después del upsert.")]
    public async Task<IActionResult> UpsertRules(Guid id, [FromBody] UpsertIntegrationMappingRulesRequest request, CancellationToken ct)
        => Ok(await _service.UpsertRulesAsync(id, request, ct));

    /// <summary>Actualiza de forma guiada el origen y tratamiento de un parámetro.</summary>
    /// <param name="id">Identificador del mapping set borrador.</param>
    /// <param name="parameterId">Parámetro contractual que se configura.</param>
    /// <param name="request">Selección proveniente exclusivamente de metadata backend y revisión esperada.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar origen y tratamiento de un dato CFA")]
    [EndpointDescription("Actualiza un parámetro de un MappingSet Draft usando únicamente un SourceCatalogFieldId y un tratamiento compatible publicados por el backend. Rechaza conflictos de Revision sin aceptar field paths ni expresiones arbitrarias en el request guiado.")]
    [HttpPut("{id:guid}/parameters/{parameterId:long}")]
    [Authorize(Policy = P1Policies.IntegrationMappingsManage)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping set persistido después de actualizar el parámetro solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La Revision esperada ya no coincide con la configuración persistida.")]
    public async Task<IActionResult> UpdateParameterMapping(
        Guid id,
        long parameterId,
        [FromBody] UpdateIntegrationParameterMappingRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UpdateParameterMappingAsync(id, parameterId, request, ct));
        }
        catch (IntegrationMappingRevisionConflictException exception)
        {
            return RevisionConflict(exception);
        }
    }

    /// <summary>Valida la cobertura y consistencia de un mapping set.</summary>
    /// <param name="id">Identificador del mapping set que se valida.</param>
    /// <param name="request">Opciones de validación, incluido el tratamiento de advertencias.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Validar mapping set de integración")]
    [EndpointDescription("Evalúa las reglas del mapping set contra los parámetros activos y el catálogo fuente del mismo método. Devuelve issues, cobertura y estado por parámetro, y el servicio persiste el resumen de validación calculado.")]
    [HttpPost("{id:guid}/validate")]
    [Authorize(Policy = P1Policies.IntegrationMappingsValidate)]
    [ProducesResponseType(typeof(IntegrationMappingValidationResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de validación con issues, resumen de cobertura y estado de los parámetros evaluados.")]
    public async Task<IActionResult> Validate(Guid id, [FromBody] ValidateIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.ValidateAsync(id, request, ct));

    /// <summary>Genera una previsualización de resolución para un mapping set.</summary>
    /// <param name="id">Identificador del mapping set que se previsualiza.</param>
    /// <param name="request">Selección de muestra real/controlada y límite usado para el JSON de previsualización cruda.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Previsualizar resolución de mapping set")]
    [EndpointDescription("Resuelve las reglas habilitadas del mapping set sobre la muestra solicitada. Puede usar una muestra controlada, una transacción por id o la transacción más reciente opcionalmente filtrada por ciclo; si no encuentra una muestra reciente usa la muestra controlada. Devuelve los valores resueltos y representaciones JSON de la previsualización.")]
    [HttpPost("{id:guid}/preview")]
    [Authorize(Policy = P1Policies.IntegrationMappingsValidate)]
    [ProducesResponseType(typeof(IntegrationMappingPreviewResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Previsualización de los valores resueltos, su origen y las representaciones JSON generadas por el servicio.")]
    public async Task<IActionResult> Preview(Guid id, [FromBody] PreviewIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.PreviewAsync(id, request, ct));

    /// <summary>Publica un mapping set borrador válido.</summary>
    /// <param name="id">Identificador del mapping set que se publica.</param>
    /// <param name="request">Actor que publica y nota opcional de publicación.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Publicar mapping set de integración")]
    [EndpointDescription("Valida un mapping set Draft antes de publicarlo y exige que ExpectedRevision coincida con la revisión persistida. Cuando la validación es satisfactoria, archiva los mapping sets Published actuales del mismo método, asigna la siguiente versión, marca el mapping set como Published y activo, registra actor/fecha e historia de publicación.")]
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = P1Policies.IntegrationMappingsPublish)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Mapping set publicado con la versión y metadata de publicación persistidas.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La Revision esperada ya no coincide con la configuración persistida; no se publica ni se mezclan cambios.")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishIntegrationMappingSetRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.PublishAsync(id, request, ct));
        }
        catch (IntegrationMappingRevisionConflictException exception)
        {
            return RevisionConflict(exception);
        }
    }

    /// <summary>Clona un mapping set y sus reglas en un nuevo borrador.</summary>
    /// <param name="id">Identificador del mapping set de origen.</param>
    /// <param name="request">Nombre opcional del clon y actor que realiza la clonación.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Clonar mapping set de integración")]
    [EndpointDescription("Copia el método, notas y reglas del mapping set de origen a un nuevo mapping set Draft, activo y versión 0. Si NewName está vacío, el servicio deriva el nombre agregando '(clon)' al nombre de origen y registra el evento de clonación.")]
    [HttpPost("{id:guid}/clone")]
    [Authorize(Policy = P1Policies.IntegrationMappingsManage)]
    [ProducesResponseType(typeof(IntegrationMappingSetDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Nuevo mapping set borrador creado como clon del mapping set de origen.")]
    public async Task<IActionResult> Clone(Guid id, [FromBody] CloneIntegrationMappingSetRequest request, CancellationToken ct)
        => Ok(await _service.CloneAsync(id, request, ct));

    /// <summary>Clona un mapping set hacia otra versión contractual usando ParameterPath compatible.</summary>
    [EndpointSummary("Clonar mapping entre ContractVersions")]
    [EndpointDescription("Crea un MappingSet Draft asociado a la ContractVersion destino, copia únicamente reglas compatibles por ParameterPath, clasifica parámetros Copied/New/Removed/Incompatible y ejecuta validación contract+mapping sin activar contrato ni mapping.")]
    [HttpPost("{id:guid}/clone-to-contract-version")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IntegrationMappingContractCloneResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de clonación con clasificación por parámetro, Draft destino y validación conjunta.")]
    public async Task<IActionResult> CloneToContractVersion(
        Guid id,
        [FromBody] CloneIntegrationMappingToContractVersionRequest request,
        CancellationToken ct)
        => Ok(await _service.CloneToContractVersionAsync(id, request, ct));

    /// <summary>Valida conjuntamente una ContractVersion y el MappingSet asociado.</summary>
    [EndpointSummary("Validar contrato y mapping")]
    [EndpointDescription("Valida cobertura, compatibilidad contractual y evidencia de incompatibilidades no resueltas con concurrencia por Revision. No publica ni activa recursos.")]
    [HttpPost("{id:guid}/validate-contract-mapping")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IntegrationContractMappingValidationResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de validación conjunta contract+mapping con revisiones actuales.")]
    public async Task<IActionResult> ValidateContractMapping(
        Guid id,
        [FromBody] ValidateIntegrationContractMappingRequest request,
        CancellationToken ct)
        => Ok(await _service.ValidateContractMappingAsync(id, request, ct));

    /// <summary>Consulta la historia registrada de un mapping set.</summary>
    /// <param name="id">Identificador del mapping set cuya historia se consulta.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar historia de mapping set")]
    [EndpointDescription("Consulta los eventos de historia persistidos para el mapping set y los devuelve desde el más reciente por PerformedAtUtc. Si no existen eventos, devuelve una colección vacía.")]
    [HttpGet("{id:guid}/history")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<IntegrationMappingSetHistoryDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Eventos de historia del mapping set, posiblemente una colección vacía, ordenados del más reciente al más antiguo.")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct)
        => Ok(await _service.GetHistoryAsync(id, ct));

    /// <summary>Compara dos mapping sets del mismo método de integración.</summary>
    /// <param name="request">Identificadores de los mapping sets izquierdo y derecho.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Comparar mapping sets de integración")]
    [EndpointDescription("Compara dos mapping sets del mismo método. El servicio selecciona por parámetro la regla de menor prioridad numérica de cada lado y devuelve metadata de ambos conjuntos y diferencias Added, Removed, Modified o Equal con los campos que cambiaron.")]
    [HttpPost("compare")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(IntegrationMappingSetComparisonResultDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Comparación de metadata y reglas entre los dos mapping sets solicitados.")]
    public async Task<IActionResult> Compare([FromBody] CompareIntegrationMappingSetsRequest request, CancellationToken ct)
        => Ok(await _service.CompareAsync(request, ct));
    private ObjectResult RevisionConflict(IntegrationMappingRevisionConflictException exception)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "La configuración cambió",
            Detail = exception.Message
        };
        problem.Extensions["expectedRevision"] = exception.ExpectedRevision;
        problem.Extensions["currentRevision"] = exception.CurrentRevision;
        return Conflict(problem);
    }

}
