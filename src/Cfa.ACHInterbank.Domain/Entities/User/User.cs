using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using System.Collections.ObjectModel;

namespace Cfa.ACHInterbank.Domain.Entities.User;

public class User : AuditableEntity
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = new Collection<UserRole>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new Collection<PasswordResetToken>();
}
