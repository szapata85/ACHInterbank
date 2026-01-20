using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Entities.User;

public class LoginLockoutSetting : AuditableEntity
{
    public int Id { get; set; }
    public int MaxFailedAttempts { get; set; }
    public int LockoutMinutes { get; set; }
}
