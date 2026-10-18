using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/clearing-house-transaction-rules")]
[Authorize]
public class ClearingHouseTransactionRulesController : ControllerBase
{
    private readonly IClearingHouseTransactionRuleService _service;

    public ClearingHouseTransactionRulesController(IClearingHouseTransactionRuleService service)
    {
        _service = service;
    }

    /// <summary>Consulta reglas transaccionales configuradas por cámara.</summary>
    /// <param name="clearingHouseId">Identificador opcional de cámara para limitar el resultado.</param>
    /// <param name="transactionNature">Naturaleza opcional de transacción. El filtro solo se aplica cuando el texto puede convertirse a TransactionNature.</param>
    /// <param name="includeInactive">Indica si se incluyen reglas inactivas; por defecto solo se devuelven reglas activas.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar reglas transaccionales por cámara")]
    [EndpointDescription("Devuelve las reglas que cumplen los filtros suministrados. ClearingHouseId es opcional; TransactionNature solo filtra cuando puede convertirse a Credit o Debit y, si no puede convertirse, el servicio actual no aplica ese filtro. IncludeInactive es false por defecto, por lo que normalmente solo se devuelven reglas activas.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ClearingHouseTransactionRuleDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reglas transaccionales que cumplen los filtros solicitados.")]
    public async Task<IActionResult> Get(
        [FromQuery] int? clearingHouseId,
        [FromQuery] string? transactionNature,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
        => Ok(await _service.GetAsync(clearingHouseId, transactionNature, includeInactive, ct));

    /// <summary>Consulta una regla transaccional por identificador.</summary>
    /// <param name="id">Identificador interno de la regla.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar regla transaccional")]
    [EndpointDescription("Busca una regla por su identificador sin requerir un identificador de cámara adicional. Devuelve 404 sin body cuando no existe un registro con el id indicado.")]
    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Regla transaccional encontrada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una regla transaccional con el identificador indicado.")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var rule = await _service.GetByIdAsync(id, ct);
        return rule is null ? NotFound() : Ok(rule);
    }

    /// <summary>Crea una regla transaccional de compatibilidad para una cámara.</summary>
    /// <param name="request">Configuración de cámara, tipo, prenotificación, vigencia, validación de receptor y metadata de la regla.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear regla transaccional por cámara")]
    [EndpointDescription("Crea una nueva versión a partir del contrato de regla. El servicio exige coherencia entre TransactionNature y TransactionType, deriva RequiresPrenotification desde PrenotificationMode, exige que la regla aplique a exportación NACHA-M y transacciones monetarias, valida vigencias y metadata y evita solapamientos. Las reglas rechazadas se responden como 422.")]
    [HttpPost]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Regla transaccional creada y ubicación canónica devuelta mediante CreatedAtAction.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La regla no cumple las validaciones de coherencia, vigencia, metadata o solapamiento del servicio.")]
    public async Task<IActionResult> Create([FromBody] CreateClearingHouseTransactionRuleRequest request, CancellationToken ct = default)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }

    /// <summary>Actualiza una regla transaccional existente.</summary>
    /// <param name="id">Identificador de la regla existente.</param>
    /// <param name="request">Configuración solicitada para la regla o su siguiente versión funcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar regla transaccional por cámara")]
    [EndpointDescription("No permite cambiar la cámara ni el tipo de una versión existente. Cuando cambia PrenotificationMode, el plazo o las fechas de vigencia, EffectiveFrom debe ser posterior al de la versión actual y el servicio crea una nueva versión; si no hay cambio funcional, actualiza metadata y validación de identificación del receptor sobre la versión existente. Un id inexistente o una regla inválida se responden como 422.")]
    [HttpPut("{id:int}")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Regla actualizada o nueva versión funcional creada según los cambios solicitados.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La regla no existe o la actualización incumple las validaciones del servicio.")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClearingHouseTransactionRuleRequest request, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }

    /// <summary>Activa una regla transaccional existente.</summary>
    /// <param name="id">Identificador de la regla que se desea activar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Activar regla transaccional")]
    [EndpointDescription("Marca la regla como activa cuando no existe otra regla activa de la misma cámara y tipo con una vigencia solapada. Un id inexistente o un solapamiento se responden como 422.")]
    [HttpPatch("{id:int}/activate")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Regla transaccional activada.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La regla no existe o su activación produciría un solapamiento activo.")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.SetActiveAsync(id, true, ct));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }

    /// <summary>Desactiva una regla transaccional existente.</summary>
    /// <param name="id">Identificador de la regla que se desea desactivar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Desactivar regla transaccional")]
    [EndpointDescription("Marca la regla como inactiva. El servicio no aplica la validación de solapamiento al desactivar; un id inexistente se traduce a 422 por el manejo actual del controller.")]
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Regla transaccional desactivada.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "No existe una regla transaccional con el identificador indicado.")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct = default)
    {
        try
        {
            return Ok(await _service.SetActiveAsync(id, false, ct));
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { code = "CLEARING_HOUSE_TRANSACTION_RULE_INVALID", message = ex.Message });
        }
    }
}
