using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Entities.User;

public class PasswordRuleSetting : AuditableEntity
{
    public int Id { get; set; }
    public int MinLength { get; set; }
    public int MinUppercase { get; set; }
    public int MinNumbers { get; set; }
    public int MinSpecial { get; set; }
    public int? MaxSpecial { get; set; }
}
