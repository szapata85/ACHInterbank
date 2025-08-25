namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Services;
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
