using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Services;

namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

public abstract class AuditableEntity : IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

