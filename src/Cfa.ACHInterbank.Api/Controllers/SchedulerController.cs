using System.Security.Claims;
using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Application.Scheduler.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
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

    [HttpGet("overview")]
    [Authorize(Policy = P1Policies.SchedulerView)]
    public async Task<ActionResult<SchedulerOverviewDto>> GetOverview(CancellationToken cancellationToken)
        => Ok(await _service.GetOverviewAsync(cancellationToken));

    [HttpGet("tasks")]
    [Authorize(Policy = P1Policies.SchedulerView)]
    public async Task<ActionResult<IReadOnlyList<SchedulerTaskDto>>> GetTasks(CancellationToken cancellationToken)
        => Ok(await _service.GetTasksAsync(cancellationToken));

    [HttpGet("tasks/{taskCode}")]
    [Authorize(Policy = P1Policies.SchedulerView)]
    public async Task<ActionResult<SchedulerTaskDto>> GetTask(string taskCode, CancellationToken cancellationToken)
    {
        var task = await _service.GetTaskAsync(taskCode, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpGet("history")]
    [Authorize(Policy = P1Policies.SchedulerHistoryView)]
    public async Task<ActionResult<SchedulerPagedResult<SchedulerExecutionDto>>> GetHistory(
        [FromQuery] SchedulerHistoryQuery query,
        CancellationToken cancellationToken)
        => Ok(await _service.GetHistoryAsync(null, query, cancellationToken));

    [HttpGet("tasks/{taskCode}/history")]
    [Authorize(Policy = P1Policies.SchedulerHistoryView)]
    public async Task<ActionResult<SchedulerPagedResult<SchedulerExecutionDto>>> GetTaskHistory(
        string taskCode,
        [FromQuery] SchedulerHistoryQuery query,
        CancellationToken cancellationToken)
        => Ok(await _service.GetHistoryAsync(taskCode, query, cancellationToken));

    [HttpGet("instances")]
    [Authorize(Policy = P1Policies.SchedulerViewInstances)]
    public async Task<ActionResult<IReadOnlyList<SchedulerInstanceDto>>> GetInstances(CancellationToken cancellationToken)
        => Ok(await _service.GetInstancesAsync(cancellationToken));

    [HttpGet("executions/{executionId:guid}")]
    [Authorize(Policy = P1Policies.SchedulerHistoryView)]
    public async Task<ActionResult<SchedulerExecutionDto>> GetExecution(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await _service.GetExecutionAsync(executionId, cancellationToken);
        return execution is null ? NotFound() : Ok(execution);
    }

    [HttpPost("tasks/{taskCode}/execute")]
    [Authorize(Policy = P1Policies.SchedulerExecute)]
    public async Task<ActionResult<ManualExecutionResult>> Execute(
        string taskCode,
        [FromBody] ExecuteSchedulerTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RequestId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(request.RequestId), "El Request ID es obligatorio.");
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

    [HttpPost("tasks/{taskCode}/pause")]
    [Authorize(Policy = P1Policies.SchedulerPauseResume)]
    public async Task<IActionResult> Pause(string taskCode, CancellationToken cancellationToken)
        => await _service.PauseAsync(taskCode, GetUserId(), GetUserName(), cancellationToken) ? NoContent() : NotFound();

    [HttpPost("tasks/{taskCode}/resume")]
    [Authorize(Policy = P1Policies.SchedulerPauseResume)]
    public async Task<IActionResult> Resume(string taskCode, CancellationToken cancellationToken)
        => await _service.ResumeAsync(taskCode, GetUserId(), GetUserName(), cancellationToken) ? NoContent() : NotFound();

    [HttpPut("tasks/{taskCode}/schedule")]
    [Authorize(Policy = P1Policies.SchedulerManageSchedule)]
    public async Task<ActionResult<SchedulerTaskDto>> UpdateSchedule(
        string taskCode,
        [FromBody] SchedulerScheduleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
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

    [HttpPost("schedule/preview")]
    [Authorize(Policy = P1Policies.SchedulerManageSchedule)]
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
}
