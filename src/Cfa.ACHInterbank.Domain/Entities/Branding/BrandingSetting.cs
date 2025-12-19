using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Entities.Branding;

public class BrandingSetting : AuditableEntity
{
    public int Id { get; set; }
    public string? PublicLogo { get; set; }
    public string? PrivateLogo { get; set; }
    public string? PublicBackground { get; set; }
    public string? PrivateBackground { get; set; }
    public string? SidebarBackground { get; set; }
    public string? ButtonColor { get; set; }
}
