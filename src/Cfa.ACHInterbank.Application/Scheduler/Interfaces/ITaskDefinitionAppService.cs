using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Dtos;

namespace Cfa.ACHInterbank.Application.Scheduler.Interfaces;

public interface ITaskDefinitionAppService
{
    Task<IReadOnlyList<TaskDefinitionDto>> GetAllAsync(CancellationToken ct = default);
    Task<TaskDefinitionDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TaskDefinitionDto> CreateAsync(TaskDefinitionDto request, CancellationToken ct = default);
    Task<TaskDefinitionDto?> UpdateAsync(int id, TaskDefinitionDto request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
