using Cfa.ACHInterbank.Application.Scheduler.Models;

namespace Cfa.ACHInterbank.Application.Scheduler.Interfaces;

public interface ISchedulerAdminService
{
    Task<SchedulerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SchedulerTaskDto>> GetTasksAsync(CancellationToken cancellationToken = default);
    Task<SchedulerTaskDto?> GetTaskAsync(string taskCode, CancellationToken cancellationToken = default);
    Task<SchedulerTechnicalInfoDto?> GetTechnicalInfoAsync(string taskCode, CancellationToken cancellationToken = default);
    Task<SchedulerPagedResult<SchedulerExecutionDto>> GetHistoryAsync(string? taskCode, SchedulerHistoryQuery query, CancellationToken cancellationToken = default);
    Task<SchedulerExecutionDto?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SchedulerInstanceDto>> GetInstancesAsync(CancellationToken cancellationToken = default);
    Task<ManualExecutionResult> ExecuteNowAsync(ExecuteSchedulerTaskCommand command, CancellationToken cancellationToken = default);
    Task<bool> PauseAsync(string taskCode, string? userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> ResumeAsync(string taskCode, string? userId, string userName, CancellationToken cancellationToken = default);
    Task<SchedulerTaskDto?> UpdateScheduleAsync(SchedulerScheduleUpdateCommand command, CancellationToken cancellationToken = default);
    Task<SchedulerSchedulePreviewDto> PreviewScheduleAsync(SchedulerScheduleUpdateRequest request, CancellationToken cancellationToken = default);
}
