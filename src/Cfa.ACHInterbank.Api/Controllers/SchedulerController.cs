using System.Security.Claims;
using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Application.Scheduler.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/scheduler")]
[Authorize]
public sealed class SchedulerController : ControllerBase
{
    private readonly ISchedulerAdminService _service;

    public SchedulerController(ISchedulerAdminService service)
    {
        _service = service;
    }

    /// <summary>Consulta el resumen operativo del scheduler.</summary>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar resumen operativo del scheduler")]
    [EndpointDescription("Consolida las instancias registradas para el scheduler configurado, ejecuciones de las últimas 24 horas, tareas con próxima ejecución, metadata del scheduler Quartz y definiciones pendientes de sincronización. Los conteos se calculan con el estado persistido y la instancia Quartz activa al momento de la consulta.")]
    [HttpGet("overview")]
    [Authorize(Policy = P1Policies.SchedulerView)]
    [ProducesResponseType(typeof(SchedulerOverviewDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resumen operativo calculado para el scheduler configurado.")]
    public async Task<ActionResult<SchedulerOverviewDto>> GetOverview(CancellationToken cancellationToken)
        => Ok(await _service.GetOverviewAsync(cancellationToken));

    /// <summary>Lista las tareas administradas por el scheduler.</summary>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar tareas del scheduler")]
    [EndpointDescription("Consulta las definiciones de tarea reconocidas por el catálogo del scheduler y las enriquece con su última ejecución, estado de ejecución actual, próxima ejecución y, cuando aplica, contextos operativos derivados de ciclos de compensación. Las definiciones sin entrada en el catálogo SchedulerTaskCatalog no se incluyen.")]
    [HttpGet("tasks")]
    [Authorize(Policy = P1Policies.SchedulerView)]
    [ProducesResponseType(typeof(IReadOnlyList<SchedulerTaskDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Tareas reconocidas por el catálogo del scheduler con su estado operativo actual.")]
    public async Task<ActionResult<IReadOnlyList<SchedulerTaskDto>>> GetTasks(CancellationToken cancellationToken)
        => Ok(await _service.GetTasksAsync(cancellationToken));

    /// <summary>Consulta una tarea administrada por el scheduler.</summary>
    /// <param name="taskCode">Código público de la tarea definido por el catálogo del scheduler.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar tarea del scheduler")]
    [EndpointDescription("Resuelve taskCode mediante SchedulerTaskCatalog y consulta la definición persistida asociada. La respuesta incluye programación, última y próxima ejecución, estado actual, sincronización y contextos operativos cuando la tarea depende de ciclos. Devuelve 404 cuando el código no está autorizado por el catálogo o no existe su definición persistida.")]
    [HttpGet("tasks/{taskCode}")]
    [Authorize(Policy = P1Policies.SchedulerView)]
    [ProducesResponseType(typeof(SchedulerTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle operativo de la tarea solicitada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El código de tarea no está autorizado por el catálogo o no existe su definición persistida.")]
    public async Task<ActionResult<SchedulerTaskDto>> GetTask(string taskCode, CancellationToken cancellationToken)
    {
        var task = await _service.GetTaskAsync(taskCode, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    /// <summary>Consulta información técnica de una tarea del scheduler.</summary>
    /// <param name="taskCode">Código público de la tarea definido por el catálogo del scheduler.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar información técnica de tarea")]
    [EndpointDescription("Resuelve la tarea por su código público y devuelve el handler interno, claves Quartz, programación persistida, política de misfire, flags de recuperación/concurrencia, parámetros persistidos y triggers asociados al job. Devuelve 404 cuando el código no está autorizado por el catálogo o no existe su definición persistida.")]
    [HttpGet("tasks/{taskCode}/technical")]
    [Authorize(Policy = P1Policies.SchedulerViewTechnical)]
    [ProducesResponseType(typeof(SchedulerTechnicalInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Información técnica de la tarea y sus triggers Quartz.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El código de tarea no está autorizado por el catálogo o no existe su definición persistida.")]
    public async Task<ActionResult<SchedulerTechnicalInfoDto>> GetTechnicalInfo(
        string taskCode,
        CancellationToken cancellationToken)
    {
        var task = await _service.GetTechnicalInfoAsync(taskCode, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    /// <summary>Consulta el historial global de ejecuciones del scheduler.</summary>
    /// <param name="query">Filtros y paginación del historial. Page se normaliza a mínimo 1 y PageSize al rango 1..100.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar historial global del scheduler")]
    [EndpointDescription("Consulta ejecuciones persistidas del scheduler aplicando los filtros informados por estado, rango de inicio, tipo de trigger, instancia, usuario y correlación. Page se normaliza a mínimo 1, PageSize se limita al rango 1..100 y los resultados se ordenan desde la ejecución más reciente por StartedAt.")]
    [HttpGet("history")]
    [Authorize(Policy = P1Policies.SchedulerHistoryView)]
    [ProducesResponseType(typeof(SchedulerPagedResult<SchedulerExecutionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de ejecuciones del scheduler que cumplen los filtros solicitados.")]
    public async Task<ActionResult<SchedulerPagedResult<SchedulerExecutionDto>>> GetHistory(
        [FromQuery] SchedulerHistoryQuery query,
        CancellationToken cancellationToken)
        => Ok(await _service.GetHistoryAsync(null, query, cancellationToken));

    /// <summary>Consulta el historial de ejecuciones de una tarea.</summary>
    /// <param name="taskCode">Código público de la tarea definido por el catálogo del scheduler.</param>
    /// <param name="query">Filtros y paginación del historial. Page se normaliza a mínimo 1 y PageSize al rango 1..100.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar historial de una tarea")]
    [EndpointDescription("Consulta ejecuciones persistidas para la tarea identificada por taskCode y aplica los filtros informados. Si el código no existe en SchedulerTaskCatalog, devuelve una página vacía en lugar de 404. Page se normaliza a mínimo 1, PageSize se limita al rango 1..100 y los resultados se ordenan desde la ejecución más reciente por StartedAt.")]
    [HttpGet("tasks/{taskCode}/history")]
    [Authorize(Policy = P1Policies.SchedulerHistoryView)]
    [ProducesResponseType(typeof(SchedulerPagedResult<SchedulerExecutionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de ejecuciones de la tarea; puede ser vacía cuando el código no existe o no tiene ejecuciones coincidentes.")]
    public async Task<ActionResult<SchedulerPagedResult<SchedulerExecutionDto>>> GetTaskHistory(
        string taskCode,
        [FromQuery] SchedulerHistoryQuery query,
        CancellationToken cancellationToken)
        => Ok(await _service.GetHistoryAsync(taskCode, query, cancellationToken));

    /// <summary>Lista las instancias registradas del scheduler.</summary>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar instancias del scheduler")]
    [EndpointDescription("Lista las instancias persistidas cuyo SchedulerName coincide con el scheduler Quartz activo. El servicio calcula el estado reportado con los umbrales configurados de heartbeat y desconexión, identifica la instancia actual y expone el número persistido de trabajos en ejecución.")]
    [HttpGet("instances")]
    [Authorize(Policy = P1Policies.SchedulerViewInstances)]
    [ProducesResponseType(typeof(IReadOnlyList<SchedulerInstanceDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Instancias registradas para el scheduler Quartz activo.")]
    public async Task<ActionResult<IReadOnlyList<SchedulerInstanceDto>>> GetInstances(CancellationToken cancellationToken)
        => Ok(await _service.GetInstancesAsync(cancellationToken));

    /// <summary>Consulta una ejecución del scheduler por su identificador.</summary>
    /// <param name="executionId">Identificador GUID de la ejecución persistida.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar ejecución del scheduler")]
    [EndpointDescription("Busca una ejecución persistida por ExecutionId y devuelve su información de disparo, instancia, solicitante, tiempos, estado, resultado, error y recuperación. Devuelve 404 cuando no existe una ejecución con el identificador indicado.")]
    [HttpGet("executions/{executionId:guid}")]
    [Authorize(Policy = P1Policies.SchedulerHistoryView)]
    [ProducesResponseType(typeof(SchedulerExecutionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de la ejecución solicitada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una ejecución con el identificador indicado.")]
    public async Task<ActionResult<SchedulerExecutionDto>> GetExecution(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await _service.GetExecutionAsync(executionId, cancellationToken);
        return execution is null ? NotFound() : Ok(execution);
    }

    /// <summary>Solicita la ejecución manual de una tarea del scheduler.</summary>
    /// <param name="taskCode">Código público de la tarea definido por el catálogo del scheduler.</param>
    /// <param name="request">Motivo e identificador único de la solicitud manual.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Ejecutar manualmente una tarea")]
    [EndpointDescription("Valida que RequestId sea distinto de Guid.Empty y que Reason, después de recortar espacios, tenga entre 10 y 500 caracteres. El servicio usa RequestId para detectar solicitudes ya registradas: una nueva ejecución aceptada devuelve 202; una solicitud duplicada devuelve 200; una tarea en conflicto o una solicitud rechazada por reglas operativas devuelve 409; y un taskCode no autorizado por el catálogo o sin definición persistida devuelve 404. El body ManualExecutionResult conserva el outcome y los identificadores disponibles para cada resultado.")]
    [HttpPost("tasks/{taskCode}/execute")]
    [Authorize(Policy = P1Policies.SchedulerExecute)]
    [ProducesResponseType(typeof(ManualExecutionResult), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ManualExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ManualExecutionResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ManualExecutionResult), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status202Accepted, "La solicitud manual fue registrada y disparada hacia Quartz; el resultado incluye ExecutionId.")]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "RequestId ya estaba registrado; se devuelve la ejecución asociada sin volver a disparar la tarea.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "RequestId es Guid.Empty o Reason no cumple la longitud de 10 a 500 caracteres; se devuelve ValidationProblemDetails.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El código de tarea no está autorizado por el catálogo o no existe su definición persistida; se devuelve ManualExecutionResult con outcome NotFound.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La ejecución entra en conflicto con una ejecución activa no paralela o es rechazada porque la ejecución manual no está habilitada, la tarea está pausada/deshabilitada o no está disponible en Quartz.")]
    public async Task<ActionResult<ManualExecutionResult>> Execute(
        string taskCode,
        [FromBody] ExecuteSchedulerTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RequestId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.RequestId), "El identificador de solicitud es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10 || request.Reason.Trim().Length > 500)
        {
            ModelState.AddModelError(nameof(request.Reason), "El motivo es obligatorio y debe tener entre 10 y 500 caracteres.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _service.ExecuteNowAsync(new ExecuteSchedulerTaskCommand(
            taskCode,
            request.Reason,
            request.RequestId,
            GetUserId(),
            GetUserName(),
            HttpContext.TraceIdentifier), cancellationToken);

        return result.Outcome switch
        {
            ManualExecutionOutcome.Accepted => AcceptedAtAction(nameof(GetExecution), new { executionId = result.ExecutionId }, result),
            ManualExecutionOutcome.Duplicate => Ok(result),
            ManualExecutionOutcome.Conflict or ManualExecutionOutcome.Rejected => Conflict(result),
            _ => NotFound(result)
        };
    }

    /// <summary>Pausa una tarea administrada por el scheduler.</summary>
    /// <param name="taskCode">Código público de la tarea definido por el catálogo del scheduler.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Pausar tarea del scheduler")]
    [EndpointDescription("Marca la definición como pausada, deja la sincronización pendiente, solicita a Quartz pausar el job y registra la sincronización resultante. Si X-Correlation-ID está presente se usa para la auditoría de la modificación; en caso contrario se usa HttpContext.TraceIdentifier. Devuelve 404 cuando taskCode no corresponde a una tarea disponible.")]
    [HttpPost("tasks/{taskCode}/pause")]
    [Authorize(Policy = P1Policies.SchedulerPauseResume)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status204NoContent, "La tarea fue encontrada, marcada como pausada y la operación Quartz finalizó correctamente.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El código de tarea no corresponde a una definición disponible para el scheduler.")]
    public async Task<IActionResult> Pause(string taskCode, CancellationToken cancellationToken)
    {
        SetAuditScope("Scheduler.Pause");
        return await _service.PauseAsync(taskCode, GetUserId(), GetUserName(), cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>Reanuda una tarea administrada por el scheduler.</summary>
    /// <param name="taskCode">Código público de la tarea definido por el catálogo del scheduler.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reanudar tarea del scheduler")]
    [EndpointDescription("Marca la definición como no pausada, sincroniza su configuración con Quartz, solicita reanudar el job y registra la sincronización resultante. Si X-Correlation-ID está presente se usa para la auditoría de la modificación; en caso contrario se usa HttpContext.TraceIdentifier. Devuelve 404 cuando taskCode no corresponde a una tarea disponible.")]
    [HttpPost("tasks/{taskCode}/resume")]
    [Authorize(Policy = P1Policies.SchedulerPauseResume)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status204NoContent, "La tarea fue encontrada, sincronizada y reanudada correctamente en Quartz.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El código de tarea no corresponde a una definición disponible para el scheduler.")]
    public async Task<IActionResult> Resume(string taskCode, CancellationToken cancellationToken)
    {
        SetAuditScope("Scheduler.Resume");
        return await _service.ResumeAsync(taskCode, GetUserId(), GetUserName(), cancellationToken) ? NoContent() : NotFound();
    }

    /// <summary>Actualiza la programación de una tarea administrada por el scheduler.</summary>
    /// <param name="taskCode">Código público de la tarea definido por el catálogo del scheduler.</param>
    /// <param name="request">Programación solicitada para la tarea.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar programación de tarea")]
    [EndpointDescription("Rechaza con 400 las tareas cuya programación depende del ciclo de compensación y las programaciones inválidas: PeriodicityType o MisfirePolicy no definidos, StartAt no anterior a EndAt, TimeZoneId no reconocido por TimeZoneInfo, cron inválido para Quartz, intervalo EveryNMinutes fuera de 1..1440, minuto horario fuera de 0..59, hora no interpretable para periodicidades diarias/semanales/mensuales, día semanal ausente, día mensual fuera de 1..31 o StartAt ausente para Once/Yearly. También aplica el intervalo mínimo definido por SchedulerTaskCatalog cuando corresponda. Con una solicitud válida actualiza y sincroniza la tarea; devuelve 404 si taskCode no corresponde a una definición disponible. X-Correlation-ID se usa opcionalmente para la auditoría.")]
    [HttpPut("tasks/{taskCode}/schedule")]
    [Authorize(Policy = P1Policies.SchedulerManageSchedule)]
    [ProducesResponseType(typeof(SchedulerTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Programación persistida y sincronizada; se devuelve el estado operativo actualizado de la tarea.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La programación no cumple una regla validada por el servicio; el controller conserva el body anónimo con la propiedad message.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "Con una programación válida, el código de tarea no corresponde a una definición disponible.")]
    public async Task<ActionResult<SchedulerTaskDto>> UpdateSchedule(
        string taskCode,
        [FromBody] SchedulerScheduleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            SetAuditScope("Scheduler.UpdateSchedule");
            var task = await _service.UpdateScheduleAsync(
                new SchedulerScheduleUpdateCommand(taskCode, request, GetUserId(), GetUserName()),
                cancellationToken);
            return task is null ? NotFound() : Ok(task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Previsualiza una programación sin persistirla.</summary>
    /// <param name="request">Programación que se desea validar y previsualizar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Previsualizar programación del scheduler")]
    [EndpointDescription("Valida la programación con las mismas reglas generales usadas por la actualización —PeriodicityType, MisfirePolicy, fechas, TimeZoneId y campos requeridos por periodicidad— sin asociarla a una tarea ni persistir cambios. Para una programación válida devuelve una descripción y hasta cinco próximas ejecuciones calculadas por el servicio. Los errores ArgumentException se traducen a 400 conservando el body anónimo con message.")]
    [HttpPost("schedule/preview")]
    [Authorize(Policy = P1Policies.SchedulerManageSchedule)]
    [ProducesResponseType(typeof(SchedulerSchedulePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Descripción de la programación y próximas ejecuciones calculadas sin persistencia.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La programación no cumple una regla validada por el servicio; el controller conserva el body anónimo con la propiedad message.")]
    public async Task<ActionResult<SchedulerSchedulePreviewDto>> PreviewSchedule(
        [FromBody] SchedulerScheduleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.PreviewScheduleAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("uid")
           ?? User.FindFirstValue("sub");

    private string GetUserName()
        => User.Identity?.Name
           ?? User.FindFirstValue("unique_name")
           ?? "usuario-autenticado";

    private void SetAuditScope(string action)
    {
        var correlation = Request.Headers["X-Correlation-ID"].ToString();
        HttpContext.Items[AchDbContext.AuditActionItemKey] = action;
        HttpContext.Items[AchDbContext.AuditCorrelationItemKey] = string.IsNullOrWhiteSpace(correlation)
            ? HttpContext.TraceIdentifier
            : correlation.Trim();
    }
}
