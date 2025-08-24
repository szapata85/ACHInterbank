using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;

namespace Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;

public interface ITaskHandler
{
    string Code { get; } // identificador de la tarea (ej: "CheckBankHolidays")
    Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken);
}
