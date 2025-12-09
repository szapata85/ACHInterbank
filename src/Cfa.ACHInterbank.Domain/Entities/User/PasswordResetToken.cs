using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Entities.User;

public class PasswordResetToken : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset Expiration { get; set; }
    public bool IsUsed { get; set; }

    public User? User { get; set; }
}
