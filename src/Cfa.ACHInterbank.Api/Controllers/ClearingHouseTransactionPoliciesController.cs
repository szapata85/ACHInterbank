using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/clearing-houses/{clearingHouseId:int}/transaction-policies")]
[Authorize]
public sealed class ClearingHouseTransactionPoliciesController : ControllerBase
{
    private readonly IClearingHouseTransactionRuleService _rules;
    private readonly ITransactionPrerequisitePolicyService _policy;

    public ClearingHouseTransactionPoliciesController(
        IClearingHouseTransactionRuleService rules,
        ITransactionPrerequisitePolicyService policy)
    {
        _rules = rules;
        _policy = policy;
    }

    /// <summary>Consulta las versiones de política transaccional registradas para una cámara.</summary>
    /// <param name="clearingHouseId">Identificador interno de la cámara compensadora.</param>
    /// <param name="transactionType">Tipo de transacción opcional para limitar las versiones devueltas.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar versiones de política transaccional por cámara")]
    [EndpointDescription("Devuelve las versiones registradas para la cámara, activas e inactivas, ordenadas por tipo de transacción y vigencia descendente. TransactionType es opcional; cuando se informa, el servicio solo admite tipos con política por cámara. Una cámara inexistente o un tipo no soportado se traducen a 422 por el manejo actual del controller.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ClearingHouseTransactionRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versiones de política transaccional registradas para la cámara y el filtro solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La cámara no existe o el tipo de transacción informado no admite una política por cámara.")]
    public async Task<IActionResult> GetVersions(
        int clearingHouseId,
        [FromQuery] TransactionTypeEnum? transactionType,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.GetVersionsAsync(clearingHouseId, transactionType, ct)));

    /// <summary>Consulta la política transaccional vigente para una cámara, tipo y fecha.</summary>
    /// <param name="clearingHouseId">Identificador interno de la cámara compensadora.</param>
    /// <param name="transactionType">Tipo de transacción cuya política vigente se desea resolver.</param>
    /// <param name="effectiveAt">Fecha de vigencia a consultar; si se omite se usa la fecha UTC actual.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar política transaccional vigente")]
    [EndpointDescription("Busca una única versión activa cuyo intervalo de vigencia contenga EffectiveAt; si la fecha no se informa usa DateTime.UtcNow.Date. Devuelve 404 cuando no existe una versión vigente. Una cámara o tipo no válidos, o la existencia de más de una versión activa para la misma fecha, se traducen a 422.")]
    [HttpGet("current")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Política transaccional activa que corresponde a la cámara, tipo y fecha solicitados.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una política activa vigente para la cámara, tipo y fecha solicitados.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La cámara o el tipo no son válidos, o existe más de una política activa para la misma fecha.")]
    public async Task<IActionResult> GetCurrent(
        int clearingHouseId,
        [FromQuery] TransactionTypeEnum transactionType,
        [FromQuery] DateTime? effectiveAt,
        CancellationToken ct)
        => await ExecuteAsync(async () =>
        {
            var policy = await _rules.GetCurrentAsync(
                clearingHouseId,
                transactionType,
                effectiveAt?.Date ?? DateTime.UtcNow.Date,
                ct);
            return policy is null ? NotFound() : Ok(policy);
        });

    /// <summary>Consulta una versión de política por identificador dentro de una cámara.</summary>
    /// <param name="clearingHouseId">Identificador interno de la cámara compensadora.</param>
    /// <param name="id">Identificador de la versión de política.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar versión de política transaccional")]
    [EndpointDescription("Busca la versión indicada restringiendo la consulta a la cámara de la ruta. Devuelve 404 cuando la cámara existe pero la versión no pertenece a ella o no existe; una cámara inexistente se traduce a 422 por el servicio actual.")]
    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versión de política transaccional encontrada para la cámara indicada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "La versión no existe o no pertenece a la cámara indicada.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La cámara indicada no existe.")]
    public async Task<IActionResult> GetById(int clearingHouseId, int id, CancellationToken ct)
        => await ExecuteAsync(async () =>
        {
            var policy = await _rules.GetByIdAsync(clearingHouseId, id, ct);
            return policy is null ? NotFound() : Ok(policy);
        });

    /// <summary>Crea una nueva versión de política transaccional para una cámara.</summary>
    /// <param name="clearingHouseId">Identificador interno de la cámara compensadora.</param>
    /// <param name="request">Decisión de prenotificación, vigencia y metadata asociada a la nueva versión.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear versión de política transaccional")]
    [EndpointDescription("Crea una versión para Credit o Debit y deriva los campos de compatibilidad desde TransactionType y PrenotificationMode. El servicio valida vigencias, fuente y referencia, evita fechas iniciales duplicadas y solapamientos, y puede cerrar la versión anterior al día previo al nuevo EffectiveFrom. Las validaciones rechazadas se responden como 422.")]
    [HttpPost]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Nueva versión de política transaccional creada para la cámara.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La cámara, el tipo, la vigencia o la metadata de la versión no cumplen las validaciones del servicio.")]
    public async Task<IActionResult> CreateVersion(
        int clearingHouseId,
        [FromBody] CreateClearingHouseTransactionPolicyVersionRequest request,
        CancellationToken ct)
        => await ExecuteAsync(async () =>
        {
            var created = await _rules.CreateVersionAsync(clearingHouseId, request, ct);
            return CreatedAtAction(nameof(GetById), new { clearingHouseId, id = created.Id }, created);
        });

    /// <summary>Actualiza la metadata de una versión de política transaccional.</summary>
    /// <param name="clearingHouseId">Identificador interno de la cámara compensadora.</param>
    /// <param name="id">Identificador de la versión de política.</param>
    /// <param name="request">Fuente, referencia y notas que reemplazan la metadata actual.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar metadata de política transaccional")]
    [EndpointDescription("Actualiza únicamente NormativeSource, NormativeReference y Notes de la versión identificada. No modifica la decisión funcional de prenotificación ni sus fechas de vigencia. Una fuente o referencia vacía, o una versión inexistente para la cámara, se responden como 422.")]
    [HttpPatch("{id:int}/metadata")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Metadata de la versión actualizada sin cambiar su decisión funcional.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La metadata es inválida o la versión no existe para la cámara indicada.")]
    public async Task<IActionResult> UpdateMetadata(
        int clearingHouseId,
        int id,
        [FromBody] UpdateClearingHouseTransactionPolicyMetadataRequest request,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.UpdateMetadataAsync(clearingHouseId, id, request, ct)));

    /// <summary>Cierra la vigencia de una versión de política transaccional.</summary>
    /// <param name="clearingHouseId">Identificador interno de la cámara compensadora.</param>
    /// <param name="id">Identificador de la versión que se desea cerrar.</param>
    /// <param name="request">Fecha final de vigencia que se asignará a la versión.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Cerrar versión de política transaccional")]
    [EndpointDescription("Asigna EffectiveTo a la versión. La fecha no puede ser anterior a EffectiveFrom ni alcanzar o superar el EffectiveFrom de la siguiente versión del mismo tipo. Una versión inexistente o una fecha de cierre inválida se responden como 422.")]
    [HttpPost("{id:int}/close")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versión cerrada con la fecha final de vigencia solicitada.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La versión no existe para la cámara o la fecha de cierre entra en conflicto con su vigencia.")]
    public async Task<IActionResult> CloseVersion(
        int clearingHouseId,
        int id,
        [FromBody] CloseClearingHouseTransactionPolicyVersionRequest request,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.CloseVersionAsync(clearingHouseId, id, request, ct)));

    /// <summary>Activa una versión de política transaccional.</summary>
    /// <param name="clearingHouseId">Identificador interno de la cámara compensadora.</param>
    /// <param name="id">Identificador de la versión que se desea activar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Activar versión de política transaccional")]
    [EndpointDescription("Marca la versión como activa únicamente cuando no existe otra política activa del mismo tipo y cámara con una vigencia solapada. Una versión inexistente o un solapamiento activo se responden como 422.")]
    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(ClearingHouseTransactionRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Versión de política transaccional activada.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La versión no existe para la cámara o su activación produciría un solapamiento activo.")]
    public async Task<IActionResult> ActivateVersion(int clearingHouseId, int id, CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _rules.ActivateVersionAsync(clearingHouseId, id, ct)));

    /// <summary>Previsualiza la decisión de prerequisitos transaccionales para una cámara.</summary>
    /// <param name="clearingHouseId">Identificador de cámara que prevalece sobre el valor recibido en el body.</param>
    /// <param name="request">Tipo, fecha efectiva y alcance de exportación usados para evaluar la regla vigente.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Previsualizar prerequisitos transaccionales por cámara")]
    [EndpointDescription("Evalúa la regla vigente usando el ClearingHouseId de la ruta. Los tipos que no requieren política retornan 200 con Decision NOT_APPLICABLE y la ausencia de regla retorna 200 con Decision RULE_NOT_CONFIGURED. Si existen múltiples reglas vigentes para la misma cámara, tipo y fecha, el controller traduce la inconsistencia a 422.")]
    [HttpPost("preview")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(TransactionPrerequisitePreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de la evaluación previa de prerequisitos para la cámara indicada.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "Existe más de una política vigente aplicable a la misma cámara, tipo y fecha.")]
    public async Task<IActionResult> Preview(
        int clearingHouseId,
        [FromBody] TransactionPrerequisitePreviewRequest request,
        CancellationToken ct)
        => await ExecuteAsync(
            async () => Ok(await _policy.PreviewAsync(request with { ClearingHouseId = clearingHouseId }, ct)));

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new
            {
                code = "CLEARING_HOUSE_TRANSACTION_POLICY_INVALID",
                message = ex.Message
            });
        }
    }
}
