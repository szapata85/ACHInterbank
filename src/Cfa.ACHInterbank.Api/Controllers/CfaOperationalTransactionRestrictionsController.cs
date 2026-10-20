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
[Route("api/cfa-operational-transaction-restrictions")]
[Authorize]
public sealed class CfaOperationalTransactionRestrictionsController(
    ICfaOperationalTransactionRestrictionAdminService service) : ControllerBase
{
    /// <summary>Consulta restricciones operacionales CFA con filtros opcionales.</summary>
    /// <param name="clearingHouseCode">Código opcional de cámara. Incluye restricciones directas de la cámara y restricciones de configuraciones de ciclo pertenecientes a ella.</param>
    /// <param name="policyVersion">Versión opcional de política de una configuración versionada de ciclo.</param>
    /// <param name="clearingHouseCycleConfigId">Identificador opcional de configuración versionada de ciclo.</param>
    /// <param name="transactionType">Tipo opcional de transacción al que debe estar asociada la restricción.</param>
    /// <param name="isPrenotification">Filtro opcional por indicador de prenotificación.</param>
    /// <param name="isActive">Filtro opcional por estado activo de la restricción.</param>
    /// <param name="effectiveAt">Instante opcional que debe quedar dentro del intervalo de vigencia de la restricción, incluyendo sus extremos.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar restricciones operacionales CFA")]
    [EndpointDescription("Devuelve las restricciones operacionales CFA que cumplen los filtros suministrados. El servicio puede almacenar restricciones globales, por cámara o por configuración versionada de ciclo; esta consulta no resuelve ni combina sus límites, solo devuelve los registros administrativos coincidentes.")]
    [HttpGet]
    [Authorize(Policy = FineGrainedPermissions.OperationalRestrictions.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<CfaOperationalTransactionRestrictionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Restricciones operacionales CFA que cumplen los filtros solicitados.")]
    public async Task<ActionResult<IReadOnlyList<CfaOperationalTransactionRestrictionDto>>> Get(
        [FromQuery] string? clearingHouseCode,
        [FromQuery] string? policyVersion,
        [FromQuery] int? clearingHouseCycleConfigId,
        [FromQuery] TransactionTypeEnum? transactionType,
        [FromQuery] bool? isPrenotification,
        [FromQuery] bool? isActive,
        [FromQuery] DateTime? effectiveAt,
        CancellationToken ct = default)
        => Ok(await service.GetAsync(
            new CfaOperationalTransactionRestrictionFilter(
                clearingHouseCode,
                policyVersion,
                clearingHouseCycleConfigId,
                transactionType,
                isPrenotification,
                isActive,
                effectiveAt),
            ct));

    /// <summary>Consulta una restricción operacional CFA por identificador.</summary>
    /// <param name="id">Identificador interno de la restricción.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar restricción operacional CFA")]
    [EndpointDescription("Busca una restricción operacional CFA por su identificador. Devuelve 404 sin cuerpo cuando no existe el registro solicitado.")]
    [HttpGet("{id:int}")]
    [Authorize(Policy = FineGrainedPermissions.OperationalRestrictions.Read)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Restricción operacional CFA encontrada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una restricción operacional CFA con el identificador indicado.")]
    public async Task<ActionResult<CfaOperationalTransactionRestrictionDto>> GetById(
        int id,
        CancellationToken ct = default)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Crea una restricción operacional CFA.</summary>
    /// <param name="request">Alcance, límites, vigencia y estado inicial de la restricción.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear restricción operacional CFA")]
    [EndpointDescription("Crea una restricción global, por cámara o por configuración versionada de ciclo. El servicio valida el alcance, los valores de enum, que los límites numéricos no sean negativos y que EffectiveTo no anteceda a EffectiveFrom. Cuando se indica una configuración de ciclo y también un código de cámara, ambos deben corresponder a la misma cámara. Las validaciones capturadas se responden como 422.")]
    [HttpPost]
    [Authorize(Policy = FineGrainedPermissions.OperationalRestrictions.Manage)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionApiError), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Restricción operacional CFA creada.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "El alcance, los límites, los valores de enum o la vigencia de la restricción no son válidos.")]
    public async Task<ActionResult<CfaOperationalTransactionRestrictionDto>> Create(
        [FromBody] CreateCfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default)
        => await ExecuteAsync(async () =>
        {
            var created = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        });

    /// <summary>Actualiza una restricción operacional CFA.</summary>
    /// <param name="id">Identificador de la restricción que se desea actualizar.</param>
    /// <param name="request">Nuevo alcance, límites, vigencia, estado y versión esperada del registro.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar restricción operacional CFA")]
    [EndpointDescription("Reemplaza los datos administrables de una restricción existente después de validar ExpectedRowVersion. Devuelve 404 cuando el id no existe, 409 cuando la versión esperada no coincide con la versión persistida y 422 cuando el alcance, los límites, los enums o la vigencia son inválidos.")]
    [HttpPut("{id:int}")]
    [Authorize(Policy = FineGrainedPermissions.OperationalRestrictions.Manage)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionApiError), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Restricción operacional CFA actualizada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una restricción operacional CFA con el identificador indicado.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La restricción cambió respecto de ExpectedRowVersion; la respuesta puede incluir la RowVersion vigente.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La versión esperada, el alcance, los límites, los valores de enum o la vigencia no son válidos.")]
    public async Task<ActionResult<CfaOperationalTransactionRestrictionDto>> Update(
        int id,
        [FromBody] UpdateCfaOperationalTransactionRestrictionRequest request,
        CancellationToken ct = default)
        => await ExecuteAsync(async () =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        });

    /// <summary>Activa una restricción operacional CFA.</summary>
    /// <param name="id">Identificador de la restricción que se desea activar.</param>
    /// <param name="request">Versión esperada del registro y fecha final de vigencia opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Activar restricción operacional CFA")]
    [EndpointDescription("Marca la restricción como activa después de validar ExpectedRowVersion. EffectiveTo es opcional; cuando se informa, se guarda como nueva fecha final y no puede anteceder a EffectiveFrom. Devuelve 404 para un id inexistente, 409 ante conflicto de versión y 422 para una versión o vigencia inválidas.")]
    [HttpPatch("{id:int}/activate")]
    [Authorize(Policy = FineGrainedPermissions.OperationalRestrictions.Manage)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionApiError), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Restricción operacional CFA activada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una restricción operacional CFA con el identificador indicado.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La restricción cambió respecto de ExpectedRowVersion; la respuesta puede incluir la RowVersion vigente.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "ExpectedRowVersion o la fecha final de vigencia no son válidos.")]
    public async Task<ActionResult<CfaOperationalTransactionRestrictionDto>> Activate(
        int id,
        [FromBody] ChangeCfaOperationalTransactionRestrictionStatusRequest request,
        CancellationToken ct = default)
        => await ExecuteAsync(async () =>
        {
            var updated = await service.SetActiveAsync(id, true, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        });

    /// <summary>Desactiva una restricción operacional CFA.</summary>
    /// <param name="id">Identificador de la restricción que se desea desactivar.</param>
    /// <param name="request">Versión esperada del registro y fecha final de vigencia opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Desactivar restricción operacional CFA")]
    [EndpointDescription("Marca la restricción como inactiva después de validar ExpectedRowVersion. EffectiveTo es opcional; cuando se informa, se guarda como nueva fecha final y no puede anteceder a EffectiveFrom. Devuelve 404 para un id inexistente, 409 ante conflicto de versión y 422 para una versión o vigencia inválidas.")]
    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Policy = FineGrainedPermissions.OperationalRestrictions.Manage)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(CfaOperationalTransactionRestrictionApiError), StatusCodes.Status422UnprocessableEntity)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Restricción operacional CFA desactivada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una restricción operacional CFA con el identificador indicado.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La restricción cambió respecto de ExpectedRowVersion; la respuesta puede incluir la RowVersion vigente.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "ExpectedRowVersion o la fecha final de vigencia no son válidos.")]
    public async Task<ActionResult<CfaOperationalTransactionRestrictionDto>> Deactivate(
        int id,
        [FromBody] ChangeCfaOperationalTransactionRestrictionStatusRequest request,
        CancellationToken ct = default)
        => await ExecuteAsync(async () =>
        {
            var updated = await service.SetActiveAsync(id, false, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        });

    private static async Task<ActionResult<CfaOperationalTransactionRestrictionDto>> ExecuteAsync(
        Func<Task<ActionResult<CfaOperationalTransactionRestrictionDto>>> action)
    {
        try
        {
            return await action();
        }
        catch (CfaOperationalTransactionRestrictionConcurrencyException ex)
        {
            return new ConflictObjectResult(new CfaOperationalTransactionRestrictionApiError(
                "CFA_OPERATIONAL_RESTRICTION_CONCURRENCY",
                ex.Message,
                ex.CurrentRowVersion));
        }
        catch (CfaOperationalTransactionRestrictionValidationException ex)
        {
            return new UnprocessableEntityObjectResult(new CfaOperationalTransactionRestrictionApiError(
                "CFA_OPERATIONAL_RESTRICTION_INVALID",
                ex.Message));
        }
    }
}
