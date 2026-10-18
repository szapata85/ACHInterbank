using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ach-colombia/operational-schedules")]
public sealed class AchColombiaCycleOperationalSchedulesController(
    IAchColombiaCycleOperationalScheduleAdminService service) : ControllerBase
{
    [HttpGet("administration")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [EndpointSummary("Consultar ciclos para administración de horarios ACH Colombia")]
    [EndpointDescription("Devuelve la política de ciclos ACH Colombia más reciente disponible junto con la programación operacional existente. La ventana regulatoria es solo lectura.")]
    [ProducesResponseType(typeof(IReadOnlyList<AchColombiaCycleOperationalScheduleAdministrationRowDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AchColombiaCycleOperationalScheduleAdministrationRowDto>> GetAdministration(
        CancellationToken ct = default)
        => service.GetAdministrationAsync(ct);

    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("Consultar programación operacional ACH Colombia")]
    [EndpointDescription("Lista la programación operacional persistida por configuración de ciclo. Estos datos no modifican la agenda regulatoria ni reprograman Quartz en esta fase.")]
    [ProducesResponseType(typeof(IReadOnlyList<AchColombiaCycleOperationalScheduleDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AchColombiaCycleOperationalScheduleDto>> Get(
        [FromQuery] int? clearingHouseCycleConfigId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => service.GetAsync(new(clearingHouseCycleConfigId, isActive), ct);

    [HttpGet("by-cycle/{clearingHouseCycleConfigId:int}")]
    [Authorize(Policy = "CanReadAch")]
    [EndpointSummary("Obtener programación operacional ACH Colombia por ciclo")]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AchColombiaCycleOperationalScheduleDto>> GetByCycle(
        int clearingHouseCycleConfigId,
        CancellationToken ct = default)
    {
        var result = await service.GetByCycleAsync(clearingHouseCycleConfigId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [EndpointSummary("Crear programación operacional ACH Colombia")]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleApiError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AchColombiaCycleOperationalScheduleDto>> Create(
        [FromBody] CreateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default)
        => await ExecuteAsync(async () =>
        {
            var created = await service.CreateAsync(request, ct);
            return CreatedAtAction(
                nameof(GetByCycle),
                new { clearingHouseCycleConfigId = created.ClearingHouseCycleConfigId },
                created);
        });

    [HttpPut("{id:int}")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [EndpointSummary("Actualizar programación operacional ACH Colombia")]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleApiError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AchColombiaCycleOperationalScheduleDto>> Update(
        int id,
        [FromBody] UpdateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default)
        => await ExecuteAsync(async () =>
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        });

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Policy = "CanManageAch")]
    [EndpointSummary("Desactivar programación operacional ACH Colombia")]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(AchColombiaCycleOperationalScheduleApiError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AchColombiaCycleOperationalScheduleDto>> Deactivate(
        int id,
        [FromBody] DeactivateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default)
        => await ExecuteAsync(async () =>
        {
            var updated = await service.DeactivateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        });

    private static async Task<ActionResult<AchColombiaCycleOperationalScheduleDto>> ExecuteAsync(
        Func<Task<ActionResult<AchColombiaCycleOperationalScheduleDto>>> action)
    {
        try
        {
            return await action();
        }
        catch (AchColombiaCycleOperationalScheduleConcurrencyException ex)
        {
            return new ConflictObjectResult(new AchColombiaCycleOperationalScheduleApiError(
                "ACHCOL_OPERATIONAL_SCHEDULE_CONCURRENCY",
                ex.Message,
                ex.CurrentRowVersion));
        }
        catch (AchColombiaCycleOperationalScheduleValidationException ex)
        {
            return new UnprocessableEntityObjectResult(new AchColombiaCycleOperationalScheduleApiError(
                "ACHCOL_OPERATIONAL_SCHEDULE_INVALID",
                ex.Message));
        }
    }
}
