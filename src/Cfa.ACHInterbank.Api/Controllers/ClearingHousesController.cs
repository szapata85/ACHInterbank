using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("clearing-houses")]
[Route("api/clearing-houses")]
[Authorize]
public class ClearingHousesController : ControllerBase
{
    private readonly IClearingHouseService _service;
    private readonly IAchCycleAppService _cycleService;

    public ClearingHousesController(IClearingHouseService service, IAchCycleAppService cycleService)
    {
        _service = service;
        _cycleService = cycleService;
    }

    /// <summary>
    /// Consulta administrativamente las cámaras compensadoras con paginación y filtros opcionales.
    /// </summary>
    /// <param name="request">Paginación, texto de búsqueda y estado activo opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar cámaras compensadoras")]
    [EndpointDescription("Devuelve un listado paginado de cámaras compensadoras. Permite buscar por código o nombre y filtrar por estado activo sin asumir reglas particulares de una cámara específica.")]
    [HttpGet]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(typeof(PaginatedResult<ClearingHouseDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Listado paginado de cámaras compensadoras que cumplen los filtros solicitados.")]
    public async Task<IActionResult> Get([FromQuery] ClearingHouseAdminQuery request, CancellationToken ct)
        => Ok(await _service.GetAsync(request, ct));

    /// <summary>
    /// Obtiene las cámaras que están activas y cumplen sus requisitos de readiness.
    /// </summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar cámaras operativas")]
    [EndpointDescription("Devuelve únicamente cámaras activas cuyo readiness está cumplido. El readiness se calcula con la configuración actual de cada cámara y sus requisitos operativos registrados.")]
    [HttpGet("operational")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IReadOnlyList<ClearingHouseDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de cámaras activas y listas para operar según su configuración actual.")]
    public async Task<IActionResult> GetOperational(CancellationToken ct)
        => Ok(await _service.GetOperationalAsync(ct));

    /// <summary>
    /// Consulta las estrategias de riel de pago que pueden seleccionarse al configurar una cámara.
    /// </summary>
    [EndpointSummary("Consultar opciones de riel de pago")]
    [EndpointDescription("Devuelve las estrategias operativas administrativamente seleccionables, excluye el código Unknown y elimina duplicados por código. Cada elemento publica Code y Name; Code es el valor que puede utilizarse como PaymentRailCode al configurar una cámara.")]
    [HttpGet("payment-rail-options")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(typeof(IReadOnlyList<ClearingHousePaymentRailOptionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Opciones de riel de pago seleccionables para la configuración administrativa de una cámara.")]
    public IActionResult GetPaymentRailOptions()
        => Ok(_service.GetPaymentRailOptions());

    /// <summary>
    /// Consulta los perfiles NACHA-M publicados disponibles para configurar una cámara.
    /// </summary>
    /// <param name="clearingHouseCode">Código opcional de la cámara. Cuando se proporciona, limita los perfiles publicados correspondientes a esa cámara según la lógica actual del servicio; si se omite, devuelve los perfiles publicados disponibles.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar perfiles NACHA-M disponibles")]
    [EndpointDescription("Devuelve el catálogo interno de ACHInterbank de perfiles NACHA-M publicados. clearingHouseCode es opcional y permite conservar el contexto de cámara al limitar los resultados. IsCurrent indica si el perfil está vigente en la fecha actual, por lo que un perfil publicado puede aparecer con IsCurrent=false. Id es el valor utilizable como NachaProfileId.")]
    [HttpGet("nacha-profiles")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(typeof(IReadOnlyList<ClearingHouseNachaProfileOptionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Perfiles NACHA-M publicados disponibles según el filtro opcional de cámara.")]
    public async Task<IActionResult> GetNachaProfiles([FromQuery] string? clearingHouseCode, CancellationToken ct)
        => Ok(await _service.GetNachaProfilesAsync(clearingHouseCode, ct));

    /// <summary>
    /// Obtiene una cámara compensadora por su identificador interno.
    /// </summary>
    /// <param name="id">Identificador interno de la cámara compensadora.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar cámara compensadora")]
    [EndpointDescription("Devuelve la configuración administrativa y el estado de readiness de la cámara identificada por id. Si la cámara no existe, responde con ProblemDetails.")]
    [HttpGet("{id:int}")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(typeof(ClearingHouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Cámara compensadora encontrada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una cámara compensadora con el identificador indicado.")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFoundProblem() : Ok(result);
    }

    /// <summary>
    /// Consulta si una cámara cumple los requisitos actuales para considerarse lista para operar.
    /// </summary>
    /// <param name="id">Identificador interno de la cámara compensadora.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar readiness de cámara")]
    [EndpointDescription("Evalúa la cámara indicada y devuelve IsReady junto con MissingRequirements. La operación no activa ni modifica la cámara; solo informa el resultado actual de readiness.")]
    [HttpGet("{id:int}/readiness")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(typeof(ClearingHouseReadinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado actual de readiness de la cámara, incluyendo los requisitos faltantes cuando existan.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una cámara compensadora con el identificador indicado.")]
    public async Task<IActionResult> GetReadiness(int id, CancellationToken ct)
    {
        try { return Ok(await _service.GetReadinessAsync(id, ct)); }
        catch (ClearingHouseNotFoundException) { return NotFoundProblem(); }
    }

    /// <summary>
    /// Registra una nueva cámara compensadora.
    /// </summary>
    /// <param name="request">Configuración administrativa inicial de la cámara.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear cámara compensadora")]
    [EndpointDescription("Crea una cámara con la configuración indicada. La cámara nueva se registra inicialmente inactiva; su activación posterior está condicionada al readiness. Las validaciones o conflictos de código se devuelven como ProblemDetails.")]
    [HttpPost]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.Create)]
    [ProducesResponseType(typeof(ClearingHouseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Cámara compensadora creada en estado inactivo.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La configuración de la cámara es incompleta o inválida.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La creación entra en conflicto con una cámara o configuración existente.")]
    public async Task<IActionResult> Create([FromBody] CreateClearingHouseRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ClearingHouseValidationException ex) { return ValidationProblem(ex); }
        catch (ClearingHouseConflictException ex) { return ConflictProblem(ex.Message); }
    }

    /// <summary>
    /// Actualiza la configuración administrativa de una cámara compensadora existente.
    /// </summary>
    /// <param name="id">Identificador interno de la cámara compensadora.</param>
    /// <param name="request">Configuración que reemplaza los valores editables de la cámara.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar cámara compensadora")]
    [EndpointDescription("Actualiza la cámara indicada. ExpectedUpdatedAt puede usarse para detectar concurrencia optimista; también pueden producirse conflictos cuando el cambio solicitado no es compatible con relaciones o estado operativo existentes.")]
    [HttpPut("{id:int}")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.Update)]
    [ProducesResponseType(typeof(ClearingHouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Cámara compensadora actualizada.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La configuración solicitada es incompleta o inválida.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una cámara compensadora con el identificador indicado.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La cámara cambió desde la versión esperada o el cambio solicitado entra en conflicto con su configuración o relaciones actuales.")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClearingHouseRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.UpdateAsync(id, request, ct)); }
        catch (ClearingHouseValidationException ex) { return ValidationProblem(ex); }
        catch (ClearingHouseConflictException ex) { return ConflictProblem(ex.Message); }
        catch (ClearingHouseNotFoundException) { return NotFoundProblem(); }
    }

    /// <summary>
    /// Activa o desactiva una cámara compensadora.
    /// </summary>
    /// <param name="id">Identificador interno de la cámara compensadora.</param>
    /// <param name="request">Estado activo solicitado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Cambiar estado de cámara compensadora")]
    [EndpointDescription("Cambia el estado activo de la cámara. Para activarla, el servicio vuelve a evaluar el readiness y rechaza la operación si existen requisitos faltantes; la desactivación no exige ese readiness.")]
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.ChangeStatus)]
    [ProducesResponseType(typeof(ClearingHouseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Estado activo de la cámara actualizado o ya coincidente con el solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La cámara no puede activarse porque no cumple los requisitos de readiness.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una cámara compensadora con el identificador indicado.")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeClearingHouseStatusRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.ChangeStatusAsync(id, request.IsActive, ct)); }
        catch (ClearingHouseValidationException ex) { return ValidationProblem(ex); }
        catch (ClearingHouseNotFoundException) { return NotFoundProblem(); }
    }

    /// <summary>
    /// Lista los ciclos asociados a una cámara compensadora.
    /// </summary>
    /// <param name="id">Identificador interno de la cámara compensadora usado como filtro.</param>
    /// <param name="processingDate">Fecha operativa opcional; cuando se informa, limita el resultado a esa ProcessingDate.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar ciclos de una cámara")]
    [EndpointDescription("Devuelve los ciclos asociados a la cámara indicada. processingDate es opcional y, cuando se proporciona, se utiliza como inicio y fin del filtro por ProcessingDate.")]
    [HttpGet("{id:int}/cycles")]
    [Authorize(Policy = FineGrainedPermissions.ClearingHouses.View)]
    [ProducesResponseType(typeof(IEnumerable<AchCycleDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de ciclos de la cámara que cumplen el filtro opcional de ProcessingDate.")]
    public async Task<IActionResult> GetCyclesForClearingHouse(
        int id,
        [FromQuery] DateTime? processingDate,
        CancellationToken ct)
        => Ok(await _cycleService.GetAsync(id, processingDate, processingDate, ct));

    private ObjectResult ValidationProblem(ClearingHouseValidationException exception)
        => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Configuración incompleta o inválida",
            detail: exception.Message,
            extensions: new Dictionary<string, object?> { ["missingRequirements"] = exception.MissingRequirements });

    private ObjectResult ConflictProblem(string detail)
        => Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflicto de cámara compensadora", detail: detail);

    private ObjectResult NotFoundProblem()
        => Problem(statusCode: StatusCodes.Status404NotFound, title: "Cámara no encontrada", detail: "La cámara compensadora no existe.");
}
