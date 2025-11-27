using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Domain.Entities.Navigation;

public class MenuItemRole
{
    public int MenuItemId { get; set; }
    public Guid RoleId { get; set; }

    public MenuItem? MenuItem { get; set; }
    public Role? Role { get; set; }
}
