using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Domain.Entities.Navigation;

public class MenuItemPermission
{
    public int MenuItemId { get; set; }
    public Guid PermissionId { get; set; }

    public MenuItem? MenuItem { get; set; }
    public Permission? Permission { get; set; }
}
